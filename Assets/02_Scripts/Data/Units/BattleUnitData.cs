using UnityEngine;

public abstract class BattleUnitData : ScriptableObject
{
    [Header("배회")]
    [SerializeField] private WanderSettings _wander = new();

    [Header("겉모습")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private PaintPalette _palette;

    [Header("카메라")]
    // 전투 카메라가 이 유닛을 화면에 담을 때 확보하는 반경. 몸집이 큰 유닛일수록 크게 준다.
    [SerializeField, Min(0f)] private float _framingRadius = 1.5f;

    public abstract IUnitStats Stats { get; }

    public WanderSettings Wander { get { return _wander; } }

    public abstract AttackStyle AttackStyle { get; }

    public GameObject ProjectilePrefab { get { return _projectilePrefab; } }
    public PaintPalette Palette { get { return _palette; } }

    public float FramingRadius { get { return _framingRadius; } }
}
