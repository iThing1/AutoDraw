// 설정 창을 하나만 열고 닫는다. 타이틀과 일시정지 메뉴가 각자 하나씩 가지고, 자신이 닫힐 때 함께 닫는다.
public class SettingsPopup
{
    private readonly SettingsUI _prefab;
    private SettingsUI _ui;

    public bool IsOpen { get { return null != _ui; } }

    public SettingsPopup(SettingsUI prefab)
    {
        _prefab = prefab;
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        _ui = GameManager.UI.Open(_prefab, UILayer.Popup);

        if (null == _ui)
        {
            return;
        }

        _ui.OnClosed += HandleClosed;
        GameManager.Sound.PlayUi(UISoundKind.Open);
    }

    // 열려 있으면 저장하고 닫는다. Esc처럼 창 밖에서 닫을 때 쓴다.
    public bool TryClose()
    {
        if (false == IsOpen)
        {
            return false;
        }

        _ui.Close();

        return true;
    }

    private void HandleClosed()
    {
        _ui.OnClosed -= HandleClosed;
        GameManager.UI.Close(_ui);
        _ui = null;

        GameManager.Sound.PlayUi(UISoundKind.Close);
    }
}
