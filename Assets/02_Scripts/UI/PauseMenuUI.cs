using System;
using UnityEngine;
using UnityEngine.UI;

// 일시정지 메뉴. 계속하기는 메뉴를 연 쪽이 닫고, 설정·종료는 여기서 바로 처리한다.
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private SettingsUI _settingsPrefab;

    private SettingsPopup _settings;

    public event Action OnResume;

    private void Awake()
    {
        _settings = new SettingsPopup(_settingsPrefab);

        _resumeButton.onClick.AddListener(HandleResumeClicked);
        _settingsButton.onClick.AddListener(_settings.Open);
        _quitButton.onClick.AddListener(AppQuitter.Quit);
    }

    // 설정 창은 메뉴와 따로 열려 있으므로, 메뉴를 닫기 전이나 Esc를 눌렀을 때 연 쪽이 먼저 부른다.
    public bool TryCloseSettings()
    {
        return _settings.TryClose();
    }

    private void HandleResumeClicked()
    {
        OnResume?.Invoke();
    }
}
