using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class AutoAttacker : MonoBehaviour
{
    private BattleUnit _owner;
    private SkillGauge _skillGauge;
    private int _paletteIndex;

    private BattleUnit _target;
    private PaintCanvas _paintCanvas;
    private CancellationToken _battleToken;
    private bool _isRunning;

    private void Awake()
    {
        _owner = GetComponent<BattleUnit>();
        _skillGauge = GetComponent<SkillGauge>();
    }

    // 공격 간격은 게임 시간으로 재서, 히트 스톱 동안에는 다음 공격도 함께 멈춘다.
    // 물감 캔버스는 전투를 시작하는 쪽이 넘겨줘, 공격 컴포넌트가 전투 매니저를 직접 찾지 않게 한다.
    public async UniTask RunAsync(BattleUnit target, PaintCanvas paintCanvas, CancellationToken token)
    {
        _target = target;
        _paintCanvas = paintCanvas;
        _battleToken = token;
        _isRunning = true;

        try
        {
            while (false == _owner.Health.IsDead && false == target.Health.IsDead)
            {
                AttackStyle style = _owner.AttackStyle;

                if (null == style)
                {
                    Debug.LogError($"자동 공격 : {_owner.name}에 공격 스타일이 지정되지 않았습니다.");
                    return;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(style.AttackInterval), cancellationToken: token);

                if (_owner.Health.IsDead || target.Health.IsDead)
                {
                    return;
                }

                // 키를 바꾸면 다음 공격부터 적용되도록, 쏘는 순간의 공격 스타일을 다시 읽는다.
                FireAsync(_owner.AttackStyle, true, token).Forget();
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    // 특수 스킬은 공격 주기와 상관없이 즉시 발동하고, 게이지를 쌓지 않는다.
    public bool TryFireSkill(AttackStyle skill)
    {
        if (false == _isRunning || null == skill || null == _target || _target.Health.IsDead)
        {
            return false;
        }

        FireAsync(skill, false, _battleToken).Forget();
        return true;
    }

    // 쏘고 나면 결과를 기다리지 않으므로 UniTaskVoid로 만들고, 부르는 쪽은 Forget()으로 실행만 한다.
    private async UniTaskVoid FireAsync(AttackStyle style, bool gainsGauge, CancellationToken token)
    {
        if (null == _owner.Data.ProjectilePrefab)
        {
            Debug.LogError($"자동 공격 : {_owner.name}의 투사체 프리팹이 지정되지 않았습니다.");
            return;
        }

        float gaugeGain = gainsGauge ? style.GaugeGainPerHit : 0f;

        try
        {
            // 새 패턴은 AttackPattern 값, 여기 case, AttackStyle의 전용 필드를 함께 추가한다.
            // 패턴이 더 늘어나면 패턴마다 클래스를 나누는 추상 클래스(전략 패턴) 구조로 분리를 검토한다.
            switch (style.Pattern)
            {
                case AttackPattern.Burst:
                    for (int i = 0; i < style.BurstCount; i++)
                    {
                        FireShotAsync(style.Shot, _owner.Body.position, Vector3.zero, gaugeGain, token).Forget();

                        await UniTask.Delay(TimeSpan.FromSeconds(style.BurstInterval), cancellationToken: token);
                    }
                    break;

                case AttackPattern.Cluster:
                    await FireClusterAsync(style, gaugeGain, token);
                    break;

                default:
                    FireShotAsync(style.Shot, _owner.Body.position, Vector3.zero, gaugeGain, token).Forget();
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // 전투가 끝나 남은 발사가 취소된 경우라 따로 처리하지 않는다.
        }
    }

    // 덩어리가 공중의 분열 지점까지 날아가 터지고, 조각들이 시간차를 두고 목표 주변으로 쏟아진다.
    private async UniTask FireClusterAsync(AttackStyle style, float gaugeGain, CancellationToken token)
    {
        ClusterSettings cluster = style.Cluster;

        Vector3 start = _owner.Body.position;
        Vector3 end = _target.Body.position;
        Vector3 splitPoint = Vector3.Lerp(start, end, cluster.SplitRatio) + Vector3.up * cluster.SplitHeight;

        Color color = PickColor();
        bool hasArrived = await ProjectileFlight.FlyAsync(_owner.Data.ProjectilePrefab, style.Shot, start, splitPoint, color, token);

        if (false == hasArrived || _target.Health.IsDead)
        {
            return;
        }

        ImpactPlayer.Play(cluster.SplitImpact, splitPoint, color);

        for (int i = 0; i < cluster.FragmentCount; i++)
        {
            Vector3 landingOffset = UnityEngine.Random.insideUnitCircle * cluster.LandingSpread;

            FireShotAsync(cluster.Fragment, splitPoint, landingOffset, gaugeGain, token).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(cluster.FragmentInterval), cancellationToken: token);
        }
    }

    private async UniTaskVoid FireShotAsync(ShotSettings shot, Vector3 start, Vector3 landingOffset, float gaugeGain, CancellationToken token)
    {
        // 피해량과 물감 색은 쏘는 순간 정해, 날아가는 투사체의 색과 맞은 자리의 물감 색이 같게 한다.
        DamageInfo info = RollDamage(shot);

        bool hasArrived = await ProjectileFlight.FlyAsync(_owner.Data.ProjectilePrefab, shot, start, _target.Body.position + landingOffset, info.PaintColor, token);

        if (false == hasArrived || _target.Health.IsDead)
        {
            return;
        }

        info.HitPoint = _target.Body.position + landingOffset;

        _target.Health.TakeDamage(info);

        HitFeedbackPlayer.Play(shot.GetHitFeedback(info.IsCritical), _target, info, _paintCanvas);

        if (null != _skillGauge && 0f < gaugeGain)
        {
            _skillGauge.Add(gaugeGain);
        }
    }

    private DamageInfo RollDamage(ShotSettings shot)
    {
        int amount = shot.Damage.Pick();
        bool isCritical = UnityEngine.Random.value < shot.CriticalChance;

        if (isCritical)
        {
            amount = Mathf.RoundToInt(amount * shot.CriticalMultiplier);
        }

        PaintPalette palette = _owner.Data.Palette;

        return new DamageInfo
        {
            Amount = amount,
            IsCritical = isCritical,
            PaintColor = PickColor(),
            IsInk = null != palette && palette.IsInk,
            Attacker = _owner,
        };
    }

    private Color PickColor()
    {
        PaintPalette palette = _owner.Data.Palette;

        if (null == palette)
        {
            return Color.white;
        }

        Color color = palette.Pick(_paletteIndex);
        _paletteIndex++;

        return color;
    }
}
