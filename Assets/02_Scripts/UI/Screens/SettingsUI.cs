using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 설정 창. 값은 바꾸는 즉시 적용하고, 닫을 때 저장한다. 창을 닫는 것은 연 쪽이 한다.
public class SettingsUI : MonoBehaviour
{
    private const string RESOLUTION_FORMAT = "{0} x {1}";

    [Header("볼륨")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    [Header("화면")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _fullScreenToggle;
    [SerializeField] private Toggle _vSyncToggle;

    [SerializeField] private Button _closeButton;

    public event Action OnClosed;

    private void Awake()
    {
        SettingsManager settings = GameManager.Settings;

        _masterSlider.SetValueWithoutNotify(settings.MasterVolume);
        _bgmSlider.SetValueWithoutNotify(settings.BgmVolume);
        _sfxSlider.SetValueWithoutNotify(settings.SfxVolume);
        _fullScreenToggle.SetIsOnWithoutNotify(settings.IsFullScreen);
        _vSyncToggle.SetIsOnWithoutNotify(settings.IsVSyncOn);
        FillResolutions(settings);

        _masterSlider.onValueChanged.AddListener(settings.SetMasterVolume);
        _bgmSlider.onValueChanged.AddListener(settings.SetBgmVolume);
        _sfxSlider.onValueChanged.AddListener(settings.SetSfxVolume);
        _resolutionDropdown.onValueChanged.AddListener(settings.SetResolution);
        _fullScreenToggle.onValueChanged.AddListener(HandleFullScreenChanged);
        _vSyncToggle.onValueChanged.AddListener(HandleVSyncChanged);
        _closeButton.onClick.AddListener(HandleCloseClicked);
    }

    // Esc로 닫을 때처럼 버튼을 거치지 않고 닫을 때도 연 쪽이 부른다.
    public void Close()
    {
        GameManager.Settings.Save();
        OnClosed?.Invoke();
    }

    // 에디터처럼 해상도 목록을 얻지 못하면 고를 수 없게 둔다.
    private void FillResolutions(SettingsManager settings)
    {
        IReadOnlyList<Vector2Int> resolutions = settings.Resolutions;
        List<string> labels = new List<string>(resolutions.Count);

        for (int i = 0; i < resolutions.Count; i++)
        {
            labels.Add(string.Format(RESOLUTION_FORMAT, resolutions[i].x, resolutions[i].y));
        }

        _resolutionDropdown.ClearOptions();
        _resolutionDropdown.AddOptions(labels);
        _resolutionDropdown.interactable = 0 < labels.Count;

        if (0 < labels.Count)
        {
            _resolutionDropdown.SetValueWithoutNotify(settings.GetCurrentResolutionIndex());
        }
    }

    private void HandleFullScreenChanged(bool isOn)
    {
        GameManager.Settings.SetFullScreen(isOn);
        GameManager.Sound.PlayUi(UISoundKind.Click);
    }

    private void HandleVSyncChanged(bool isOn)
    {
        GameManager.Settings.SetVSync(isOn);
        GameManager.Sound.PlayUi(UISoundKind.Click);
    }

    private void HandleCloseClicked()
    {
        Close();
    }
}
