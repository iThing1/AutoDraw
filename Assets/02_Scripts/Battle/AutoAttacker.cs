using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class AutoAttacker : MonoBehaviour
{
    private BattleUnit _owner;
    private SkillGauge _skillGauge;
    private int _nextColorIndex;

    private AttackTarget _target;
    private PaintCanvas _paintCanvas;
    private CancellationToken _battleToken;
    private bool _isRunning;
    private bool _hasLoggedMissingProjectile;

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

        FireShotAsync(shot, start, _target.GetAimPoint(landingOffset), gaugeGain, token).Forget();
    }

    // 목표를 겨누지 않고 정해진 방향으로 사거리만큼 날린다. 날아가는 길에 목표가 닿으면 맞는다.
    // 추적 강도가 0인 탄과 함께 써서, 플레이어가 피할 자리를 찾아 움직이게 하는 탄막에 쓴다.
    public void FireShotToward(ShotSettings shot, Vector3 start, Vector2 direction, float range, float gaugeGain, CancellationToken token)
    {
        if (false == HasProjectilePrefab())
        {
            return;
        }

        Vector3 end = start + (Vector3)(direction.normalized * range);

        FireShotAsync(shot, start, end, gaugeGain, token).Forget();
    }

    // 목표가 아닌 고정된 지점으로 날린다. 파편이 갈라지는 공중 지점처럼 맞히지 않는 비행에 쓴다.
    public async UniTask<bool> FlyToPointAsync(ShotSettings shot, Vector3 start, Vector3 end, Color color, CancellationToken token)
    {
        if (false == HasProjectilePrefab())
        {
            return false;
        }

        FlightResult flight = await ProjectileFlight.FlyAsync(_owner.Data.ProjectilePrefab, shot, start, end, color, token);

        return FlightOutcome.Arrived == flight.Outcome;
    }

    // 날아가는 과정 없이 바로 맞힌다. 근접 공격처럼 투사체가 없는 공격에 쓴다.
    public void Strike(ShotSettings hit, Vector3 hitPoint, float gaugeGain)
    {
        ApplyHit(hit, RollDamage(hit), hitPoint, gaugeGain);
    }

    // 범위 공격. 폭발 반경 안에 목표가 있으면 목표 자리에 맞고, 방패를 들고 있으면 막힌다.
    public void Blast(ShotSettings hit, Vector3 center, float radius, float gaugeGain)
    {
        if (false == _target.IsInBlast(center, radius))
        {
            return;
        }

        ApplyHit(hit, RollDamage(hit), _target.GetAimPoint(Vector3.zero), gaugeGain);
    }

    public Color PickColor()
    {
        PaintPalette palette = _owner.Data.Palette;

        if (null == palette)
        {
            return Color.white;
        }

        Color color = palette.Pick(_nextColorIndex);
        _nextColorIndex++;

        return color;
    }

    private async UniTask FireAsync(AttackStyle style, bool gainsGauge, CancellationToken token)
    {
        float gaugeGain = gainsGauge ? style.GaugeGainPerHit : 0f;
        bool hasBegunAction = false;

        try
        {
            if (style.StopsMovement)
            {
                hasBegunAction = await WaitAndBeginActionAsync(token);

                if (false == hasBegunAction)
                {
                    return;
                }
            }

            await style.FireAsync(this, gaugeGain, token);
        }
        catch (OperationCanceledException)
        {
            // 전투가 끝나 남은 발사가 취소된 경우라 따로 처리하지 않는다.
        }
        finally
        {
            // 기다리다 취소됐으면 다른 패턴의 Action을 끝내지 않는다.
            if (hasBegunAction)
            {
                _owner.EndAction();
            }
        }
    }

    private async UniTask<bool> WaitAndBeginActionAsync(CancellationToken token)
    {
        while (false == _owner.TryBeginAction())
        {
            if (UnitState.Dead == _owner.State)
            {
                return false;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        return true;
    }

    private async UniTaskVoid FireShotAsync(ShotSettings shot, Vector3 start, Vector3 end, float gaugeGain, CancellationToken token)
    {
        // 피해량과 물감 색은 쏘는 순간 정해, 날아가는 투사체의 색과 맞은 자리의 물감 색이 같게 한다.
        DamageInfo damage = RollDamage(shot);

        FlightResult flight = await ProjectileFlight.FlyAsync(_owner.Data.ProjectilePrefab, shot, start, end, _target, damage.PaintColor, token);

        if (FlightOutcome.Blocked == flight.Outcome)
        {
            _target.NotifyBlocked(flight.LandingPoint);
        }
        else if (FlightOutcome.Arrived == flight.Outcome)
        {
            // 큰 탄은 명중 반경 밖에서 몸 가장자리에 닿으므로, 비행이 이미 닿았다고 본 탄은 명중 반경으로 다시 거르지 않는다.
            DealHit(shot, damage, flight.LandingPoint, gaugeGain);
        }
    }

    // 방패가 명중 반경보다 넓어서, 명중 반경 밖이어도 방패 안이면 막기 판정으로 넘긴다.
    private void ApplyHit(ShotSettings hit, DamageInfo damage, Vector3 hitPoint, float gaugeGain)
    {
        if (false == _target.IsHit(hitPoint) && false == _target.IsBlockedAt(hitPoint, 0f))
        {
            return;
        }

        DealHit(hit, damage, hitPoint, gaugeGain);
    }

    // 막혔으면 피해·물감 자국·게이지 없이 끝난다. 막기 연출은 막은 쪽이 맡는다.
    private void DealHit(ShotSettings hit, DamageInfo damage, Vector3 hitPoint, float gaugeGain)
    {
        if (_target.IsDefeated || _target.TryBlockHit(hitPoint))
        {
            return;
        }

        damage.HitPoint = hitPoint;

        _target.Unit.Health.TakeDamage(damage);

        HitFeedbackPlayer.Play(hit.GetHitFeedback(damage.IsCritical), _target.Unit, damage, _paintCanvas);

        if (null != _skillGauge && 0f < gaugeGain)
        {
            _skillGauge.Add(gaugeGain);
        }
    }

    private DamageInfo RollDamage(ShotSettings shot)
    {
        int amount = shot.Damage.PickRandom();
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

        // 쏠 때마다 남기면 같은 로그가 쌓여 다른 에러가 묻히므로 한 번만 알린다.
        if (false == _hasLoggedMissingProjectile)
        {
            _hasLoggedMissingProjectile = true;
            Debug.LogError($"자동 공격 : {_owner.name}의 투사체 프리팹이 지정되지 않았습니다.");
        }

        return false;
    }
}
