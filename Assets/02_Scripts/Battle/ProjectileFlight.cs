using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

public static class ProjectileFlight
{
    public static UniTask<FlightResult> FlyAsync(GameObject prefab, ShotSettings shot, Vector3 start, Vector3 end, Color color, CancellationToken token)
    {
        return FlyAsync(prefab, shot, start, end, null, color, token);
    }

    public static async UniTask<FlightResult> FlyAsync(GameObject prefab, ShotSettings shot, Vector3 start, Vector3 end, AttackTarget target,
        Color color, CancellationToken token)
    {
        TrajectorySettings trajectory = PickTrajectory(shot.Trajectories);

        if (null == trajectory)
        {
            Debug.LogError($"투사체 비행 : {prefab.name}에 사용할 수 있는 궤적이 없습니다.");
            return new FlightResult(FlightOutcome.Canceled, start);
        }

        GameObject projectile = GameManager.Pool.Spawn(prefab, start, Quaternion.identity);
        projectile.transform.localScale = prefab.transform.localScale * shot.ProjectileScale;

        if (projectile.TryGetComponent(out SpriteRenderer projectileRenderer))
        {
            projectileRenderer.color = color;
        }

        if (projectile.TryGetComponent(out TrailRenderer trail))
        {
            SetupTrail(trail, shot, color);
        }

        float height = trajectory.Height.PickRandom();
        float duration = trajectory.Duration.PickRandom();

        Transform projectileTransform = projectile.transform;

        Tween flight = projectileTransform.DOPath(BuildPath(trajectory.Type, start, end, height), duration, PathType.CatmullRom)
            .SetEase(Ease.Linear);

        bool isBlocked = false;
        float flightSpeed = 0f < duration ? Vector3.Distance(start, end) / duration : 0f;
        Vector3 lastPosition = start;
        Vector3 velocity = 0f < flightSpeed ? (end - start).normalized * flightSpeed : Vector3.zero;

        if (null != target)
        {
            Transform followTarget = target.FollowTransform;
            Vector3 targetStartPosition = followTarget.position;
            float homing = shot.Homing;
            float maxHomingSpeed = flightSpeed * homing;
            Vector3 homingOffset = Vector3.zero;

            // 보정이 따라가는 속도를 탄 속도 × 추적 강도로 묶어, 목표가 갑자기 움직여도 탄이 홱 꺾이지 않게 한다.
            // 도착 지점만 보면 방패를 가로질러 지나가도 막히지 않아, 날아오는 동안 매 프레임 방패에 닿았는지 본다.
            flight = flight.OnUpdate(() =>
            {
                Vector3 desiredOffset = (followTarget.position - targetStartPosition) * (flight.ElapsedPercentage() * homing);
                homingOffset = Vector3.MoveTowards(homingOffset, desiredOffset, maxHomingSpeed * Time.deltaTime);
                projectileTransform.position += homingOffset;

                Vector3 currentPosition = projectileTransform.position;

                // 빗나간 뒤 같은 속도로 이어 가도록 실제 속도를 기억한다. 마지막 프레임은 덜 움직여 넘긴다.
                if (0f < Time.deltaTime && 1f > flight.ElapsedPercentage())
                {
                    velocity = (currentPosition - lastPosition) / Time.deltaTime;
                }

                lastPosition = currentPosition;

                if (target.IsBlockedAt(currentPosition))
                {
                    isBlocked = true;
                    flight.Kill();
                }
            });
        }

        // 전투가 끝나 토큰이 취소되면 트윈을 멈추고, 예외를 던지는 대신 isCanceled를 true로 돌려받는다.
        bool isCanceled = await flight.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token).SuppressCancellationThrow();

        Vector3 landingPoint = projectileTransform.position;
        FlightOutcome outcome = GetOutcome(isBlocked, isCanceled, target, landingPoint);

        if (FlightOutcome.Missed == outcome)
        {
            await FlyPastAsync(projectileTransform, velocity, shot.MissTravelTime, token);
        }

        GameManager.Pool.Despawn(projectile);

        return new FlightResult(outcome, landingPoint);
    }

    // 방패에 막혀 직접 멈춘 경우는 트윈이 취소로 끝나더라도 막힌 것으로 본다.
    private static FlightOutcome GetOutcome(bool isBlocked, bool isCanceled, AttackTarget target, Vector3 landingPoint)
    {
        if (isBlocked)
        {
            return FlightOutcome.Blocked;
        }

        if (isCanceled)
        {
            return FlightOutcome.Canceled;
        }

        if (null != target && false == target.IsHit(landingPoint))
        {
            return FlightOutcome.Missed;
        }

        return FlightOutcome.Arrived;
    }

    // 외곽선 같은 자식까지 함께 사라지도록 색 대신 크기를 줄인다.
    private static async UniTask FlyPastAsync(Transform projectile, Vector3 velocity, float travelTime, CancellationToken token)
    {
        if (0f >= travelTime)
        {
            return;
        }

        Vector3 destination = projectile.position + velocity * travelTime;

        Sequence pass = DOTween.Sequence()
            .Join(projectile.DOMove(destination, travelTime).SetEase(Ease.Linear))
            .Join(projectile.DOScale(0f, travelTime).SetEase(Ease.InQuad));

        await pass.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token).SuppressCancellationThrow();
    }

    private static void SetupTrail(TrailRenderer trail, ShotSettings shot, Color color)
    {
        // 풀에서 다시 꺼낸 투사체에는 이전 비행의 꼬리가 남아 있어, 그대로 두면 이전 위치에서 새 출발점까지 선이 그어진다.
        trail.Clear();

        trail.emitting = 0f < shot.TrailTime;
        trail.time = shot.TrailTime;
        trail.widthMultiplier = shot.TrailWidth;

        Color trailColor = shot.UseTrailColor ? shot.TrailColor : color;
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
    }

    // 경로의 시작점은 투사체의 현재 위치라서 경유점과 도착점만 넣는다.
    private static Vector3[] BuildPath(TrajectoryType type, Vector3 start, Vector3 end, float height)
    {
        switch (type)
        {
            case TrajectoryType.Straight:
                return new Vector3[] { end };

            case TrajectoryType.Wave:
                // 날아가는 방향에 수직인 방향으로 번갈아 비켜 가며 물결을 만든다.
                Vector3 normal = Vector3.Cross((end - start).normalized, Vector3.forward);

                return new Vector3[]
                {
                    Vector3.Lerp(start, end, 0.33f) + normal * height,
                    Vector3.Lerp(start, end, 0.66f) - normal * height,
                    end,
                };

            default:
                return new Vector3[]
                {
                    Vector3.Lerp(start, end, 0.5f) + Vector3.up * height,
                    end,
                };
        }
    }

    private static TrajectorySettings PickTrajectory(TrajectorySettings[] trajectories)
    {
        if (null == trajectories || 0 == trajectories.Length)
        {
            return null;
        }

        float totalWeight = 0f;

        for (int i = 0; i < trajectories.Length; i++)
        {
            totalWeight += trajectories[i].Weight;
        }

        if (0f >= totalWeight)
        {
            return trajectories[0];
        }

        float remainingWeight = Random.value * totalWeight;

        for (int i = 0; i < trajectories.Length; i++)
        {
            remainingWeight -= trajectories[i].Weight;

            if (0f >= remainingWeight)
            {
                return trajectories[i];
            }
        }

        return trajectories[trajectories.Length - 1];
    }
}
