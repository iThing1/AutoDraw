using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

// 다음 전시실로 가는 문. 전시실을 모두 채워야 자물쇠가 풀리고, 처음 열어 보기 전까지 위에 알림 표시를 띄운다.
public class GalleryDoor : GalleryInteractable
{
    [SerializeField] private string _label = "NEXT ROOM";
    // 잠금이 풀리는 모습을 보여 줄 때 카메라가 서는 자리.
    [SerializeField] private Transform _viewPoint;

    [Header("알림 표시")]
    [SerializeField] private Renderer _alertMark;
    [SerializeField] private Color _alertColor = new Color(1f, 0.85f, 0.65f, 1f);
    [SerializeField, Min(0f)] private float _alertBobHeight = 0.12f;
    [SerializeField, Min(0.05f)] private float _alertBobDuration = 0.6f;

    [Header("자물쇠")]
    [SerializeField] private Transform _lock;
    [SerializeField] private Transform _lockShackle;
    // 잠긴 문을 눌렀을 때 고리를 축으로 흔들리는 각도.
    [SerializeField] private float _lockSwingAngle = 16f;
    [SerializeField, Min(0f)] private float _lockSwingDuration = 0.7f;
    [SerializeField, Min(0f)] private float _shackleLift = 0.1f;
    [SerializeField, Min(0f)] private float _shackleDuration = 0.25f;
    [SerializeField, Min(0f)] private float _unlockHoldDuration = 0.35f;
    // 자물쇠가 떨어져 멈추는 문 기준 높이.
    [SerializeField] private float _lockFloorY = 0.1f;
    [SerializeField, Min(0f)] private float _lockDropDuration = 0.45f;
    [SerializeField] private float _lockDropTilt = 35f;
    [SerializeField, Min(0f)] private float _lockVanishDuration = 0.15f;
    [SerializeField, Min(0f)] private float _unlockFlashDuration = 0.18f;
    [SerializeField] private AudioClip _unlockSound;
    [SerializeField] private AudioClip _lockDropSound;

    private Vector3 _alertHome;
    private bool _isOpen;

    public override bool IsLocked { get { return false == _isOpen; } }
    public override string Label { get { return IsLocked ? LOCKED_LABEL : _label; } }
    public Transform ViewPoint { get { return _viewPoint; } }

    protected override void Awake()
    {
        base.Awake();

        _alertHome = _alertMark.transform.localPosition;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor(BASE_COLOR_ID, _alertColor);
        _alertMark.SetPropertyBlock(block);
    }

    protected override void PlayLocked()
    {
        _lock.DOComplete();
        _lock.DOPunchRotation(new Vector3(0f, 0f, _lockSwingAngle), _lockSwingDuration, 5, 0.6f)
            .SetLink(_lock.gameObject);
    }

    public void Refresh(bool isOpen, bool showsAlert)
    {
        _isOpen = isOpen;
        _lock.gameObject.SetActive(false == isOpen);

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

    // 막 열린 문은 연출을 볼 때까지 자물쇠를 걸어 둔다.
    public void KeepLockForUnlock()
    {
        _lock.gameObject.SetActive(true);
        _alertMark.gameObject.SetActive(false);
    }

    public async UniTask PlayUnlockAsync(CancellationToken token)
    {
        Vector3 lockPosition = _lock.localPosition;
        Quaternion lockRotation = _lock.localRotation;
        Vector3 lockScale = _lock.localScale;
        Vector3 shacklePosition = _lockShackle.localPosition;

        GameManager.Sound.PlaySfx(_unlockSound);

        Sequence unlock = DOTween.Sequence()
            .Append(_lockShackle.DOLocalMoveY(shacklePosition.y + _shackleLift, _shackleDuration).SetEase(Ease.OutBack))
            .Join(_lock.DOPunchScale(Vector3.one * 0.2f, _shackleDuration, 6, 0.5f))
            .AppendInterval(_unlockHoldDuration)
            .Append(_lock.DOLocalMoveY(_lockFloorY, _lockDropDuration).SetEase(Ease.InQuad))
            .Join(_lock.DOLocalRotate(new Vector3(0f, 0f, _lockDropTilt), _lockDropDuration))
            .AppendCallback(() => GameManager.Sound.PlaySfx(_lockDropSound))
            .Append(_lock.DOScale(0f, _lockVanishDuration))
            .AppendCallback(() => SetHighlighted(true))
            .AppendInterval(_unlockFlashDuration)
            .AppendCallback(() => SetHighlighted(false))
            .AppendInterval(_unlockFlashDuration)
            .AppendCallback(() => SetHighlighted(true))
            .AppendInterval(_unlockFlashDuration);

        try
        {
            await unlock.ToUniTaskKillOnCancel(token);
        }
        finally
        {
            SetHighlighted(false);
            _lock.SetLocalPositionAndRotation(lockPosition, lockRotation);
            _lock.localScale = lockScale;
            _lockShackle.localPosition = shacklePosition;
        }

        _lock.gameObject.SetActive(false);
    }

    public override void Interact(GalleryStage gallery)
    {
        if (IsLocked)
        {
            PlayLocked();
            return;
        }

        gallery.UseDoor();
    }
}
