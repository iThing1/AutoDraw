using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 잉크 팔에서 버티기. 보스가 먼저 종이 곳곳에 먹물 웅덩이를 흩뿌리고, 정해진 시간 동안 웅덩이에서 그림자 팔이 한두 개씩 뻗어 나온다.
// 팔은 플레이어가 갈 자리를 앞질러 노리고 기어 오는 동안에도 따라와, 한 방향으로 달리기만 해서는 피할 수 없다.
// 붙잡히면 그때마다 피해를 입을 뿐 따로 실패는 없고, 끝까지 버티면 그로기로 이어진다. 팔 모양은 패배 연출의 팔을 같이 쓴다.
[CreateAssetMenu(fileName = "InkArmsGimmick", menuName = "Scriptable Objects/Boss/Gimmick/InkArms")]
public class InkArmsGimmick : BossGimmick
{
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
    // 팔이 닿은 뒤 다시 잉크로 되감기는 시간.
    [SerializeField, Min(0f)] private float _retractDuration = 0.35f;

    [Header("노리기")]
    // 이 거리 범위의 웅덩이에서 팔이 나온다.
    [SerializeField] private FloatRange _sourceDistance = new FloatRange(3f, 12f);
    // 팔이 다 뻗을 때 플레이어가 있을 자리를 이 시간(초)만큼 앞질러 노린다.
    [SerializeField, Min(0f)] private float _leadTime = 0.35f;
    // 기어 오는 동안 붙잡을 자리가 플레이어를 따라가는 빠르기(초당 거리).
    [SerializeField, Min(0f)] private float _homingSpeed = 4.5f;

    [Header("팔")]
    // 한 번에 서로 다른 웅덩이에서 나오는 팔 수.
    [SerializeField] private IntRange _armsPerLunge = new IntRange(1, 2);
    // 팔의 모양·기어 오는 빠르기·꿈틀거림은 먹물 삼킴 연출의 값을 같이 쓴다.
    [SerializeField] private InkSwallowSettings _arms = new();
    [SerializeField] private ShotSettings _grabHit = new();

    public override void Prewarm()
    {
        if (null != _arms.ArmPrefab)
        {
            int lungeCount = Mathf.CeilToInt((_arms.SourceTwitchDuration + _sourceDistance.Max / _arms.CrawlSpeed + _retractDuration) / _lungeInterval) + 1;

            GameManager.Pool.Prewarm(_arms.ArmPrefab.gameObject, lungeCount * _armsPerLunge.Max);
        }
    }

    public override async UniTask<bool> PlayAsync(BattleUnit boss, CancellationToken token)
    {
        if (null == _arms.ArmPrefab || null == boss.Attacker.PaintCanvas)
        {
            Debug.LogError($"잉크 팔 : {name}에 팔 프리팹이 없거나 물감 캔버스를 찾을 수 없습니다.");
            return true;
        }

        AutoAttacker attacker = boss.Attacker;
        List<Transform> pools = new List<Transform>(_poolCount);
        List<Transform> sources = new List<Transform>(_armsPerLunge.Max);
        List<UniTask> lunges = new List<UniTask>();
        float elapsed = 0f;

        try
        {
            if (null != attacker.Hands)
            {
                attacker.Hands.PoseAsync(HandGesture.BothRaise, attacker.Target.GetAimPoint(Vector3.zero), _poolSettleDuration, token).Forget();
            }

            await ScatterPoolsAsync(attacker, pools, token);

            if (null != attacker.Hands)
            {
                attacker.Hands.PoseAsync(HandGesture.BothPress, attacker.Target.GetAimPoint(Vector3.zero), _lungeInterval, token).Forget();
            }

            while (elapsed < _duration && false == attacker.Target.IsDefeated)
            {
                PickSources(attacker, pools, _armsPerLunge.PickRandom(), sources);

                for (int i = 0; i < sources.Count; i++)
                {
                    lunges.Add(LungeAsync(attacker, sources[i], token));
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_lungeInterval), cancellationToken: token);
                elapsed += _lungeInterval;
            }

            await UniTask.WhenAll(lunges);
        }
        finally
        {
            if (null != attacker.Hands)
            {
                attacker.Hands.ReturnToRest();
            }
        }

        return true;
    }

    // 플레이어 둘레 곳곳에 웅덩이를 하나씩 찍는다. 이 웅덩이들이 팔이 나올 자리다.
    private async UniTask ScatterPoolsAsync(AutoAttacker attacker, List<Transform> pools, CancellationToken token)
    {
        Vector3 center = attacker.Target.FollowTransform.position;

        for (int i = 0; i < _poolCount; i++)
        {
            Vector3 position = center + (Vector3)(UnityEngine.Random.insideUnitCircle * _poolScatterRadius);
            PaintSplat pool = attacker.PaintCanvas.LeaveInkSplat(position, _poolStyle, _arms.InkColor, _poolScale.PickRandom());

            if (null != pool)
            {
                pools.Add(pool.transform);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_poolInterval), cancellationToken: token);
        }

        await UniTask.Delay(TimeSpan.FromSeconds(_poolSettleDuration), cancellationToken: token);
    }

    // 웅덩이가 꿈틀한 뒤 팔이 뻗어 나온다. 다 뻗은 순간 붙잡을 자리에 있으면 붙잡힌다.
    private async UniTask LungeAsync(AutoAttacker attacker, Transform source, CancellationToken token)
    {
        TwitchSource(source);

        await UniTask.Delay(TimeSpan.FromSeconds(_arms.SourceTwitchDuration), cancellationToken: token);

        AttackTarget target = attacker.Target;
        Vector3 root = source.position;
        Vector3 grabPoint = target.GetPredictedPoint(_leadTime);

        InkArm arm = GameManager.Pool.Spawn(_arms.ArmPrefab.gameObject, root, Quaternion.identity).GetComponent<InkArm>();
        arm.Setup(root, grabPoint, 0f, false, _arms);

        SwallowFrame frame = new SwallowFrame { Remain = 1f };
        float arrivalTime = arm.ArrivalTime;
        float endTime = arrivalTime + _arms.GrabDuration + _retractDuration;
        bool hasStruck = false;

        try
        {
            // 게임 시간으로 돌아 히트 스톱 동안에는 팔도 함께 멈춘다.
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
                    // 명중 반경 밖으로 비켜섰으면 빗나가고, 방패를 들고 있으면 막힌다.
                    hasStruck = true;
                    attacker.Strike(_grabHit, grabPoint, 0f);
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

    // 팔이 나오기 직전 웅덩이가 꿈틀해 어디서 나올지 미리 알린다.
    private void TwitchSource(Transform source)
    {
        source.DOComplete();
        source.DOPunchScale(source.localScale * _arms.SourceTwitchStrength, _arms.SourceTwitchDuration, PunchTween.VIBRATO, PunchTween.ELASTICITY);
    }

    // 플레이어와 알맞게 떨어진 웅덩이를 무작위 순서로 count개까지 고른다. 하나도 없으면 너무 가깝지 않은 것 중 가장 가까운 것을 쓴다.
    private void PickSources(AutoAttacker attacker, List<Transform> pools, int count, List<Transform> result)
    {
        result.Clear();

        if (0 == pools.Count)
        {
            return;
        }

        Vector3 center = attacker.Target.FollowTransform.position;
        int startIndex = UnityEngine.Random.Range(0, pools.Count);
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < pools.Count && result.Count < count; i++)
        {
            Transform pool = pools[(startIndex + i) % pools.Count];
            float distance = Vector2.Distance(pool.position, center);

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
    }
}
