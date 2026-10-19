using DG.Tweening;
using TMPro;
using UnityEngine;

// 갤러리 벽의 액자. 스테이지가 비어 있으면 천으로 덮인 잠긴 액자이고, 깬 스테이지는 완성된 그림으로 바뀐다.
public class GalleryFrame : MonoBehaviour
{
    private const string LOCKED_LABEL = "LOCKED";
    private static readonly int BASE_MAP_ID = Shader.PropertyToID("_BaseMap");
    private static readonly int BASE_COLOR_ID = Shader.PropertyToID("_BaseColor");

    // 비워 두면 잠긴 액자가 된다.
    [SerializeField] private StageData _stage;

    [Header("모양")]
    [SerializeField] private Renderer _painting;
    [SerializeField] private GameObject _cloth;
    [SerializeField] private Renderer _border;
    [SerializeField] private Color _highlightColor = new Color(0.85f, 0.65f, 0.3f, 1f);
    // 들어갈 때 카메라가 다가가 멈추는 자리. 그림이 화면을 거의 채우는 거리에 둔다.
    [SerializeField] private Transform _viewPoint;

    [Header("이름표")]
    [SerializeField] private TMP_Text _nameplate;
    [SerializeField] private string _clearedNameplateFormat = "{0}\n<size=60%>CLEARED</size>";

    [Header("잠김")]
    [SerializeField, Min(0f)] private float _lockedShakeDuration = 0.3f;
    [SerializeField, Min(0f)] private float _lockedShakeStrength = 0.04f;

    private MaterialPropertyBlock _propertyBlock;
    private Color _borderColor;
    private Texture2D _savedPainting;
    private bool _isPaintingLoaded;

    public StageData Stage { get { return _stage; } }
    public bool IsLocked { get { return null == _stage; } }
    public Transform ViewPoint { get { return _viewPoint; } }
    public string Label { get { return IsLocked ? LOCKED_LABEL : _stage.DisplayName; } }

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        _borderColor = _border.sharedMaterial.GetColor(BASE_COLOR_ID);
    }

    // 파일에서 불러온 그림은 에셋이 아니라 직접 지워야 남지 않는다.
    private void OnDestroy()
    {
        ReleaseSavedPainting();
    }

    // 깨기 전에는 빈 도화지를, 깬 뒤에는 마지막으로 깬 판에서 칠해 찍어 둔 그림을 건다. 이름표에도 깼는지 적는다.
    public void Refresh()
    {
        _cloth.SetActive(IsLocked);
        _painting.gameObject.SetActive(false == IsLocked);

        if (IsLocked)
        {
            _nameplate.text = LOCKED_LABEL;
            return;
        }

        // 그림 파일은 깼을 때만 바뀌므로, 처음 한 번과 새로 깬 뒤에만 다시 읽는다.
        if (false == _isPaintingLoaded)
        {
            LoadPainting();
        }
    }

    // 새로 깨 그림 파일이 바뀌었으니 다음 Refresh에서 다시 읽게 한다.
    public void MarkPaintingChanged()
    {
        _isPaintingLoaded = false;
    }

    private void LoadPainting()
    {
        ReleaseSavedPainting();
        _savedPainting = PaintingStorage.Load(_stage);
        _isPaintingLoaded = true;

        Texture painting = _stage.BlankCanvas;
        string nameplate = _stage.DisplayName;

        if (null != _savedPainting)
        {
            painting = _savedPainting;
            nameplate = string.Format(_clearedNameplateFormat, _stage.DisplayName);
        }

        _nameplate.text = nameplate;

        // 액자마다 그림만 다르므로 재질은 하나를 함께 쓰고 그림만 따로 넣는다.
        _painting.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetTexture(BASE_MAP_ID, painting);
        _painting.SetPropertyBlock(_propertyBlock);
    }

    private void ReleaseSavedPainting()
    {
        if (null != _savedPainting)
        {
            Destroy(_savedPainting);
            _savedPainting = null;
        }
    }

    public void SetHighlighted(bool isHighlighted)
    {
        Color color = _borderColor;

        if (isHighlighted)
        {
            color = _highlightColor;
        }

        _border.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BASE_COLOR_ID, color);
        _border.SetPropertyBlock(_propertyBlock);
    }

    public void PlayLocked()
    {
        transform.DOComplete();
        transform.DOShakePosition(_lockedShakeDuration, _lockedShakeStrength);
    }
}
