using TMPro;
using UnityEngine;

// 갤러리 화면 가운데 조준점과, 바라보는 액자의 이름·상호작용 키 안내.
public class GalleryHudUI : MonoBehaviour
{
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private TMP_Text _prompt;
    [SerializeField] private string _enterFormat = "[{0}] ENTER";

    // 안내 문구는 늘 같아 처음에 한 번만 만든다.
    public void Setup(string interactKey)
    {
        _prompt.text = string.Format(_enterFormat, interactKey);
        ShowFocus(null);
    }

    public void SetCrosshairVisible(bool isVisible)
    {
        _crosshair.SetActive(isVisible);
    }

    // 잠긴 액자는 이름 대신 잠김만 보여 주고, 들어갈 수 없으니 키 안내도 띄우지 않는다.
    public void ShowFocus(GalleryFrame frame)
    {
        bool hasFrame = null != frame;

        _label.gameObject.SetActive(hasFrame);
        _prompt.gameObject.SetActive(hasFrame && false == frame.IsLocked);

        if (hasFrame)
        {
            _label.text = frame.Label;
        }
    }
}
