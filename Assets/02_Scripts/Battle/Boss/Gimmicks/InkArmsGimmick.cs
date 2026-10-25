using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 잉크 팔에서 버티기. 종이 곳곳의 먹물 웅덩이에서 그림자 팔이 플레이어를 앞질러 노리며 뻗어 나온다. 끝까지 버티면 그로기로 이어진다.
[CreateAssetMenu(fileName = "InkArmsGimmick", menuName = "Scriptable Objects/Boss/Gimmick/InkArms")]
public class InkArmsGimmick : BossGimmick
{
    private const int FALLBACK_POOL_TRIES = 5;

    [Header("먹물 흩뿌리기")]
    // 플레이어 둘레 이 반경 안에 웅덩이를 흩뿌린다. 앞뒤 어디로 달려도 근처에 웅덩이가 있도록 넉넉히 깐다.
    [SerializeField, Min(1)] private int _poolCount = 16;
    [SerializeField, Min(0f)] private float _poolScatterRadius = 18f;
    [SerializeField] private SplatStyle _poolStyle;
    [SerializeField] private FloatRange _poolScale = new FloatRange(1.6f, 2.6f);
    [SerializeField, Min(0f)] private float _poolInterval = 0.04f;
    [SerializeField, Min(0f)] private float _poolSettleDuration = 0.5f;

    [Header("시간")]
    [SerializeField, Min(0f)] private float _duration = 8f;
    [SerializeField, Min(0.05f)] private float _lungeInterval = 0.7f;
    [SerializeField, Min(0f)] private float _retractDuration = 0.35f;

    [Header("노리기")]
    [SerializeField] private FloatRange _sourceDistance = new FloatRange(3f, 12f);
    // 팔이 다 뻗을 때 플레이어가 있을 자리를 이 시간(초)만큼 앞질러 노린다.
    [SerializeField, Min(0f)] private float _leadTime = 0.35f;
    // 기어 오는 동안 붙잡을 자리가 플레이어를 따라가는 빠르기(초당 거리).
    [SerializeField, Min(0f)] private float _homingSpeed = 4.5f;

    [Header("팔")]
    [SerializeField] private IntRange _armsPerLunge = new IntRange(1, 2);
    // 팔의 모양·기어 오는 빠르기·꿈틀거림은 먹물 삼킴 연출의 값을 같이 쓴다.
    [SerializeField] private InkSwallowSettings _arms = new();
    [SerializeField] private HitSettings _grabHit = new();

    public override void Prewarm()
    {
        if (null != _arms.ArmPrefab)
        {
            int lungeCount = Mathf.CeilToInt((_arms.SourceTwitchDuration + _sourceDistance.Max / _arms.CrawlSpeed + _retractDuration) / _lungeInterval) + 1;

            GameManager.Pool.Prewarm(_arms.ArmPrefab.gameObject, lungeCount * _armsPerLunge.Max);
        }
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        if (null == _arms.ArmPrefab || null == context.PaintCanvas)
        {
            Debug.LogError($"잉크 팔 : {name}에 팔 프리팹이 없거나 물감 캔버스를 찾을 수 없습니다.");
            return true;
        }

        List<PaintMark> pools = new List<PaintMark>(_poolCount);
        List<PaintMark> sources = new List<PaintMark>(_armsPerLunge.Max);
        List<UniTask> lunges = new List<UniTask>();
        float elapsed = 0f;

        try
        {
            if (null != context.Hands)
            {
                context.Hands.PoseAsync(HandGesture.BothRaise, context.Target.GetAimPoint(Vector3.zero), _poolSettleDuration, token).Forget();
            }

            await ScatterPoolsAsync(context, pools, token);

            if (null != context.Hands)
            {
                context.Hands.PoseAsync(HandGesture.BothPress, context.Target.GetAimPoint(Vector3.zero), _lungeInterval, token).Forget();
            }

            while (elapsed < _duration && false == context.Target.IsDefeated)
            {
                PickSources(context, pools, _armsPerLunge.PickRandom(), sources);

                for (int i = 0; i < sources.Count; i++)
                {
                    lunges.Add(LungeAsync(context, sources[i], token));
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_lungeInterval), cancellationToken: token);
                elapsed += _lungeInterval;
            }

            await UniTask.WhenAll(lunges);
        }
        finally
        {
            if (null != context.Hands)
            {
                context.Hands.ReturnToRest();
            }
        }

