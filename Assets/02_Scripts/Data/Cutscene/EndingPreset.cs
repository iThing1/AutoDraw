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
    [SerializeField, Min(0f)] private float _inkFadeDelay;
    [SerializeField] private Ease _inkFadeEase = Ease.InQuad;

    [Header("전체 보기")]
    // 보스가 조각난 뒤 종이 전체를 담도록 카메라를 뒤로 빼는 시간.
    [SerializeField, Min(0f)] private float _overviewBlendDuration = 1.5f;
    [SerializeField, Min(0f)] private float _overviewPadding = 0f;

    [Header("그림 촬영")]
    // 액자에 거는 그림 크기. 맵·액자와 같은 16:9로 찍는다.
    [SerializeField] private Vector2Int _paintingSize = new Vector2Int(1280, 720);

    [Header("스테이지 클리어 문구")]
    [SerializeField] private ResultTitleSettings _resultTitle = new();

    public float OverviewBlendDuration { get { return _overviewBlendDuration; } }
    public float OverviewPadding { get { return _overviewPadding; } }
    public Vector2Int PaintingSize { get { return _paintingSize; } }

    public float SlowTimeScale { get { return _slowTimeScale; } }
    public float SlowDuration { get { return _slowDuration; } }

    public CinematicSettings Cinematic { get { return _cinematic; } }

    public float DelayBeforeShatter { get { return _delayBeforeShatter; } }
    public ShatterSettings Shatter { get { return _shatter; } }

    public float InkFadeDuration { get { return _inkFadeDuration; } }
    public float InkFadeDelay { get { return _inkFadeDelay; } }
    public Ease InkFadeEase { get { return _inkFadeEase; } }

    public ResultTitleSettings ResultTitle { get { return _resultTitle; } }
}
