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

    private AttackContext _context;
    private AttackContext _skillContext;
    private CancellationToken _battleToken;
    private bool _isRunning;

    // 기믹·부활이 진행 중인 공격을 끊을 때 쓴다. 끊으면 버리고 다음 공격 때 새로 만든다.
    // 끊긴 토큰을 아직 쥔 작업(날아가던 탄 등)이 있을 수 있어 Dispose하지 않는다. 링크된 소스는 전투 토큰과 함께 정리된다.
    private CancellationTokenSource _interruptSource;
    private bool _isInterrupted;
    // 끊긴 동안 기다리는 쪽(공격 루프, 환경 탄막)을 깨우는 신호. 누가 기다릴 때만 만든다.
    private UniTaskCompletionSource _resumeSource;
    // 쿨타임 조건을 보려고 패턴마다 마지막으로 쏜 시간을 기억한다. 에셋(SO)에 두면 다른 유닛과 섞여 여기 둔다.
    private readonly Dictionary<AttackStyle, float> _lastFireTimes = new();

    // 전투를 시작하기 전에는 null이다.
    public AttackContext Context { get { return _context; } }
    // 전투가 끝나면(누군가 쓰러지면) 끊기는 토큰. 공격 루프 밖에서 도는 보스 연출도 전투와 함께 끝내는 데 쓴다.
    public CancellationToken BattleToken { get { return _battleToken; } }

    private void Awake()
    {
        _owner = GetComponent<BattleUnit>();
        _skillGauge = GetComponent<SkillGauge>();
        TryGetComponent(out _hands);
    }

    // 공격 간격은 게임 시간으로 재서, 히트 스톱 동안에는 다음 공격도 함께 멈춘다.
    // 물감 캔버스는 전투를 시작하는 쪽이 넘겨줘, 공격 컴포넌트가 전투 스테이지를 직접 찾지 않게 한다.
    public async UniTask RunAsync(BattleUnit target, PaintCanvas paintCanvas, CancellationToken token)
    {
        AttackTarget attackTarget = new AttackTarget(target);
        _context = CreateContext(attackTarget, paintCanvas, DamageDealer.ForWeapon(_owner, _skillGauge, attackTarget, paintCanvas));
        _skillContext = CreateContext(attackTarget, paintCanvas, DamageDealer.ForSkill(_owner, _skillGauge, attackTarget, paintCanvas));
        _battleToken = token;
        _isRunning = true;

        try
        {
            while (false == _owner.Health.IsDead && false == _context.Target.IsDefeated)
            {
                // 기믹·부활로 끊긴 동안에는 다시 이어질 때까지 새 공격을 고르지 않는다.
                await WaitUntilResumedAsync(token);

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

        if (null == _resumeSource)
        {
            return;
        }

        UniTaskCompletionSource source = _resumeSource;
        _resumeSource = null;
        source.TrySetResult();
    }

    // 끊기지 않았으면 바로 끝난다. 깨어난 사이 다시 끊겼으면 계속 기다린다.
    public async UniTask WaitUntilResumedAsync(CancellationToken token)
    {
        while (_isInterrupted)
        {
            if (null == _resumeSource)
            {
                _resumeSource = new UniTaskCompletionSource();
            }

            await _resumeSource.Task.AttachExternalCancellation(token);

            // Resume을 부른 쪽의 처리가 끝난 다음 프레임에 이어 간다.
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
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

        await WaitIntervalAsync(selectedStyle, style, token);

        if (_owner.Health.IsDead || _context.Target.IsDefeated)
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
            await FireAsync(style, _context, true, token);
        }
        else
        {
            FireAsync(style, _context, true, token).Forget();
        }

        return true;
    }

    // 기다리는 동안 공격을 바꾸면 바뀐 공격의 간격보다 오래 기다리지 않는다. 고르자마자 나와야 하는 공전 구슬 같은 공격을 위해서다.
    private async UniTask WaitIntervalAsync(AttackStyle selectedStyle, AttackStyle style, CancellationToken token)
    {
        float interval = GetInterval(style);
        float elapsed = 0f;

        while (elapsed < interval)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;

            AttackStyle current = _owner.AttackStyle;

            if (selectedStyle != current && null != current)
            {
                interval = Mathf.Min(interval, GetInterval(current));
            }
        }
    }

    private float GetInterval(AttackStyle style)
    {
        return style.AttackInterval * _owner.WeaponModifier.IntervalScale;
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
        if (false == _isRunning || _isInterrupted || null == skill || null == _context || _context.Target.IsDefeated)
        {
            return false;
        }

        FireAsync(skill.PickStyle(this), _skillContext, false, _battleToken).Forget();
        return true;
    }

    private async UniTask FireAsync(AttackStyle style, AttackContext context, bool gainsGauge, CancellationToken token)
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
                await _hands.PoseAsync(style.Tell, context.Target.GetAimPoint(Vector3.zero), style.TellDuration, token);
            }

            await style.FireAsync(context, gaugeGain, token);
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

    // 무기 공격과 스킬은 피해 처리를 따로 써, 무기 보정이 스킬에 붙지 않게 한다.
    private AttackContext CreateContext(AttackTarget attackTarget, PaintCanvas paintCanvas, DamageDealer damage)
    {
        ProjectileLauncher launcher = new ProjectileLauncher(_owner, attackTarget, damage);

        return new AttackContext(_owner, attackTarget, _hands, paintCanvas, damage, launcher);
    }

    private async UniTask<bool> WaitAndBeginActionAsync(CancellationToken token)
    {
        while (false == _owner.TryBeginAction())
        {
            if (UnitState.Dead == _owner.State)
            {
                return false;
            }

            await _owner.WaitUntilActionEndsAsync(token);
        }

        return true;
    }
}