        return true;
    }

    private async UniTask ScatterPoolsAsync(AttackContext context, List<PaintMark> pools, CancellationToken token)
    {
        Vector3 center = context.Target.FollowTransform.position;

        for (int i = 0; i < _poolCount; i++)
        {
            Vector3 position = center + (Vector3)(UnityEngine.Random.insideUnitCircle * _poolScatterRadius);
            PaintMark pool = context.PaintCanvas.LeaveInkSplat(position, _poolStyle, _arms.InkColor, _poolScale.PickRandom());

            if (null != pool)
            {
                pools.Add(pool);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_poolInterval), cancellationToken: token);
        }

        await UniTask.Delay(TimeSpan.FromSeconds(_poolSettleDuration), cancellationToken: token);
    }

    private async UniTask LungeAsync(AttackContext context, PaintMark source, CancellationToken token)
    {
        context.PaintCanvas.Twitch(source, _arms.SourceTwitchStrength, _arms.SourceTwitchDuration, false);

        await UniTask.Delay(TimeSpan.FromSeconds(_arms.SourceTwitchDuration), cancellationToken: token);

        AttackTarget target = context.Target;
        Vector3 root = source.Position;
        Vector3 grabPoint = target.GetPredictedPoint(_leadTime);

        InkArm arm = GameManager.Pool.Spawn(_arms.ArmPrefab.gameObject, root, Quaternion.identity).GetComponent<InkArm>();
        arm.Setup(root, grabPoint, 0f, false, _arms);

        SwallowFrame frame = new SwallowFrame { Remain = 1f };
        float arrivalTime = arm.ArrivalTime;
        float endTime = arrivalTime + _arms.GrabDuration + _retractDuration;
        bool hasStruck = false;

        try
        {
            while (frame.Elapsed < endTime)
            {
                frame.Elapsed += Time.deltaTime;
                frame.Grab = SwallowFrame.GetProgress(frame.Elapsed, arrivalTime, _arms.GrabDuration);
                frame.PullProgress = SwallowFrame.GetProgress(frame.Elapsed, arrivalTime + _arms.GrabDuration, _retractDuration);

                if (frame.Elapsed < arrivalTime)
                {
                    grabPoint = Vector3.MoveTowards(grabPoint, target.GetAimPoint(Vector3.zero), _homingSpeed * Time.deltaTime);
                    arm.SetGrabPoint(grabPoint);
                }
                else if (false == hasStruck)
                {
                    hasStruck = true;
                    context.Damage.Strike(_grabHit, grabPoint, 0f);
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

    // 덮이거나 지워진 웅덩이는 빼고, 플레이어와 알맞게 떨어진 웅덩이를 무작위 순서로 count개까지 고른다.
    // 없으면 너무 가깝지 않은 것 중 가장 가까운 것을, 그것도 없으면 알맞은 거리에 웅덩이를 새로 찍어 쓴다.
    private void PickSources(AttackContext context, List<PaintMark> pools, int count, List<PaintMark> result)
    {
        result.Clear();
        RemoveHiddenPools(context.PaintCanvas, pools);

        Vector3 center = context.Target.FollowTransform.position;
        int startIndex = UnityEngine.Random.Range(0, pools.Count);
        PaintMark nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < pools.Count && result.Count < count; i++)
        {
            PaintMark pool = pools[(startIndex + i) % pools.Count];
            float distance = Vector2.Distance(pool.Position, center);

            if (_sourceDistance.Min <= distance && _sourceDistance.Max >= distance)
            {
                result.Add(pool);
            }
            else if (_sourceDistance.Min <= distance && distance < nearestDistance)
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
            PaintMark pool = LeavePoolAround(context, center);

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
    private PaintMark LeavePoolAround(AttackContext context, Vector3 center)
    {
        Vector3 position = center;

        for (int i = 0; i < FALLBACK_POOL_TRIES; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            position = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _sourceDistance.PickRandom();

            if (false == context.PaintCanvas.IsPaintedAt(position, 0f))
            {
                break;
            }
        }

        return context.PaintCanvas.LeaveInkSplat(position, _poolStyle, _arms.InkColor, _poolScale.PickRandom());
    }
}
