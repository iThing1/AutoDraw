using UnityEngine;

// 갤러리에서 바라보고 상호작용 키로 고르는 물건. 콜라이더와 같은 오브젝트에 둔다. 바라보는 동안 렌더러 하나의 색을 바꿔 강조한다.
public abstract class GalleryInteractable : MonoBehaviour
{
    private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

    // HUD 안내에 "[키] 이 문구"로 보인다.
    [SerializeField] private string _actionLabel = "ENTER";

    [Header("강조")]
    [SerializeField] private Renderer _highlightRenderer;
    [SerializeField] private Color _highlightColor = new Color(0.85f, 0.65f, 0.3f, 1f);

    private MaterialPropertyBlock _highlightBlock;
    private Color _baseColor;

    public string ActionLabel { get { return _actionLabel; } }
    public abstract string Label { get; }
    public virtual bool IsLocked { get { return false; } }

    // 상호작용 키를 눌렀다. 무엇을 할지는 물건마다 다르고, 갤러리의 일(액자 들어가기·창 열기)은 gallery에 맡긴다.
    public abstract void Interact(GalleryStage gallery);

    protected virtual void Awake()
    {
        if (null == _highlightRenderer)
        {
            Debug.LogError($"갤러리 물건 : {name}에 강조할 렌더러가 지정되지 않아 강조하지 않습니다.");
            return;
        }

        _highlightBlock = new MaterialPropertyBlock();
        _baseColor = _highlightRenderer.sharedMaterial.GetColor(BASE_COLOR_ID);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        if (null == _highlightRenderer)
        {
            return;
        }

        _highlightRenderer.GetPropertyBlock(_highlightBlock);
        _highlightBlock.SetColor(BASE_COLOR_ID, isHighlighted ? _highlightColor : _baseColor);
        _highlightRenderer.SetPropertyBlock(_highlightBlock);
    }
}
