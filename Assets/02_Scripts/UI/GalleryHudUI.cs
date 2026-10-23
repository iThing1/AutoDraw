using DG.Tweening;
using TMPro;
using UnityEngine;

// 갤러리 화면 가운데 조준점과, 바라보는 물건의 이름·상호작용 키 안내, 팔레트를 주웠을 때의 알림.
public class GalleryHudUI : MonoBehaviour
{
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _prompt;
    // {0}은 키 이름, {1}은 물건마다 정한 동작 문구.
    [SerializeField] private string _promptFormat = "[{0}] {1}";

    [Header("알림")]
    [SerializeField] private CanvasGroup _notice;
    [SerializeField] private TMP_Text _noticeText;
    // {0}은 남은 팔레트 수.
    [SerializeField] private string _paletteFoundFormat = "PALETTE FOUND  x{0}";
    [SerializeField, Min(0f)] private float _noticeHoldDuration = 1.5f;
    [SerializeField, Min(0f)] private float _noticeFadeDuration = 0.4f;

    private string _interactKey;

    private void Awake()
    {
        _notice.alpha = 0f;
    }

    public void Setup(string interactKey)
    {
        _interactKey = interactKey;
        ShowFocus(null);
    }

    public void SetCrosshairVisible(bool isVisible)
    {
        _crosshair.SetActive(isVisible);
    }

    public void ShowPaletteFound(int availablePalettes)
    {
        _noticeText.SetText(_paletteFoundFormat, availablePalettes);

        _notice.DOKill();
        _notice.alpha = 1f;
        _notice.DOFade(0f, _noticeFadeDuration)
            .SetDelay(_noticeHoldDuration)
            .SetLink(gameObject);
    }

    // 잠긴 물건은 이름 대신 잠김만 보여 주고, 고를 수 없으니 키 안내도 띄우지 않는다.
    public void ShowFocus(GalleryInteractable target)
    {
        bool hasTarget = null != target;

        _label.gameObject.SetActive(hasTarget);
        _prompt.gameObject.SetActive(hasTarget && false == target.IsLocked);

        if (hasTarget)
        {
            _label.text = target.Label;
            _prompt.text = string.Format(_promptFormat, _interactKey, target.ActionLabel);
        }
    }
}
