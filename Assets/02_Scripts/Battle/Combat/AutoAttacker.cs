using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class AutoAttacker : MonoBehaviour
{
    private BattleUnit _owner;
    private SkillGauge _skillGauge;
    private BossHands _hands;

    private AttackTarget _target;
    private PaintCanvas _paintCanvas;
    private DamageDealer _damageDealer;
    private CancellationToken _battleToken;
    private bool _isRunning;
    private bool _hasLoggedMissingProjectile;

    // 기믹·부활이 진행 중인 공격을 끊을 때 쓴다. 끊으면 버리고 다음 공격 때 새로 만든다.
    // 끊긴 토큰을 아직 쥔 작업(날아가던 탄 등)이 있을 수 있어 Dispose하지 않는다. 링크된 소스는 전투 토큰과 함께 정리된다.
    private CancellationTokenSource _interruptSource;
    private bool _isInterrupted;
    // 쿨타임 조건을 보려고 패턴마다 마지막으로 쏜 시간을 기억한다. 에셋(SO)에 두면 다른 유닛과 섞여 여기 둔다.
    private readonly Dictionary<AttackStyle, float> _lastFireTimes = new();

    public BattleUnit Owner { get { return _owner; } }
    public AttackTarget Target { get { return _target; } }
    public BossHands Hands { get { return _hands; } }
    public PaintCanvas PaintCanvas { get { return _paintCanvas; } }
    // 전투가 끝나면(누군가 쓰러지면) 끊기는 토큰. 공격 루프 밖에서 도는 보스 연출도 전투와 함께 끝내는 데 쓴다.
    public CancellationToken BattleToken { get { return _battleToken; } }
    public bool IsInterrupted { get { return _isInterrupted; } }

    private void Awake()
    {
        _owner = GetComponent<BattleUnit>();
        _skillGauge = GetComponent<SkillGauge>();
        TryGetComponent(out _hands);
    }

    // 공격 간격은 게임 시간으로 재서, 히트 스톱 동안에는 다음 공격도 함께 멈춘다.
    // 물감 캔버스는 전투를 시작하는 쪽이 넘겨줘, 공격 컴포넌트가 전투 매니저를 직접 찾지 않게 한다.
    public async UniTask RunAsync(BattleUnit target, PaintCanvas paintCanvas, CancellationToken token)
    {
        _target = new AttackTarget(target);
        _paintCanvas = paintCanvas;
        _damageDealer = new DamageDealer(_owner, _skillGauge, _target, paintCanvas);
        _battleToken = token;
        _isRunning = true;

        try
        {
            while (false == _owner.Health.IsDead && false == _target.IsDefeated)
            {
                // 기믹·부활로 끊긴 동안에는 다시 이어질 때까지 새 공격을 고르지 않는다.
                while (_isInterrupted)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                try
                {
                    if (false == await AttackOnceAsync(GetAttackToken(token)))
                    {
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    // 전투가 끝나 취소됐으면 루프를 나가고, 기믹이 끊은 공격이면 다음 공격으로 넘어간다.
                    if (token.IsCancellationRequested)
                    {
                        throw;
                    }
                }
            }
        }
        finally
        {
            _isRunning = false;
            _interruptSource = null;
        }
    }

    // 진행 중인 공격(예고·발사·날아가던 탄)을 끊고, Resume 전까지 새 공격을 시작하지 않는다.
    public void Interrupt()
    {
        _isInterrupted = true;

        if (null != _interruptSource)
        {
            _interruptSource.Cancel();
            _interruptSource = null;
        }
    }

    public void Resume()
    {
        _isInterrupted = false;
    }

    public bool IsOnCooldown(AttackStyle style, float cooldown)
    {
        if (0f >= cooldown || null == style || false == _lastFireTimes.TryGetValue(style, out float lastFireTime))
        {
            return false;
        }

        return Time.time - lastFireTime < cooldown;
    }

    // 공격 스타일이 없으면 false를 돌려 공격을 끝낸다.
    private async UniTask<bool> AttackOnceAsync(CancellationToken token)
    {
        AttackStyle selectedStyle = _owner.AttackStyle;

        if (null == selectedStyle)
        {
            Debug.LogError($"자동 공격 : {_owner.name}에 공격 스타일이 지정되지 않았습니다.");
            return false;
        }

        // 여러 패턴을 묶은 스타일이면 이번에 쓸 패턴을 먼저 골라, 간격·대기·멈춤을 그 패턴의 값으로 정한다.
        AttackStyle style = selectedStyle.PickStyle(this);

        await UniTask.Delay(TimeSpan.FromSeconds(style.AttackInterval), cancellationToken: token);

        if (_owner.Health.IsDead || _target.IsDefeated)
        {
            return true;
        }

        // 기다리는 동안 키로 공격을 바꿨거나 페이즈가 바뀌었으면 다음 공격부터가 아니라 바로 새 공격을 쏜다.
        if (selectedStyle != _owner.AttackStyle)
        {
            style = _owner.AttackStyle.PickStyle(this);
        }

        if (style.WaitUntilFinished)
        {
            await FireAsync(style, true, token);
        }
        else
        {
            FireAsync(style, true, token).Forget();
        }

        return true;
    }

    private CancellationToken GetAttackToken(CancellationToken battleToken)
    {
        if (null == _interruptSource)
        {
            _interruptSource = CancellationTokenSource.CreateLinkedTokenSource(battleToken);
        }

        return _interruptSource.Token;
    }

    // 특수 스킬은 공격 주기와 상관없이 즉시 발동하고, 게이지를 쌓지 않는다.
    public bool TryFireSkill(AttackStyle skill)
    {
        if (false == _isRunning || _isInterrupted || null == skill || null == _target || _target.IsDefeated)
        {
            return false;
        }

        FireAsync(skill.PickStyle(this), false, _battleToken).Forget();
        return true;
    }

    // 공격 스타일들이 함께 쓰는 발사. 맞으면 피해, 타격 연출, 물감 자국, 게이지까지 처리한다.
    public void FireShot(ShotSettings shot, Vector3 start, Vector3 landingOffset, float gaugeGain, CancellationToken token)
    {
        if (false == HasProjectilePrefab())
        {
            return;
        }

        FireShotAsync(_owner.Data.ProjectilePrefab, shot, start, _target.GetAimPoint(landingOffset), gaugeGain, token).Forget();
    }

    // 목표를 겨누지 않고 정해진 방향으로 사거리만큼 날린다. 날아가는 길에 목표가 닿으면 맞는다.
    // 추적 강도가 0인 탄과 함께 써서, 플레이어가 피할 자리를 찾아 움직이게 하는 탄막에 쓴다.
    public void FireShotToward(GameObject projectilePrefab, ShotSettings shot, Vector3 start, Vector2 direction, float range, float gaugeGain,
        CancellationToken token)
    {
        if (null == projectilePrefab)
        {
            if (false == HasProjectilePrefab())
            {
                return;
            }

            projectilePrefab = _owner.Data.ProjectilePrefab;
        }

        Vector3 end = start + (Vector3)(direction.normalized * range);

        FireShotAsync(projectilePrefab, shot, start, end, gaugeGain, token).Forget();
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

    // 맞히는 규칙은 DamageDealer가 맡고, 공격 스타일은 여기를 거쳐 부른다.
    public void Strike(ShotSettings hit, Vector3 hitPoint, float gaugeGain)
    {
        _damageDealer.Strike(hit, hitPoint, gaugeGain);
    }

    public void StrikeUnblockable(ShotSettings hit, Vector3 hitPoint)
    {
        _damageDealer.StrikeUnblockable(hit, hitPoint);
    }

    public void Blast(ShotSettings hit, Vector3 center, float radius, float gaugeGain)
    {
        _damageDealer.Blast(hit, center, radius, gaugeGain);
    }

    public Color PickColor()
    {
        return _damageDealer.PickColor();
    }

    private async UniTask FireAsync(AttackStyle style, bool gainsGauge, CancellationToken token)
    {
        float gaugeGain = gainsGauge ? style.GaugeGainPerHit : 0f;
        bool hasBegunAction = false;
        bool hasPosed = false;

        _lastFireTimes[style] = Time.time;

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

            // 손 달린 보스는 패턴마다 정해진 손동작을 먼저 보여 줘, 무엇이 올지 읽고 대비할 틈을 준다.
            if (null != _hands && HandGesture.None != style.Tell)
            {
                hasPosed = true;
                await _hands.PoseAsync(style.Tell, _target.GetAimPoint(Vector3.zero), style.TellDuration, token);
            }

            await style.FireAsync(this, gaugeGain, token);
        }
        catch (OperationCanceledException)
        {
            // 전투가 끝나 남은 발사가 취소된 경우라 따로 처리하지 않는다.
        }
        finally
        {
            if (hasPosed)
            {
                _hands.ReturnToRest();
            }

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

    private async UniTaskVoid FireShotAsync(GameObject projectilePrefab, ShotSettings shot, Vector3 start, Vector3 end, float gaugeGain,
        CancellationToken token)
    {
        DamageInfo damage = _damageDealer.RollDamage(shot);

        FlightResult flight = await ProjectileFlight.FlyAsync(projectilePrefab, shot, start, end, _target, damage.PaintColor, token);

        if (FlightOutcome.Blocked == flight.Outcome)
        {
            _target.NotifyBlocked(flight.LandingPoint);
        }
        else if (FlightOutcome.Arrived == flight.Outcome)
        {
            _damageDealer.DealHit(shot, damage, flight.LandingPoint, gaugeGain);
        }
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
