using UnityEngine;

[CreateAssetMenu(fileName = "DefeatPreset", menuName = "Scriptable Objects/DefeatPreset")]
public class DefeatPreset : ScriptableObject
{
    [Header("쓰러짐 슬로 모션")]
    [SerializeField, Range(0.01f, 1f)] private float _slowTimeScale = 0.3f;
    [SerializeField, Min(0f)] private float _slowDuration = 1.2f;

    [Header("시네마틱 진입")]
    [SerializeField] private CinematicSettings _cinematic = new();

    [Header("결과 문구")]
    [SerializeField] private ResultTitleSettings _title = new();

    public float SlowTimeScale { get { return _slowTimeScale; } }
    public float SlowDuration { get { return _slowDuration; } }

    public CinematicSettings Cinematic { get { return _cinematic; } }

    public ResultTitleSettings Title { get { return _title; } }
}
