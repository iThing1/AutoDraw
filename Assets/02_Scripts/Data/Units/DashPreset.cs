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

    [Header("잔상")]
    // 비우면 잔상을 남기지 않는다.
    [SerializeField] private GameObject _afterimagePrefab;
    [SerializeField] private Color _afterimageColor = new Color(0.55f, 0.9f, 0.65f, 0.5f);
    [SerializeField, Min(0.01f)] private float _afterimageInterval = 0.03f;
    [SerializeField, Min(0f)] private float _afterimageFadeDuration = 0.25f;

    [Header("붓 자국")]
    // 가로로 이어 붙여도 이음매가 없는 띠 그림. 비우면 자국을 남기지 않는다.
    [SerializeField] private Texture2D _strokeTexture;
    // 띠 그림 한 장이 덮는 길이.
    [SerializeField, Min(0.1f)] private float _strokeTileLength = 3.2f;
    [SerializeField, Min(0.1f)] private float _strokeWidth = 0.8f;

    public string DisplayName { get { return _displayName; } }
    public Color IconColor { get { return _iconColor; } }

    public float Duration { get { return _duration; } }
    public Ease Ease { get { return _ease; } }

    public ImpactSettings StartImpact { get { return _startImpact; } }

    public GameObject AfterimagePrefab { get { return _afterimagePrefab; } }
    public Color AfterimageColor { get { return _afterimageColor; } }
    public float AfterimageInterval { get { return _afterimageInterval; } }
    public float AfterimageFadeDuration { get { return _afterimageFadeDuration; } }

    public Texture2D StrokeTexture { get { return _strokeTexture; } }
    public float StrokeTileLength { get { return _strokeTileLength; } }
    public float StrokeWidth { get { return _strokeWidth; } }
}
