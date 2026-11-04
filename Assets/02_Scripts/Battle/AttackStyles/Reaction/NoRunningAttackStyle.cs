using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 플레이어가 대시를 자주 쓰면 "뛰지 마시오". 정해진 시간 동안 보스 머리 위에 경고 표지를 걸고 플레이어 발밑에 감시 표시를 둔다.
// 그동안 대시하면 멈춰 선 자리에 경보 조명이 켜졌다가 터진다. 묶음이 기다리지 않게 두면 보스는 그동안 다른 패턴도 쓴다.
[CreateAssetMenu(fileName = "NoRunningAttackStyle", menuName = "Scriptable Objects/AttackStyle/NoRunning")]
public class NoRunningAttackStyle : UtilityReactionAttackStyle
{
    [Header("감시")]
    [SerializeField, Min(0.1f)] private float _watchDuration = 6f;
    // 보스 머리 위 표지와 플레이어 발밑 표시. 1x1 네모·원 그림을 크기에 맞춰 늘린다.
    [SerializeField] private GameObject _signPrefab;
    [SerializeField] private Vector2 _signSize = new Vector2(1.2f, 1.2f);
    [SerializeField] private Vector3 _signOffset = new Vector3(0f, 2.6f, 0f);
    [SerializeField] private GameObject _markerPrefab;
    [SerializeField, Min(0.1f)] private float _markerRadius = 1.1f;
    [SerializeField] private Color _watchColor = new Color(0.85f, 0.1f, 0.1f, 0.35f);

    [Header("경보")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.85f, 0.1f, 0.1f, 0.45f);
    [SerializeField, Min(0.1f)] private float _alarmRadius = 1.8f;
    [SerializeField, Min(0f)] private float _warnDuration = 0.5f;
    // 대시가 끝나기를 이보다 오래 기다리지 않는다. 끊긴 대시 때문에 경보가 걸려 있지 않게 한다.
    [SerializeField, Min(0f)] private float _maxDashWait = 0.5f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    public override void Prewarm()
    {
        if (null != _signPrefab)
        {
            GameManager.Pool.Prewarm(_signPrefab, 1);
        }

        if (null != _markerPrefab)
        {
            GameManager.Pool.Prewarm(_markerPrefab, 1);
        }

        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, 4);
        }
    }

    // 감시가 시작될 때 부모가 횟수를 다시 세기 시작하므로, 그 뒤로 늘어난 횟수가 곧 감시 중에 한 대시다.
    protected override async UniTask ReactAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _signPrefab || null == _markerPrefab)
        {
            return;
        }

        float diameter = _markerRadius * 2f;
        SpriteRenderer sign = GameManager.Pool.SpawnSprite(_signPrefab, context.Owner.Body.position + _signOffset, 0f, _signSize, _watchColor);
        SpriteRenderer marker = GameManager.Pool.SpawnSprite(_markerPrefab, context.Target.FollowTransform.position, 0f, new Vector2(diameter, diameter), _watchColor);
        List<UniTask> alarms = ListPool<UniTask>.Get();

        try
        {
            int seenUses = 0;
            float elapsed = 0f;

            while (elapsed < _watchDuration)
            {
                sign.transform.position = context.Owner.Body.position + _signOffset;
                marker.transform.position = context.Target.FollowTransform.position;

                int uses = context.Observer.GetUsesSinceMark(Utility);

                if (uses > seenUses)
                {
                    seenUses = uses;
                    alarms.Add(AlarmAsync(context, gaugeGain, token));
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }

            // 감시 중에 한 대시는 이미 벌을 받았으므로, 끝나면 다시 센다. 감시가 끝나자마자 또 걸리지 않게 한다.
            context.Observer.MarkUses(Utility);

            await UniTask.WhenAll(alarms);
        }
        finally
        {
            GameManager.Pool.DespawnSprite(sign);
            GameManager.Pool.DespawnSprite(marker);
            ListPool<UniTask>.Release(alarms);
        }
    }

    // 대시가 끝나 멈춰 선 자리를 노린다. 대시 중에는 행동 상태라 그게 풀릴 때까지 기다린다.
    private async UniTask AlarmAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        BattleUnit unit = context.Target.Unit;
        float waited = 0f;

        while (UnitState.Action == unit.State && waited < _maxDashWait)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            waited += Time.deltaTime;
        }

        Vector3 point = context.Target.GetAimPoint(Vector3.zero);

        await Telegraph.PlayCircleAsync(GameManager.Pool, _telegraphPrefab, point, _alarmRadius, _telegraphColor, _warnDuration, token);

        GameManager.Feedback.PlayImpact(_impact, point);
        context.Damage.Blast(_hit, point, _alarmRadius, gaugeGain);
    }
}
