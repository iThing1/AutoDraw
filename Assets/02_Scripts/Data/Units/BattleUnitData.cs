using UnityEngine;

[CreateAssetMenu(fileName = "BattleUnitData", menuName = "Scriptable Objects/BattleUnitData")]
public class BattleUnitData : ScriptableObject
{
    [Header("이동")]
    [SerializeField, Min(0f)] private float _moveSpeed = 3f;

    [Header("배회")]
    [SerializeField] private WanderSettings _wander = new();

    [Header("체력")]
    [SerializeField, Min(1)] private int _maxHp = 100;
    // 떨어진 자리가 몸 중심에서 이 거리 안이면 맞는다. 몸보다 넉넉하게 준다.
    [SerializeField, Min(0f)] private float _hitRadius = 1f;

    [Header("공격")]
    [SerializeField] private AttackStyle _attackStyle;

    [Header("겉모습")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private PaintPalette _palette;

    [Header("카메라")]
    // 전투 카메라가 이 유닛을 화면에 담을 때 확보하는 반경. 몸집이 큰 유닛일수록 크게 준다.
    [SerializeField, Min(0f)] private float _framingRadius = 1.5f;

    public float MoveSpeed { get { return _moveSpeed; } }

    public WanderSettings Wander { get { return _wander; } }

    public int MaxHp { get { return _maxHp; } }
    public float HitRadius { get { return _hitRadius; } }

    public AttackStyle AttackStyle { get { return _attackStyle; } }

    public GameObject ProjectilePrefab { get { return _projectilePrefab; } }
    public PaintPalette Palette { get { return _palette; } }

    public float FramingRadius { get { return _framingRadius; } }
}
