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

        // 큰 탄은 가장자리가 먼저 방패에 닿으므로, 막기 판정에 탄의 반지름을 더한다.
        float projectileRadius = 0f;

        if (projectile.TryGetComponent(out SpriteRenderer projectileRenderer))
        {
            projectileRenderer.color = color;
            projectileRadius = projectileRenderer.bounds.extents.x;
        }

        if (projectile.TryGetComponent(out TrailRenderer trail))
        {
            SetupTrail(trail, shot, color);
        }

        float height = trajectory.Height.PickRandom();
        float duration = trajectory.PickDuration(Vector3.Distance(start, end));

        Transform projectileTransform = projectile.transform;

        Tween flight = CreateFlightTween(projectileTransform, trajectory.Type, start, end, height, duration).SetEase(Ease.Linear);

        // 날아오는 도중 방패나 몸에 닿으면 Blocked·Arrived가 되고, 닿지 않은 동안은 Missed로 둔다.
        FlightOutcome contactOutcome = FlightOutcome.Missed;
        Vector3 contactPoint = end;
        float flightSpeed = 0f < duration ? Vector3.Distance(start, end) / duration : 0f;
        Vector3 lastPosition = start;
        Vector3 velocity = 0f < flightSpeed ? (end - start).normalized * flightSpeed : Vector3.zero;

        if (null != target)
        {
            Transform followTarget = target.FollowTransform;
            Vector3 targetStartPosition = followTarget.position;
            Vector3 lastTargetPosition = targetStartPosition;
            float homing = shot.Homing;
            float maxHomingSpeed = flightSpeed * homing;
            Vector3 homingOffset = Vector3.zero;

            // 보정이 따라가는 속도를 탄 속도 × 추적 강도로 묶어, 목표가 갑자기 움직여도 탄이 홱 꺾이지 않게 한다.
            // 도착 지점만 보면 방패를 가로질러 지나가도 막히지 않아, 날아오는 동안 매 프레임 방패에 닿았는지 본다.
            flight = flight.OnUpdate(() =>
            {
                float slowScale = ApplySlowZone(flight, projectileTransform);
                Vector3 desiredOffset = (followTarget.position - targetStartPosition) * (flight.ElapsedPercentage() * homing);
                homingOffset = Vector3.MoveTowards(homingOffset, desiredOffset, maxHomingSpeed * Time.deltaTime * slowScale);
                projectileTransform.position += homingOffset;

                Vector3 currentPosition = projectileTransform.position;

                // 빗나간 뒤 같은 속도로 이어 가도록 실제 속도를 기억한다. 마지막 프레임은 덜 움직여 넘긴다.
                // 느려지는 구역 안에서 잰 속도는 배율로 되돌려, 구역을 벗어난 뒤에는 원래 속도로 날아가게 한다.
                if (0f < Time.deltaTime && 0f < slowScale && 1f > flight.ElapsedPercentage())
                {
                    velocity = (currentPosition - lastPosition) / (Time.deltaTime * slowScale);
                }

                contactOutcome = CheckContact(target, lastPosition, currentPosition, lastTargetPosition, projectileRadius, out contactPoint);
                lastPosition = currentPosition;
                lastTargetPosition = followTarget.position;

                if (FlightOutcome.Missed != contactOutcome)
                {
                    flight.Kill();
                }
            });
        }
        else
        {
            flight = flight.OnUpdate(() => ApplySlowZone(flight, projectileTransform));
        }

        // 전투가 끝나 토큰이 취소되면 트윈을 멈추고, 예외를 던지는 대신 isCanceled를 true로 돌려받는다.
        bool isCanceled = await flight.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token).SuppressCancellationThrow();

        Vector3 landingPoint = FlightOutcome.Missed != contactOutcome ? contactPoint : projectileTransform.position;
        FlightOutcome outcome = GetOutcome(contactOutcome, isCanceled, target, landingPoint);

        // 도착 지점에서 비켜난 목표가 탄이 지나갈 길 위에 있을 수 있어, 지나가는 동안에도 닿았는지 본다.
        if (FlightOutcome.Missed == outcome)
        {
            FlightResult pastResult = await FlyPastAsync(projectileTransform, velocity, shot.MissTravelTime, target, projectileRadius, token);
            outcome = pastResult.Outcome;

            if (FlightOutcome.Missed != outcome)
            {
                landingPoint = pastResult.LandingPoint;
            }
        }

        GameManager.Pool.Despawn(projectile);

        return new FlightResult(outcome, landingPoint);
    }

    // 탄과 목표가 같은 프레임에 함께 움직이면 서로 엇갈려 지나갈 수 있다.
    // 목표 기준으로 본 탄의 이동 선분에서 몸에 가장 가까운 점으로 판정하고, 닿지 않았으면 Missed를 돌려준다.
    private static FlightOutcome CheckContact(AttackTarget target, Vector3 from, Vector3 to, Vector3 targetFrom, float projectileRadius,
        out Vector3 contactPoint)
    {
        Vector3 targetTo = target.FollowTransform.position;
        contactPoint = targetTo + GetClosestPointOnSegment(from - targetFrom, to - targetTo, Vector3.zero);

        if (target.IsBlockedAt(contactPoint, projectileRadius))
        {
            return FlightOutcome.Blocked;
        }

        if (target.IsTouching(contactPoint, projectileRadius))
        {
            return FlightOutcome.Arrived;
        }

        return FlightOutcome.Missed;
    }

    // 방패나 몸에 닿아 직접 멈춘 경우는 트윈이 취소로 끝나더라도 닿은 것으로 본다.
    private static FlightOutcome GetOutcome(FlightOutcome contactOutcome, bool isCanceled, AttackTarget target, Vector3 landingPoint)
    {
        if (FlightOutcome.Missed != contactOutcome)
        {
            return contactOutcome;
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

    private static Vector3 GetClosestPointOnSegment(Vector3 from, Vector3 to, Vector3 point)
    {
        Vector3 segment = to - from;
        float sqrLength = segment.sqrMagnitude;

        if (0f >= sqrLength)
        {
            return from;
        }

        float t = Mathf.Clamp01(Vector3.Dot(point - from, segment) / sqrLength);

        return from + segment * t;
    }

    // 외곽선 같은 자식까지 함께 사라지도록 색 대신 크기를 줄인다.
    private static async UniTask<FlightResult> FlyPastAsync(Transform projectile, Vector3 velocity, float travelTime, AttackTarget target,
        float projectileRadius, CancellationToken token)
    {
        if (0f >= travelTime)
        {
            return new FlightResult(FlightOutcome.Missed, projectile.position);
        }

        Vector3 destination = projectile.position + velocity * travelTime;
        FlightOutcome contactOutcome = FlightOutcome.Missed;
        Vector3 contactPoint = destination;

        Sequence pass = DOTween.Sequence()
            .Join(projectile.DOMove(destination, travelTime).SetEase(Ease.Linear))
            .Join(projectile.DOScale(0f, travelTime).SetEase(Ease.InQuad));

        if (null != target)
        {
            Vector3 lastPosition = projectile.position;
            Vector3 lastTargetPosition = target.FollowTransform.position;

            pass = pass.OnUpdate(() =>
            {
                ApplySlowZone(pass, projectile);

                Vector3 currentPosition = projectile.position;

                contactOutcome = CheckContact(target, lastPosition, currentPosition, lastTargetPosition, projectileRadius, out contactPoint);
                lastPosition = currentPosition;
                lastTargetPosition = target.FollowTransform.position;

                if (FlightOutcome.Missed != contactOutcome)
                {
                    pass.Kill();
                }
            });
        }
        else
        {
            pass = pass.OnUpdate(() => ApplySlowZone(pass, projectile));
        }

        bool isCanceled = await pass.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token).SuppressCancellationThrow();

        if (FlightOutcome.Missed != contactOutcome)
        {
            return new FlightResult(contactOutcome, contactPoint);
        }

        return new FlightResult(isCanceled ? FlightOutcome.Canceled : FlightOutcome.Missed, projectile.position);
    }

    // 느려지는 구역 안에 있는 동안만 이 탄의 트윈을 늦춘다. 게임 전체 시간은 건드리지 않는다.
    private static float ApplySlowZone(Tween tween, Transform projectile)
    {
        float scale = GameManager.SlowZones.GetProjectileScale(projectile.position);
        tween.timeScale = scale;

        return scale;
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

    // 경로 트윈은 탄마다 경로를 새로 만들어 탄막에서 할당이 몰리므로, 경유점이 없는 직선은 위치 트윈으로 날린다.
    private static Tween CreateFlightTween(Transform projectile, TrajectoryType type, Vector3 start, Vector3 end, float height, float duration)
    {
        if (TrajectoryType.Straight == type)
        {
            return projectile.DOMove(end, duration);
        }

        return projectile.DOPath(BuildPath(type, start, end, height), duration, PathType.CatmullRom);
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
