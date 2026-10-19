using UnityEngine;

// 보스와 플레이어가 함께 쓰는 유닛 데이터. 스탯이 어디 있는지(보스는 SO, 플레이어는 데이터 표)는 하위 클래스가 정한다.
public abstract class BattleUnitData : ScriptableObject
{
    [Header("배회")]
    [SerializeField] private WanderSettings _wander = new();

    [Header("공격")]
    [SerializeField] private AttackStyle _attackStyle;

    [Header("겉모습")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private PaintPalette _palette;

    [Header("카메라")]
    // 전투 카메라가 이 유닛을 화면에 담을 때 확보하는 반경. 몸집이 큰 유닛일수록 크게 준다.
    [SerializeField, Min(0f)] private float _framingRadius = 1.5f;

    public abstract IUnitStats Stats { get; }

    public WanderSettings Wander { get { return _wander; } }

    public AttackStyle AttackStyle { get { return _attackStyle; } }

    public GameObject ProjectilePrefab { get { return _projectilePrefab; } }
    public PaintPalette Palette { get { return _palette; } }

    public float FramingRadius { get { return _framingRadius; } }
}
