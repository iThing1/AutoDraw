using DG.Tweening;
using UnityEngine;

// 다음 전시실로 가는 문. 전시실을 모두 채워야 열리고, 처음 열어 보기 전까지 위에 알림 표시를 띄운다.
public class GalleryDoor : GalleryInteractable
{
    [SerializeField] private string _label = "NEXT ROOM";

    [Header("알림 표시")]
    [SerializeField] private Renderer _alertMark;
    [SerializeField] private Color _alertColor = new Color(1f, 0.85f, 0.65f, 1f);
    [SerializeField, Min(0f)] private float _alertBobHeight = 0.12f;
    [SerializeField, Min(0.05f)] private float _alertBobDuration = 0.6f;

    private Vector3 _alertHome;
    private bool _isOpen;

    public override bool IsLocked { get { return false == _isOpen; } }
    public override string Label { get { return IsLocked ? LOCKED_LABEL : _label; } }

    protected override void Awake()
    {
        base.Awake();

        _alertHome = _alertMark.transform.localPosition;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor(BASE_COLOR_ID, _alertColor);
        _alertMark.SetPropertyBlock(block);
    }

    public void Refresh(bool isOpen, bool showsAlert)
    {
        _isOpen = isOpen;

        Transform mark = _alertMark.transform;

        mark.DOKill();
        mark.localPosition = _alertHome;
        _alertMark.gameObject.SetActive(showsAlert);

        if (showsAlert)
        {
            mark.DOLocalMoveY(_alertHome.y + _alertBobHeight, _alertBobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(mark.gameObject);
        }
    }

    public override void Interact(GalleryStage gallery)
    {
        if (IsLocked)
        {
            PlayLocked();
            return;
        }

        gallery.UseDoor(this);
    }
}
