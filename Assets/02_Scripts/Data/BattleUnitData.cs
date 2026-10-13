using UnityEngine;

[CreateAssetMenu(fileName = "BattleUnitData", menuName = "Scriptable Objects/BattleUnitData")]
public class BattleUnitData : ScriptableObject
{
    [Header("이동")]
    [SerializeField, Min(0f)] private float _moveSpeed = 3f;

    [Header("체력")]
    [SerializeField, Min(1)] private int _maxHp = 100;

    [Header("자동 회복")]
    [SerializeField] private bool _useAutoHeal;
    [SerializeField, Range(0f, 1f)] private float _autoHealThresholdRatio = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _autoHealTargetRatio = 0.7f;
    [SerializeField, Min(0f)] private float _autoHealDuration = 1f;

    [Header("공격")]
    [SerializeField] private AttackStyle _attackStyle;

    [Header("겉모습")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private PaintPalette _palette;

    [Header("카메라")]
    // 전투 카메라가 이 유닛을 화면에 담을 때 확보하는 반경. 몸집이 큰 유닛일수록 크게 준다.
    [SerializeField, Min(0f)] private float _framingRadius = 1.5f;

    [Header("보스 이름 배너")]
    [SerializeField] private string _bannerTitle;
    [SerializeField] private string _bannerSubtitle;

    public float MoveSpeed { get { return _moveSpeed; } }

    public int MaxHp { get { return _maxHp; } }

    public bool UseAutoHeal { get { return _useAutoHeal; } }
    public float AutoHealThresholdRatio { get { return _autoHealThresholdRatio; } }
    public float AutoHealTargetRatio { get { return _autoHealTargetRatio; } }
    public float AutoHealDuration { get { return _autoHealDuration; } }

    public AttackStyle AttackStyle { get { return _attackStyle; } }

    public GameObject ProjectilePrefab { get { return _projectilePrefab; } }
    public PaintPalette Palette { get { return _palette; } }

    public float FramingRadius { get { return _framingRadius; } }

    public string BannerTitle { get { return _bannerTitle; } }
    public string BannerSubtitle { get { return _bannerSubtitle; } }
}
