using DG.Tweening;
using UnityEngine;

// 거리와 쿨타임은 특성으로 바뀌어 플레이어 스탯에 있고, 여기는 겉모습과 움직임의 느낌만 둔다.
[CreateAssetMenu(fileName = "DashPreset", menuName = "Scriptable Objects/DashPreset")]
public class DashPreset : ScriptableObject
{
    [Header("표시")]
    [SerializeField] private string _displayName = "Dash";
    [SerializeField] private Color _iconColor = new Color(0.55f, 0.9f, 0.65f);

    [Header("움직임")]
    [SerializeField, Min(0.01f)] private float _duration = 0.15f;
    [SerializeField] private Ease _ease = Ease.OutQuad;

    [Header("연출")]
    [SerializeField] private ImpactSettings _startImpact = new();

    public string DisplayName { get { return _displayName; } }
    public Color IconColor { get { return _iconColor; } }

    public float Duration { get { return _duration; } }
    public Ease Ease { get { return _ease; } }

    public ImpactSettings StartImpact { get { return _startImpact; } }
}
