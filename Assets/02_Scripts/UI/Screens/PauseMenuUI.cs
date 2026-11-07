using System;
using UnityEngine;
using UnityEngine.UI;

// 일시정지 메뉴. 계속하기·스테이지 떠나기는 메뉴를 연 쪽이 닫고, 설정·종료는 여기서 바로 처리한다.
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private SettingsUI _settingsPrefab;

    [Header("전투 전용")]
    // 프리팹에서는 꺼 두고, 전투에서 열 때만 켠다.
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _galleryButton;

    private SettingsPopup _settings;

    public event Action OnResume;
    // 다시 하기·갤러리로를 고르면 알린다. 결과 화면과 같은 선택지를 쓴다.
    public event Action<ResultChoice> OnLeaveChosen;

    private void Awake()
    {
        _settings = new SettingsPopup(_settingsPrefab);

        _resumeButton.onClick.AddListener(HandleResumeClicked);
        _settingsButton.onClick.AddListener(_settings.Open);
        _quitButton.onClick.AddListener(AppQuitter.Quit);
        _retryButton.onClick.AddListener(HandleRetryClicked);
        _galleryButton.onClick.AddListener(HandleGalleryClicked);
    }

    public void ShowStageButtons()
    {
        _retryButton.gameObject.SetActive(true);
        _galleryButton.gameObject.SetActive(true);
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

    private void HandleRetryClicked()
    {
        OnLeaveChosen?.Invoke(ResultChoice.Retry);
    }

    private void HandleGalleryClicked()
    {
        OnLeaveChosen?.Invoke(ResultChoice.Gallery);
    }
}
