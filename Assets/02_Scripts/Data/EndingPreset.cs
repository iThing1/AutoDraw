using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "EndingPreset", menuName = "Scriptable Objects/EndingPreset")]
public class EndingPreset : ScriptableObject
{
    [Header("처치 슬로 모션")]
    [SerializeField, Range(0.01f, 1f)] private float _slowTimeScale = 0.2f;
    [SerializeField, Min(0f)] private float _slowDuration = 1f;

    [Header("시네마틱 진입·퇴장")]
    [SerializeField] private CinematicSettings _cinematic = new();

    [Header("보스 조각")]
    [SerializeField, Min(0f)] private float _delayBeforeShatter = 0.8f;
    [SerializeField] private ShatterSettings _shatter = new();

    [Header("잉크 걷힘")]
    [SerializeField, Min(0f)] private float _inkFadeDuration = 1.5f;
    [SerializeField] private FloatRange _inkFadeDelay = new(0f, 0.8f);
    [SerializeField] private Ease _inkFadeEase = Ease.InQuad;

    [Header("스테이지 클리어 문구")]
    [SerializeField] private StageClearSettings _stageClear = new();

    public float SlowTimeScale { get { return _slowTimeScale; } }
    public float SlowDuration { get { return _slowDuration; } }

    public CinematicSettings Cinematic { get { return _cinematic; } }

    public float DelayBeforeShatter { get { return _delayBeforeShatter; } }
    public ShatterSettings Shatter { get { return _shatter; } }

    public float InkFadeDuration { get { return _inkFadeDuration; } }
    public FloatRange InkFadeDelay { get { return _inkFadeDelay; } }
    public Ease InkFadeEase { get { return _inkFadeEase; } }

    public StageClearSettings StageClear { get { return _stageClear; } }
}
