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

    public static async UniTask<FlightResult> FlyAsync(GameObject prefab, ShotSettings shot, Vector3 start, Vector3 end, Transform followTarget,
        Color color, CancellationToken token)
    {
        TrajectorySettings trajectory = PickTrajectory(shot.Trajectories);

        if (null == trajectory)
        {
            Debug.LogError($"투사체 비행 : {prefab.name}에 사용할 수 있는 궤적이 없습니다.");
            return new FlightResult(false, start);
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

        if (null != followTarget && 0f < shot.Homing)
        {
            Vector3 targetStartPosition = followTarget.position;
            float homing = shot.Homing;

            // 목표가 움직인 거리를 진행률과 추적 강도만큼 더한다. 추적 강도가 1이면 도착하는 순간 목표에 붙는다.
            flight = flight.OnUpdate(() =>
            {
                Vector3 targetMoved = followTarget.position - targetStartPosition;
                projectileTransform.position += targetMoved * (flight.ElapsedPercentage() * homing);
            });
        }

        // 전투가 끝나 토큰이 취소되면 트윈을 멈추고, 예외를 던지는 대신 isCanceled를 true로 돌려받는다.
        bool isCanceled = await flight.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token).SuppressCancellationThrow();

        Vector3 landingPoint = projectileTransform.position;

        GameManager.Pool.Despawn(projectile);

        return new FlightResult(false == isCanceled, landingPoint);
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
