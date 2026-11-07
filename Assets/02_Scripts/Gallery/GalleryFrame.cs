using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using TMPro;
using UnityEngine;

// 갤러리 벽의 액자. 스테이지가 비어 있거나 아직 열리지 않았으면 천으로 덮인 잠긴 액자이고, 깬 스테이지는 완성된 그림으로 바뀐다.
public class GalleryFrame : GalleryInteractable
{
    private static readonly int BASE_MAP_ID = Shader.PropertyToID("_BaseMap");

    // 비워 두면 잠긴 액자가 된다.
    [SerializeField] private StageData _stage;

    [Header("모양")]
    [SerializeField] private Renderer _painting;
    [SerializeField] private GameObject _cloth;
    // 들어갈 때 카메라가 다가가 멈추는 자리. 그림이 화면을 거의 채우는 거리에 둔다.
    [SerializeField] private Transform _viewPoint;

    [Header("이름표")]
    [SerializeField] private TMP_Text _nameplate;
    [SerializeField] private string _clearedNameplateFormat = "{0}\n<size=60%>CLEARED</size>";

    [Header("열림 연출")]
    [SerializeField, Min(0f)] private float _unveilDuration = 0.6f;
    // 천이 접히며 내려가는 거리. 천 높이의 절반쯤이면 아랫단으로 모인다.
    [SerializeField, Min(0f)] private float _unveilDrop = 0.55f;
    [SerializeField] private Ease _unveilEase = Ease.InQuad;

    private MaterialPropertyBlock _propertyBlock;
    private Texture2D _savedPainting;
    private bool _isPaintingLoaded;
    private bool _isOpen;

    public StageData Stage { get { return _stage; } }
    public override bool IsLocked { get { return false == _isOpen; } }
    public Transform ViewPoint { get { return _viewPoint; } }
    public override string Label { get { return IsLocked ? LOCKED_LABEL : _stage.DisplayName; } }

    protected override void Awake()
    {
        base.Awake();
        _propertyBlock = new MaterialPropertyBlock();
    }

    // 파일에서 불러온 그림은 에셋이 아니라 직접 지워야 남지 않는다.
    private void OnDestroy()
    {
        ReleaseSavedPainting();
    }

    // 열어도 되는지는 전시실이 판단해 넘겨준다.
    public void Refresh(bool isOpen)
    {
        _isOpen = null != _stage && isOpen;

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

        // 그림을 찍지 못하고 깬 경우에도 맞도록, 깼는지는 그림 파일이 아닌 진행 기록으로 본다.
        bool isCleared = GameManager.Player.Progress.IsCleared(_stage.SaveKey);
        _nameplate.text = isCleared ? string.Format(_clearedNameplateFormat, _stage.DisplayName) : _stage.DisplayName;
    }

    // 막 열린 액자는 연출을 볼 때까지 천을 덮어 둔다.
    public void KeepClothForUnveil()
    {
        _cloth.SetActive(true);
    }

    public async UniTask PlayUnveilAsync(CancellationToken token)
    {
        Transform cloth = _cloth.transform;
        Vector3 position = cloth.localPosition;
        Vector3 scale = cloth.localScale;

        Sequence unveil = DOTween.Sequence()
            .Join(cloth.DOLocalMoveY(position.y - _unveilDrop, _unveilDuration))
            .Join(cloth.DOScaleY(0f, _unveilDuration))
            .SetEase(_unveilEase);

        try
        {
            await unveil.ToUniTaskKillOnCancel(token);
        }
        finally
        {
            _cloth.SetActive(false);
            cloth.localPosition = position;
            cloth.localScale = scale;
        }
    }

    public void MarkPaintingChanged()
    {
        _isPaintingLoaded = false;
    }

    private void LoadPainting()
    {
        ReleaseSavedPainting();
        _savedPainting = PaintingStorage.Load(_stage);
        _isPaintingLoaded = true;

        Texture painting = null != _savedPainting ? _savedPainting : _stage.BlankCanvas;

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

    public override void Interact(GalleryStage gallery)
    {
        if (IsLocked)
        {
            PlayLocked();
            return;
        }

        gallery.EnterFrame(this);
    }
}
