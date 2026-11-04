using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 바닥에 원 예고를 띄우고 터뜨린다. 여러 자리를 한꺼번에 떨어뜨리면 하나에라도 들어 있을 때 한 번만 맞는다.
public static class GroundDrop
{
    public static void Prewarm(PoolManager pool, GroundDropSettings drop, int count)
    {
        if (null != drop.Prefab)
        {
            pool.Prewarm(drop.Prefab, count * 2);
        }
    }

    public static async UniTask PlayAsync(PoolManager pool, FeedbackManager feedback, AttackContext context, GroundDropSettings drop, Vector3 point,
        CancellationToken token)
    {
        await Telegraph.PlayCircleAsync(pool, drop.Prefab, point, drop.Radius, drop.TelegraphColor, drop.WarnDuration, token);

        feedback.PlayImpact(drop.Impact, point);
        context.Damage.Blast(drop.Hit, point, drop.Radius, 0f);
    }

    public static async UniTask PlayAsync(PoolManager pool, FeedbackManager feedback, AttackContext context, GroundDropSettings drop,
        IReadOnlyList<Vector3> points, CancellationToken token)
    {
        await Telegraph.PlayCirclesAsync(pool, drop.Prefab, points, drop.Radius, drop.TelegraphColor, drop.WarnDuration, token);

        AttackTarget target = context.Target;

        feedback.PlayImpact(drop.Impact, points[0]);

        if (target.IsInAnyBlast(points, drop.Radius))
        {
            context.Damage.Strike(drop.Hit, target.GetAimPoint(Vector3.zero), 0f);
        }
    }
}
