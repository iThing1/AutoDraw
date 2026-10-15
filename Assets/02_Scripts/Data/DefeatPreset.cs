using UnityEngine;

[CreateAssetMenu(fileName = "DefeatPreset", menuName = "Scriptable Objects/DefeatPreset")]
public class DefeatPreset : ScriptableObject
{
    [Header("쓰러짐 슬로 모션")]
    // 삼켜지는 연출이 끝날 때까지 걸린다.
    [SerializeField, Range(0.01f, 1f)] private float _slowTimeScale = 0.3f;

    [Header("시네마틱 진입")]
    [SerializeField] private CinematicSettings _cinematic = new();

    [Header("먹물에 삼켜짐")]
    [SerializeField] private InkSwallowSettings _swallow = new();
    // 삼켜진 뒤 결과 화면이 뜨기 전까지 비워 두는 시간.
    [SerializeField, Min(0f)] private float _holdBeforeResult = 0.4f;

    [Header("결과 문구")]
    [SerializeField] private ResultTitleSettings _title = new();

    public float SlowTimeScale { get { return _slowTimeScale; } }

    public CinematicSettings Cinematic { get { return _cinematic; } }

    public InkSwallowSettings Swallow { get { return _swallow; } }
    public float HoldBeforeResult { get { return _holdBeforeResult; } }

    public ResultTitleSettings Title { get { return _title; } }
}
