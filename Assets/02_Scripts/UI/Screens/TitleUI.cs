using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

// 타이틀 화면. 시작을 누르면 알리고, 설정·종료는 여기서 바로 처리한다. 닫는 것은 연 쪽이 한다.
public class TitleUI : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private SettingsUI _settingsPrefab;

    private SettingsPopup _settings;

    private void Awake()
    {
        _settings = new SettingsPopup(_settingsPrefab);

        _settingsButton.onClick.AddListener(_settings.Open);
        _quitButton.onClick.AddListener(AppQuitter.Quit);
    }

    // 설정 창이 시작 버튼을 다 가리지 않을 수 있어, 시작하면 열린 설정 창도 저장하고 닫는다.
    public async UniTask WaitForStartAsync(CancellationToken token)
    {
        await _startButton.OnClickAsync(token);

        _settings.TryClose();
        GameManager.Sound.PlayUi(UISoundKind.Confirm);
    }
}
