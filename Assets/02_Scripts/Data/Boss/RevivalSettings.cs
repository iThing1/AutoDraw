using DG.Tweening;
using System;
using UnityEngine;

// 보스가 페이즈 끝에서 쓰러졌다가, 위에서 내려온 검은 빛기둥을 받아 검게 물들며 다시 일어나는 연출의 값.
[Serializable]
public class RevivalSettings
{
    [Header("쓰러짐")]
    [SerializeField, Range(0.01f, 1f)] private float _slowTimeScale = 0.25f;
    [SerializeField, Min(0f)] private float _slowDuration = 0.8f;
    [SerializeField] private CinematicSettings _cinematic = new();
    // 쓰러지며 납작하게 주저앉는 크기 배율(가로, 세로).
    [SerializeField] private Vector2 _collapseScale = new Vector2(1.2f, 0.55f);
    [SerializeField, Min(0f)] private float _collapseDuration = 0.5f;
    [SerializeField, Min(0f)] private float _holdDuration = 0.6f;

    [Header("검은 빛")]
    // 네모 그림 하나를 길게 늘려 쓴다. 위에서 보스까지 내려와 감싼 동안 보스가 검게 물든다.
    [SerializeField] private GameObject _beamPrefab;
    [SerializeField] private Color _beamColor = new Color(0.04f, 0.04f, 0.06f, 0.75f);
    [SerializeField, Min(0f)] private float _beamWidth = 3.2f;
    // 화면 위 끝에서 내려오도록 넉넉히 잡는다.
    [SerializeField, Min(0f)] private float _beamHeight = 40f;
    [SerializeField, Min(0f)] private float _beamDropDuration = 0.35f;
    [SerializeField, Min(0f)] private float _beamHoldDuration = 0.8f;
    [SerializeField, Min(0f)] private float _beamFadeDuration = 0.5f;
    [SerializeField] private ImpactSettings _beamImpact = new();

    [Header("일어남")]
    [SerializeField, Min(0f)] private float _riseDuration = 0.5f;
    [SerializeField] private Ease _riseEase = Ease.OutBack;
    [SerializeField] private ImpactSettings _riseImpact = new();
    [SerializeField] private BannerSettings _banner = new();

    public float SlowTimeScale { get { return _slowTimeScale; } }
    public float SlowDuration { get { return _slowDuration; } }
    public CinematicSettings Cinematic { get { return _cinematic; } }
    public Vector2 CollapseScale { get { return _collapseScale; } }
    public float CollapseDuration { get { return _collapseDuration; } }
    public float HoldDuration { get { return _holdDuration; } }

    public GameObject BeamPrefab { get { return _beamPrefab; } }
    public Color BeamColor { get { return _beamColor; } }
    public float BeamWidth { get { return _beamWidth; } }
    public float BeamHeight { get { return _beamHeight; } }
    public float BeamDropDuration { get { return _beamDropDuration; } }
    public float BeamHoldDuration { get { return _beamHoldDuration; } }
    public float BeamFadeDuration { get { return _beamFadeDuration; } }
    public ImpactSettings BeamImpact { get { return _beamImpact; } }

    public float RiseDuration { get { return _riseDuration; } }
    public Ease RiseEase { get { return _riseEase; } }
    public ImpactSettings RiseImpact { get { return _riseImpact; } }
    public BannerSettings Banner { get { return _banner; } }
}
