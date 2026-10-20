using TMPro;
using UnityEngine;

// 갤러리 화면 가운데 조준점과, 바라보는 물건의 이름·상호작용 키 안내.
public class GalleryHudUI : MonoBehaviour
{
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _prompt;
    // {0}은 키 이름, {1}은 물건마다 정한 동작 문구.
    [SerializeField] private string _promptFormat = "[{0}] {1}";

    private string _interactKey;

    public void Setup(string interactKey)
    {
        _interactKey = interactKey;
        ShowFocus(null);
    }

    public void SetCrosshairVisible(bool isVisible)
    {
        _crosshair.SetActive(isVisible);
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
