using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

public static class ProjectileFlight
{
    public static UniTask<bool> FlyAsync(GameObject prefab, ShotSettings shot, Vector3 start, Vector3 end, Color color, CancellationToken token)
    {
        return FlyAsync(prefab, shot, start, end, null, color, token);
    }

    // 도착하면 true, 전투가 끝나 중간에 취소되면 false를 돌려준다.
    public static async UniTask<bool> FlyAsync(GameObject prefab, ShotSettings shot, Vector3 start, Vector3 end, Transform followTarget,
        Color color, CancellationToken token)
    {
        TrajectorySettings trajectory = PickTrajectory(shot.Trajectories);

        if (null == trajectory)
        {
            Debug.LogError($"투사체 비행 : {prefab.name}에 사용할 수 있는 궤적이 없습니다.");
            return false;
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

        float height = trajectory.Height.Pick();
        float duration = trajectory.Duration.Pick();

        Transform projectileTransform = projectile.transform;

        Tween flight = projectileTransform.DOPath(BuildPath(trajectory.Type, start, end, height), duration, PathType.CatmullRom)
            .SetEase(Ease.Linear);

        if (null != followTarget)
        {
            Vector3 targetStartPosition = followTarget.position;

            // 궤적 모양은 쏘는 순간 정해 두고, 목표가 그동안 움직인 거리를 진행률만큼 더해 도착하는 순간 목표에 붙게 한다.
            flight = flight.OnUpdate(() =>
            {
                Vector3 targetMoved = followTarget.position - targetStartPosition;
                projectileTransform.position += targetMoved * flight.ElapsedPercentage();
            });
        }

        // 전투가 끝나 토큰이 취소되면 트윈을 멈추고, 예외를 던지는 대신 isCanceled를 true로 돌려받는다.
        bool isCanceled = await flight.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token).SuppressCancellationThrow();

        GameManager.Pool.Despawn(projectile);

        return false == isCanceled;
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

        float pick = Random.value * totalWeight;

        for (int i = 0; i < trajectories.Length; i++)
        {
            pick -= trajectories[i].Weight;

            if (0f >= pick)
            {
                return trajectories[i];
            }
        }

        return trajectories[trajectories.Length - 1];
    }
}
