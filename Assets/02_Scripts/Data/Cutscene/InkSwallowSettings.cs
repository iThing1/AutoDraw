using DG.Tweening;
using System;
using UnityEngine;

// 먹물 삼킴 연출의 값. 팔의 모양(굵기·테두리·손가락)은 팔 프리팹에서 정한다.
[Serializable]
public class InkSwallowSettings
{
    [Header("잉크 고르기")]
    [SerializeField] private Color _inkColor = Color.black;
    [SerializeField] private IntRange _sourceCount = new(4, 6);
    // 이 거리 안의 잉크 자국에서만 팔이 나온다. 너무 멀면 화면 밖에서 나와 보이지 않는다.
    [SerializeField, Min(0f)] private float _sourceSearchRadius = 10f;
    [SerializeField, Min(0f)] private float _sourceTwitchDuration = 0.25f;
    [SerializeField, Min(0f)] private float _sourceTwitchStrength = 0.25f;

    [Header("잉크가 모자랄 때")]
    // 주변 잉크가 모자라면 대상 둘레에 새로 찍어 채운다.
    [SerializeField] private SplatStyle _fallbackSplatStyle;
    [SerializeField] private FloatRange _fallbackSplatScale = new(0.8f, 1.3f);
    [SerializeField] private FloatRange _fallbackDistance = new(2.5f, 4.5f);

    [Header("팔")]
    [SerializeField] private InkArm _armPrefab;
    [SerializeField] private IntRange _armsPerSource = new(1, 4);
    // 같은 잉크에서 나온 팔들이 한 점에서 겹치지 않게 뿌리를 흩어 놓는 거리.
    [SerializeField, Min(0f)] private float _rootSpread = 0.3f;
    // 붙잡을 자리를 대상 중심에서 흩어 놓는 거리. 여러 손이 몸 곳곳을 쥔 것처럼 보이게 한다.
    [SerializeField, Min(0f)] private float _grabSpread = 0.35f;
    [SerializeField] private FloatRange _armStartDelay = new(0f, 0.25f);
    [SerializeField, Min(0.1f)] private float _crawlSpeed = 9f;
    // 머리가 좌우로 구불거리는 폭과 한 줄에 생기는 굽이 수.
    [SerializeField, Min(0f)] private float _slitherAmplitude = 0.6f;
    [SerializeField, Min(0f)] private float _slitherWaves = 1.2f;

    [Header("움켜쥠")]
    [SerializeField, Min(0f)] private float _grabDuration = 0.15f;
    [SerializeField, Min(0f)] private float _shakeStrength = 0.1f;

    [Header("끌고 감")]
    [SerializeField, Min(0f)] private float _pullDelay = 0.15f;
    [SerializeField, Min(0f)] private float _pullDuration = 0.7f;
    [SerializeField] private Ease _pullEase = Ease.InBack;
    // 끌고 가는 시간 중 이 비율 안에 다른 잉크의 팔들이 손을 놓고 자기 잉크로 되감긴다.
    [SerializeField, Range(0.05f, 1f)] private float _releaseRatio = 0.5f;

    [Header("가라앉음")]
    [SerializeField, Min(0f)] private float _sinkRippleDuration = 0.35f;
    [SerializeField, Min(0f)] private float _sinkRippleStrength = 0.3f;
    // 가라앉은 자리에 더 튀는 잉크 자국. 비워 두면 남기지 않는다.
    [SerializeField] private HitFeedbackPreset _residueSplat;

    public Color InkColor { get { return _inkColor; } }
    public IntRange SourceCount { get { return _sourceCount; } }
    public float SourceSearchRadius { get { return _sourceSearchRadius; } }
    public float SourceTwitchDuration { get { return _sourceTwitchDuration; } }
    public float SourceTwitchStrength { get { return _sourceTwitchStrength; } }

    public SplatStyle FallbackSplatStyle { get { return _fallbackSplatStyle; } }
    public FloatRange FallbackSplatScale { get { return _fallbackSplatScale; } }
    public FloatRange FallbackDistance { get { return _fallbackDistance; } }

    public InkArm ArmPrefab { get { return _armPrefab; } }
    public IntRange ArmsPerSource { get { return _armsPerSource; } }
    public float RootSpread { get { return _rootSpread; } }
    public float GrabSpread { get { return _grabSpread; } }
    public FloatRange ArmStartDelay { get { return _armStartDelay; } }
    public float CrawlSpeed { get { return _crawlSpeed; } }
    public float SlitherAmplitude { get { return _slitherAmplitude; } }
    public float SlitherWaves { get { return _slitherWaves; } }

    public float GrabDuration { get { return _grabDuration; } }
    public float ShakeStrength { get { return _shakeStrength; } }

    public float PullDelay { get { return _pullDelay; } }
    public float PullDuration { get { return _pullDuration; } }
    public Ease PullEase { get { return _pullEase; } }
    public float ReleaseRatio { get { return _releaseRatio; } }

    public float SinkRippleDuration { get { return _sinkRippleDuration; } }
    public float SinkRippleStrength { get { return _sinkRippleStrength; } }
    public HitFeedbackPreset ResidueSplat { get { return _residueSplat; } }

    // 한 번에 나올 수 있는 가장 많은 팔 수. 첫 패배 때 끊기지 않도록 풀에 미리 만들어 둘 때 쓴다.
    public int MaxArmCount { get { return _sourceCount.Max * _armsPerSource.Max; } }
}
