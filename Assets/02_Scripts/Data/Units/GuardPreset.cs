using DG.Tweening;
using UnityEngine;

// 막기의 이름·아이콘과 방패 연출. 막는 시간·비용·쿨타임·반경은 플레이어 스탯(데이터 표)에 있다.
[CreateAssetMenu(fileName = "GuardPreset", menuName = "Scriptable Objects/GuardPreset")]
public class GuardPreset : ScriptableObject
{
    [Header("표시")]
    [SerializeField] private string _displayName = "Guard";
    [SerializeField] private Color _iconColor = new Color(0.55f, 0.75f, 1f);

    [Header("방패")]
    [SerializeField, Min(0f)] private float _shieldAppearDuration = 0.08f;
    [SerializeField] private Ease _shieldAppearEase = Ease.OutBack;
    [SerializeField, Min(0f)] private float _shieldPunch = 0.3f;
    [SerializeField, Min(0f)] private float _shieldPunchDuration = 0.15f;

    [Header("막았을 때")]
    [SerializeField] private ImpactSettings _blockImpact = new();
    [SerializeField, Min(0f)] private float _blockHitStopDuration = 0.05f;
    [SerializeField, Range(0f, 1f)] private float _blockHitStopTimeScale = 0.1f;

    [Header("막기가 안 나갈 때")]
    // 쿨타임이나 게이지가 모자라 막기가 안 나가면 방패를 이 색으로 잠깐 비춰, 왜 안 막혔는지 알 수 있게 한다.
    [SerializeField] private Color _rejectColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
    [SerializeField, Min(0f)] private float _rejectFlashDuration = 0.2f;

    public string DisplayName { get { return _displayName; } }
    public Color IconColor { get { return _iconColor; } }

    public float ShieldAppearDuration { get { return _shieldAppearDuration; } }
    public Ease ShieldAppearEase { get { return _shieldAppearEase; } }
    public float ShieldPunch { get { return _shieldPunch; } }
    public float ShieldPunchDuration { get { return _shieldPunchDuration; } }

    public ImpactSettings BlockImpact { get { return _blockImpact; } }
    public float BlockHitStopDuration { get { return _blockHitStopDuration; } }
    public float BlockHitStopTimeScale { get { return _blockHitStopTimeScale; } }

    public Color RejectColor { get { return _rejectColor; } }
    public float RejectFlashDuration { get { return _rejectFlashDuration; } }
}
