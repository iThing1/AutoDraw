using UnityEngine;

[CreateAssetMenu(fileName = "HitFeedbackPreset", menuName = "Scriptable Objects/HitFeedbackPreset")]
public class HitFeedbackPreset : ScriptableObject
{
    [Header("히트 스톱")]
    [SerializeField, Min(0f)] private float _hitStopDuration = 0.06f;
    [SerializeField, Range(0f, 1f)] private float _hitStopTimeScale = 0.05f;

    [Header("플래시")]
    [SerializeField, Min(0f)] private float _flashDuration = 0.08f;
    [SerializeField] private Color _flashColor = Color.white;

    [Header("넉백")]
    [SerializeField, Min(0f)] private float _knockbackDistance = 0.3f;
    [SerializeField, Min(0f)] private float _knockbackDuration = 0.15f;

    [Header("임팩트")]
    [SerializeField] private ImpactSettings _impact = new();

    [Header("물감 자국")]
    [SerializeField] private IntRange _splatCount = new(1, 3);
    [SerializeField] private FloatRange _splatScale = new(0.3f, 0.6f);
    [SerializeField, Min(0f)] private float _splatSpread = 1.5f;

    public float HitStopDuration { get { return _hitStopDuration; } }
    public float HitStopTimeScale { get { return _hitStopTimeScale; } }

    public float FlashDuration { get { return _flashDuration; } }
    public Color FlashColor { get { return _flashColor; } }

    public float KnockbackDistance { get { return _knockbackDistance; } }
    public float KnockbackDuration { get { return _knockbackDuration; } }

    public ImpactSettings Impact { get { return _impact; } }

    public IntRange SplatCount { get { return _splatCount; } }
    public FloatRange SplatScale { get { return _splatScale; } }
    public float SplatSpread { get { return _splatSpread; } }
}
