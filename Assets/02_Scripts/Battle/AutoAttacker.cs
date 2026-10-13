using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class AutoAttacker : MonoBehaviour
{
    private BattleUnit _owner;
    private SkillGauge _skillGauge;
    private int _paletteIndex;

    private AttackTarget _target;
    private PaintCanvas _paintCanvas;
    private CancellationToken _battleToken;
    private bool _isRunning;

    public BattleUnit Owner { get { return _owner; } }
    public AttackTarget Target { get { return _target; } }

    private void Awake()
    {
        _owner = GetComponent<BattleUnit>();
        _skillGauge = GetComponent<SkillGauge>();
    }

    // 공격 간격은 게임 시간으로 재서, 히트 스톱 동안에는 다음 공격도 함께 멈춘다.
    // 물감 캔버스는 전투를 시작하는 쪽이 넘겨줘, 공격 컴포넌트가 전투 매니저를 직접 찾지 않게 한다.
    public async UniTask RunAsync(BattleUnit target, PaintCanvas paintCanvas, CancellationToken token)
    {
        _target = new AttackTarget(target);
        _paintCanvas = paintCanvas;
        _battleToken = token;
        _isRunning = true;

        try
        {
            while (false == _owner.Health.IsDead && false == _target.IsDefeated)
            {
                AttackStyle selectedStyle = _owner.AttackStyle;

                if (null == selectedStyle)
                {
                    Debug.LogError($"자동 공격 : {_owner.name}에 공격 스타일이 지정되지 않았습니다.");
                    return;
                }

                // 여러 패턴을 묶은 스타일이면 이번에 쓸 패턴을 먼저 골라, 간격·대기·멈춤을 그 패턴의 값으로 정한다.
                AttackStyle style = selectedStyle.PickStyle();

                await UniTask.Delay(TimeSpan.FromSeconds(style.AttackInterval), cancellationToken: token);

                if (_owner.Health.IsDead || _target.IsDefeated)
                {
                    return;
                }

                // 기다리는 동안 키로 공격을 바꿨으면 다음 공격부터가 아니라 바로 새 공격을 쏜다.
                if (selectedStyle != _owner.AttackStyle)
                {
                    style = _owner.AttackStyle.PickStyle();
                }

                if (style.WaitUntilFinished)
                {
                    await FireAsync(style, true, token);
                }
                else
                {
                    FireAsync(style, true, token).Forget();
                }
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
        if (false == _isRunning || null == skill || null == _target || _target.IsDefeated)
        {
            return false;
        }

        FireAsync(skill.PickStyle(), false, _battleToken).Forget();
        return true;
    }

    // 공격 스타일들이 함께 쓰는 발사. 맞으면 피해, 타격 연출, 물감 자국, 게이지까지 처리한다.
    public void FireShot(ShotSettings shot, Vector3 start, Vector3 landingOffset, float gaugeGain, CancellationToken token)
    {
        if (false == HasProjectilePrefab())
        {
            return;
        }

        FireShotAsync(shot, start, landingOffset, gaugeGain, token).Forget();
    }

    // 목표가 아닌 고정된 지점으로 날린다. 파편이 갈라지는 공중 지점처럼 맞히지 않는 비행에 쓴다.
    public UniTask<bool> FlyToPointAsync(ShotSettings shot, Vector3 start, Vector3 end, Color color, CancellationToken token)
    {
        if (false == HasProjectilePrefab())
        {
            return UniTask.FromResult(false);
        }

        return ProjectileFlight.FlyAsync(_owner.Data.ProjectilePrefab, shot, start, end, color, token);
    }

    // 날아가는 과정 없이 바로 맞힌다. 근접 공격처럼 투사체가 없는 공격에 쓴다.
    public void Strike(ShotSettings hit, Vector3 hitPoint, float gaugeGain)
    {
        ApplyHit(hit, RollDamage(hit), hitPoint, gaugeGain);
    }

    public Color PickColor()
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

    private async UniTask FireAsync(AttackStyle style, bool gainsGauge, CancellationToken token)
    {
        float gaugeGain = gainsGauge ? style.GaugeGainPerHit : 0f;
        // WanderMover는 Unity 오브젝트라 ?. 대신 == null로 확인한다. ?.는 파괴된 오브젝트를 null로 보지 않는다.
        WanderMover mover = style.StopsMovement ? _owner.Mover : null;

        if (null != mover)
        {
            mover.Pause();
        }

        try
        {
            await style.FireAsync(this, gaugeGain, token);
        }
        catch (OperationCanceledException)
        {
            // 전투가 끝나 남은 발사가 취소된 경우라 따로 처리하지 않는다.
        }
        finally
        {
            if (null != mover)
            {
                mover.Resume();
            }
        }
    }

    private async UniTaskVoid FireShotAsync(ShotSettings shot, Vector3 start, Vector3 landingOffset, float gaugeGain, CancellationToken token)
    {
        // 피해량과 물감 색은 쏘는 순간 정해, 날아가는 투사체의 색과 맞은 자리의 물감 색이 같게 한다.
        DamageInfo info = RollDamage(shot);

        bool hasArrived = await ProjectileFlight.FlyAsync(_owner.Data.ProjectilePrefab, shot, start, _target.GetAimPoint(landingOffset),
            _target.FollowTransform, info.PaintColor, token);

        if (false == hasArrived)
        {
            return;
        }

        ApplyHit(shot, info, _target.GetAimPoint(landingOffset), gaugeGain);
    }

    private void ApplyHit(ShotSettings hit, DamageInfo info, Vector3 hitPoint, float gaugeGain)
    {
        if (_target.IsDefeated || false == _target.IsHit(hitPoint))
        {
            return;
        }

        info.HitPoint = hitPoint;

        _target.Unit.Health.TakeDamage(info);

        HitFeedbackPlayer.Play(hit.GetHitFeedback(info.IsCritical), _target.Unit, info, _paintCanvas);

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

    private bool HasProjectilePrefab()
    {
        if (null != _owner.Data.ProjectilePrefab)
        {
            return true;
        }

        Debug.LogError($"자동 공격 : {_owner.name}의 투사체 프리팹이 지정되지 않았습니다.");
        return false;
    }
}
