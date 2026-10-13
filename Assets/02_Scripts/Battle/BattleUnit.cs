using UnityEngine;

[RequireComponent(typeof(Health), typeof(AutoAttacker), typeof(UnitView))]
public class BattleUnit : MonoBehaviour
{
    [SerializeField] private BattleUnitData _data;
    [SerializeField] private Transform _body;

    private Health _health;
    private AutoAttacker _attacker;
    private UnitView _view;
    private WanderMover _mover;

    // 키로 고른 공격이 데이터 에셋을 고치지 않도록, 실행 중에만 쓰는 덮어쓰기 값을 따로 둔다.
    private AttackStyle _attackStyleOverride;

    public BattleUnitData Data { get { return _data; } }
    public Health Health { get { return _health; } }
    public AutoAttacker Attacker { get { return _attacker; } }
    public UnitView View { get { return _view; } }

    // 배회하지 않는 유닛은 null이다.
    public WanderMover Mover { get { return _mover; } }

    // 넉백·플래시·투사체 조준의 기준점. 따로 지정하지 않으면 자기 자신을 쓴다.
    public Transform Body { get { return null != _body ? _body : transform; } }

    public AttackStyle AttackStyle { get { return null != _attackStyleOverride ? _attackStyleOverride : GetDataAttackStyle(); } }

    private void Awake()
    {
        _health = GetComponent<Health>();
        _attacker = GetComponent<AutoAttacker>();
        _view = GetComponent<UnitView>();
        TryGetComponent(out _mover);

        if (null == _data)
        {
            Debug.LogError($"전투 유닛 : {name}에 BattleUnitData가 지정되지 않았습니다.");
            return;
        }

        _health.Init(_data.MaxHp);
    }

    public void OverrideAttackStyle(AttackStyle attackStyle)
    {
        _attackStyleOverride = attackStyle;
    }

    // 데이터가 비어 있으면 Awake에서 이미 로그를 남겼으므로, 여기서는 null을 돌려 공격 쪽의 안내 로그로 이어지게 한다.
    private AttackStyle GetDataAttackStyle()
    {
        return null != _data ? _data.AttackStyle : null;
    }
}
