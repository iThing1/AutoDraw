using Cysharp.Threading.Tasks;
using DG.Tweening;
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
            pool.Prewarm(drop.Prefab, count * (0f < drop.Linger ? 3 : 2));
        }
    }

    public static async UniTask PlayAsync(PoolManager pool, FeedbackManager feedback, AttackContext context, GroundDropSettings drop, Vector3 point,
        CancellationToken token)
    {
        await Telegraph.PlayCircleAsync(pool, drop.Prefab, point, drop.Radius, drop.TelegraphColor, drop.WarnDuration, token);

        feedback.PlayImpact(drop.Impact, point);
        context.Damage.Blast(drop.Hit, point, drop.Radius, 0f);

        if (0f < drop.Linger && null != drop.Prefab)
        {
            await LingerAsync(pool, feedback, context, drop, point, token);
        }
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

    // 터지는 순간 맞은 뒤 곧바로 또 맞지 않도록, 첫 웅덩이 피해는 한 간격 뒤부터 준다.
    private static async UniTask LingerAsync(PoolManager pool, FeedbackManager feedback, AttackContext context, GroundDropSettings drop, Vector3 point,
        CancellationToken token)
    {
        AttackTarget target = context.Target;
        float diameter = drop.Radius * 2f;
        SpriteRenderer puddle = pool.SpawnSprite(drop.Prefab, point, 0f, new Vector2(diameter, diameter), drop.PuddleColor);
        float elapsed = 0f;
        float nextHitTime = drop.TouchInterval;
        float lastHitTime = float.NegativeInfinity;

        try
        {
            while (elapsed < drop.Linger && false == target.IsDefeated)
            {
                if (elapsed >= nextHitTime && target.IsTouching(point, drop.Radius))
                {
                    nextHitTime = elapsed + drop.TouchInterval;
                    lastHitTime = elapsed;

                    Vector3 hitPoint = target.GetAimPoint(Vector3.zero);
                    feedback.PlayImpact(drop.TouchImpact, hitPoint);
                    context.Damage.Strike(drop.TouchHit, hitPoint, 0f);
                }

                float flash = 1f - Mathf.Clamp01((elapsed - lastHitTime) / drop.TouchInterval);
                puddle.color = Color.Lerp(drop.PuddleColor, drop.FlashColor, flash);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }

            await puddle.DOFade(0f, drop.FadeDuration).ToUniTaskKillOnCancel(token);
        }
        finally
        {
            pool.DespawnSprite(puddle);
        }
    }
}
