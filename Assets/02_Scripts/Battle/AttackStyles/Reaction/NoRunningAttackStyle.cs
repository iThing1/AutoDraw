using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 대시를 자주 쓰면 나오는 반응. 감시 시간 동안 대시가 끝난 자리마다 경보가 터진다.
[CreateAssetMenu(fileName = "NoRunningAttackStyle", menuName = "Scriptable Objects/AttackStyle/NoRunning")]
public class NoRunningAttackStyle : UtilityReactionAttackStyle
{
    [Header("감시")]
    [SerializeField, Min(0.1f)] private float _watchDuration = 6f;
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
    // 끊긴 대시 때문에 경보가 걸려 있지 않도록 대시 끝을 이 이상 기다리지 않는다.
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

    // 부모가 감시 시작 때 횟수를 다시 세므로, 늘어난 횟수가 감시 중에 한 대시다.
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

            // 끝나자마자 다시 걸리지 않게 다시 센다.
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
