using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "CutscenePreset", menuName = "Scriptable Objects/CutscenePreset")]
public class CutscenePreset : ScriptableObject
{
    [Header("시네마틱 진입·퇴장")]
    [SerializeField] private CinematicSettings _cinematic = new();

    [Header("보스 낙하")]
    [SerializeField, Min(0f)] private float _delayBeforeDrop = 0.2f;
    [SerializeField, Min(0f)] private float _bossDropHeight = 12f;
    [SerializeField, Min(0f)] private float _bossDropDuration = 0.45f;
    [SerializeField] private Ease _bossDropEase = Ease.InQuad;

    [Header("착지 임팩트")]
    [SerializeField] private ImpactSettings _landImpact = new();

    [Header("보스 이름 배너")]
    [SerializeField] private BannerSettings _banner = new();

    public CinematicSettings Cinematic { get { return _cinematic; } }

    public float DelayBeforeDrop { get { return _delayBeforeDrop; } }
    public float BossDropHeight { get { return _bossDropHeight; } }
    public float BossDropDuration { get { return _bossDropDuration; } }
    public Ease BossDropEase { get { return _bossDropEase; } }

    public ImpactSettings LandImpact { get { return _landImpact; } }

    public BannerSettings Banner { get { return _banner; } }
}
