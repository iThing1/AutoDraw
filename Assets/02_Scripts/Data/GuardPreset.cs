using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "GuardPreset", menuName = "Scriptable Objects/GuardPreset")]
public class GuardPreset : ScriptableObject
{
    [Header("표시")]
    [SerializeField] private string _displayName = "Guard";
    [SerializeField] private Color _iconColor = new Color(0.55f, 0.75f, 1f);

    [Header("막기")]
    [SerializeField, Min(0f)] private float _guardDuration = 0.25f;
    [SerializeField, Min(0f)] private float _gaugeCost = 25f;
    [SerializeField, Min(0f)] private float _cooldown = 0.4f;

    [Header("방패")]
    [SerializeField, Min(0f)] private float _shieldAppearDuration = 0.08f;
    [SerializeField] private Ease _shieldAppearEase = Ease.OutBack;
    [SerializeField, Min(0f)] private float _shieldPunch = 0.3f;
    [SerializeField, Min(0f)] private float _shieldPunchDuration = 0.15f;

    [Header("막았을 때")]
    [SerializeField] private ImpactSettings _blockImpact = new();

    public string DisplayName { get { return _displayName; } }
    public Color IconColor { get { return _iconColor; } }

    public float GuardDuration { get { return _guardDuration; } }
    public float GaugeCost { get { return _gaugeCost; } }
    public float Cooldown { get { return _cooldown; } }

    public float ShieldAppearDuration { get { return _shieldAppearDuration; } }
    public Ease ShieldAppearEase { get { return _shieldAppearEase; } }
    public float ShieldPunch { get { return _shieldPunch; } }
    public float ShieldPunchDuration { get { return _shieldPunchDuration; } }

    public ImpactSettings BlockImpact { get { return _blockImpact; } }
}
