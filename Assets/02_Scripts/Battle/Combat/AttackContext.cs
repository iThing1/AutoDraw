// 공격 패턴·환경 탄막·기믹이 쓰는 전투 정보. 언제 쏠지는 AutoAttacker가 정하고, 패턴은 이 맥락만 받아 어떻게 쏠지만 정한다.
public class AttackContext
{
    private readonly BattleUnit _owner;
    private readonly AttackTarget _target;
    private readonly TargetObserver _observer;
    private readonly BossHands _hands;
    private readonly PaintCanvas _paintCanvas;
    private readonly DamageDealer _damage;
    private readonly ProjectileLauncher _launcher;
    private readonly RewindLog _rewindLog;

    public AttackContext(BattleUnit owner, AttackTarget target, TargetObserver observer, BossHands hands, PaintCanvas paintCanvas, DamageDealer damage,
        ProjectileLauncher launcher, RewindLog rewindLog)
    {
        _owner = owner;
        _target = target;
        _observer = observer;
        _hands = hands;
        _paintCanvas = paintCanvas;
        _damage = damage;
        _launcher = launcher;
        _rewindLog = rewindLog;
    }

    public BattleUnit Owner { get { return _owner; } }
    public AttackTarget Target { get { return _target; } }
    // 고른 무기나 막기·대시를 쓴 횟수처럼, 맞히는 것과 상관없이 목표를 지켜보고 알아낸 것.
    public TargetObserver Observer { get { return _observer; } }
    // 손 달린 보스만 있다. 없으면 null이다.
    public BossHands Hands { get { return _hands; } }
    public PaintCanvas PaintCanvas { get { return _paintCanvas; } }
    public DamageDealer Damage { get { return _damage; } }
    public ProjectileLauncher Launcher { get { return _launcher; } }
    // 되감기가 다시 재생할, 최근에 쓴 패턴의 기록.
    public RewindLog RewindLog { get { return _rewindLog; } }
}
