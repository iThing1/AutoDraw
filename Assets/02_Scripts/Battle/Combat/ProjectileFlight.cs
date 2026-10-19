using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

// 보통 탄을 풀에서 꺼내 크기·색·꼬리를 맞추고, 궤적을 골라 날린 뒤 돌려준다. 어떻게 나는지는 TrajectoryMotion이 맡는다.
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

        FlightResult result = await projectile.GetOrAddComponent<TrajectoryMotion>().FlyAsync(shot, trajectory, start, end, target, token);

        GameManager.Pool.Despawn(projectile);

        return result;
    }

    private static void SetupTrail(TrailRenderer trail, ShotSettings shot, Color color)
    {
        SetupTrail(trail, shot.TrailTime, shot.TrailWidth, shot.UseTrailColor ? shot.TrailColor : color);
    }

    // 같은 투사체 프리팹을 여러 공격이 함께 쓰므로, 꺼낼 때마다 꼬리를 그 공격의 값으로 다시 맞춘다. 끝으로 갈수록 투명해진다.
    public static void SetupTrail(TrailRenderer trail, float time, float width, Color color)
    {
        // 풀에서 다시 꺼낸 투사체에는 이전 비행의 꼬리가 남아 있어, 그대로 두면 이전 위치에서 새 출발점까지 선이 그어진다.
        trail.Clear();

        trail.emitting = 0f < time;
        trail.time = time;
        trail.widthMultiplier = width;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
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
