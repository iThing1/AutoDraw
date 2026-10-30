using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 목표 둘레에 먹물 웅덩이를 흩뿌리고, 정해진 시간 동안 웅덩이에서 그림자 팔을 뻗어 목표를 붙잡는다.
// 손 자세처럼 쓰는 쪽마다 다른 연출은 부르는 쪽이 두 단계 사이에 끼운다.
public static class InkArmsRun
{
    private const int FALLBACK_POOL_TRIES = 5;

    public static void Prewarm(InkArmsSettings settings)
    {
        InkSwallowSettings arms = settings.Arms;

        if (null == arms.ArmPrefab)
        {
            return;
        }

        int lungeCount = Mathf.CeilToInt((arms.SourceTwitchDuration + settings.SourceDistance.Max / arms.CrawlSpeed + settings.RetractDuration) / settings.LungeInterval) + 1;

        GameManager.Pool.Prewarm(arms.ArmPrefab.gameObject, lungeCount * settings.ArmsPerLunge.Max);
    }

    // 팔 프리팹이나 물감 캔버스가 없으면 뻗을 수 없다.
    public static bool CanRun(AttackContext context, InkArmsSettings settings)
    {
        return null != settings.Arms.ArmPrefab && null != context.PaintCanvas;
    }

    public static async UniTask ScatterPoolsAsync(AttackContext context, InkArmsSettings settings, List<PaintMark> pools, CancellationToken token)
    {
        Vector3 center = context.Target.FollowTransform.position;

        for (int i = 0; i < settings.PoolCount; i++)
        {
            Vector3 position = center + (Vector3)(UnityEngine.Random.insideUnitCircle * settings.PoolScatterRadius);
            PaintMark pool = context.PaintCanvas.LeaveInkSplat(position, settings.PoolStyle, settings.Arms.InkColor, settings.PoolScale.PickRandom());

            if (null != pool)
            {
                pools.Add(pool);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(settings.PoolInterval), cancellationToken: token);
        }

        await UniTask.Delay(TimeSpan.FromSeconds(settings.PoolSettleDuration), cancellationToken: token);
    }

    // 시간이 다 되거나 목표가 쓰러질 때까지 간격마다 팔을 뻗고, 마지막 팔이 거둬질 때까지 기다린다.
    public static async UniTask LungeAsync(AttackContext context, InkArmsSettings settings, List<PaintMark> pools, float gaugeGain, CancellationToken token)
    {
        List<PaintMark> sources = new List<PaintMark>(settings.ArmsPerLunge.Max);
        List<UniTask> lunges = new List<UniTask>();
        float elapsed = 0f;

        while (elapsed < settings.Duration && false == context.Target.IsDefeated)
        {
            PickSources(context, settings, pools, settings.ArmsPerLunge.PickRandom(), sources);

            for (int i = 0; i < sources.Count; i++)
            {
                lunges.Add(LungeArmAsync(context, settings, sources[i], gaugeGain, token));
            }

            await UniTask.Delay(TimeSpan.FromSeconds(settings.LungeInterval), cancellationToken: token);
            elapsed += settings.LungeInterval;
        }

        await UniTask.WhenAll(lunges);
    }

    private static async UniTask LungeArmAsync(AttackContext context, InkArmsSettings settings, PaintMark source, float gaugeGain, CancellationToken token)
    {
        InkSwallowSettings arms = settings.Arms;

        context.PaintCanvas.Twitch(source, arms.SourceTwitchStrength, arms.SourceTwitchDuration, false);

        await UniTask.Delay(TimeSpan.FromSeconds(arms.SourceTwitchDuration), cancellationToken: token);

        AttackTarget target = context.Target;
        Vector3 root = source.Position;
        Vector3 grabPoint = target.GetPredictedPoint(settings.LeadTime);

        InkArm arm = GameManager.Pool.Spawn(arms.ArmPrefab.gameObject, root, Quaternion.identity).GetComponent<InkArm>();
        arm.Setup(root, grabPoint, 0f, false, arms);

        SwallowFrame frame = new SwallowFrame { Remain = 1f };
        float arrivalTime = arm.ArrivalTime;
        float endTime = arrivalTime + arms.GrabDuration + settings.RetractDuration;
        bool hasStruck = false;

        try
        {
            while (frame.Elapsed < endTime)
            {
                frame.Elapsed += Time.deltaTime;
                frame.Grab = SwallowFrame.GetProgress(frame.Elapsed, arrivalTime, arms.GrabDuration);
                frame.PullProgress = SwallowFrame.GetProgress(frame.Elapsed, arrivalTime + arms.GrabDuration, settings.RetractDuration);

                if (frame.Elapsed < arrivalTime)
                {
                    grabPoint = Vector3.MoveTowards(grabPoint, target.GetAimPoint(Vector3.zero), settings.HomingSpeed * Time.deltaTime);
                    arm.SetGrabPoint(grabPoint);
                }
                else if (false == hasStruck)
                {
                    hasStruck = true;
                    context.Damage.Strike(settings.GrabHit, grabPoint, gaugeGain);
                }

                arm.Tick(frame);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            GameManager.Pool.Despawn(arm.gameObject);
        }
    }

    // 덮이거나 지워진 웅덩이는 빼고, 목표와 알맞게 떨어진 웅덩이를 무작위 순서로 count개까지 고른다.
    // 없으면 너무 가깝지 않은 것 중 가장 가까운 것을, 그것도 없으면 알맞은 거리에 웅덩이를 새로 찍어 쓴다.
    private static void PickSources(AttackContext context, InkArmsSettings settings, List<PaintMark> pools, int count, List<PaintMark> result)
    {
        result.Clear();
        RemoveHiddenPools(context.PaintCanvas, pools);

        FloatRange sourceDistance = settings.SourceDistance;
        Vector3 center = context.Target.FollowTransform.position;
        int startIndex = UnityEngine.Random.Range(0, pools.Count);
        PaintMark nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < pools.Count && result.Count < count; i++)
        {
            PaintMark pool = pools[(startIndex + i) % pools.Count];
            float distance = Vector2.Distance(pool.Position, center);

            if (sourceDistance.Min <= distance && sourceDistance.Max >= distance)
            {
                result.Add(pool);
            }
            else if (sourceDistance.Min <= distance && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = pool;
            }
        }

        if (0 == result.Count && null != nearest)
        {
            result.Add(nearest);
        }

        if (0 == result.Count)
        {
            PaintMark pool = LeavePoolAround(context, settings, center);

            if (null != pool)
            {
                pools.Add(pool);
                result.Add(pool);
            }
        }
    }

    private static void RemoveHiddenPools(PaintCanvas canvas, List<PaintMark> pools)
    {
        for (int i = pools.Count - 1; i >= 0; i--)
        {
            if (false == canvas.IsInkExposed(pools[i]))
            {
                pools.RemoveAt(i);
            }
        }
    }

    // 칠해지지 않은 자리를 몇 번 찾아보고, 못 찾으면 마지막에 고른 자리에 찍는다.
    private static PaintMark LeavePoolAround(AttackContext context, InkArmsSettings settings, Vector3 center)
    {
        Vector3 position = center;

        for (int i = 0; i < FALLBACK_POOL_TRIES; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            position = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * settings.SourceDistance.PickRandom();

            if (false == context.PaintCanvas.IsPaintedAt(position, 0f))
            {
                break;
            }
        }

        return context.PaintCanvas.LeaveInkSplat(position, settings.PoolStyle, settings.Arms.InkColor, settings.PoolScale.PickRandom());
    }
}
