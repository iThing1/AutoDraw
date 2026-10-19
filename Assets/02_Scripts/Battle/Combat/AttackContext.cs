// 공격 패턴·환경 탄막·기믹이 쓰는 전투 정보. 누가 누구를 노리는지, 손과 물감 캔버스, 피해 처리와 발사를 담는다.
// 언제 쏠지(공격 루프·끊기·쿨타임)는 AutoAttacker가 정하고, 패턴은 이 맥락만 받아 어떻게 쏠지만 정한다. 전투마다 새로 만든다.
public class AttackContext
{
    private readonly BattleUnit _owner;
    private readonly AttackTarget _target;
    private readonly BossHands _hands;
    private readonly PaintCanvas _paintCanvas;
    private readonly DamageDealer _damage;
    private readonly ProjectileLauncher _launcher;

    public AttackContext(BattleUnit owner, AttackTarget target, BossHands hands, PaintCanvas paintCanvas, DamageDealer damage,
        ProjectileLauncher launcher)
    {
        _owner = owner;
        _target = target;
        _hands = hands;
        _paintCanvas = paintCanvas;
        _damage = damage;
        _launcher = launcher;
    }

    public BattleUnit Owner { get { return _owner; } }
    public AttackTarget Target { get { return _target; } }
    // 손 달린 보스만 있다. 없으면 null이다.
    public BossHands Hands { get { return _hands; } }
    public PaintCanvas PaintCanvas { get { return _paintCanvas; } }
    public DamageDealer Damage { get { return _damage; } }
    public ProjectileLauncher Launcher { get { return _launcher; } }
}
