using System.Collections.Generic;
using UnityEngine;

// 볼륨·화면 설정을 바로 적용하고, 창을 닫을 때 저장한다. 해상도·창 모드는 유니티가 기억하므로 화면 값을 그대로 읽는다.
public class SettingsManager
{
    private const string FILE_NAME = "Settings.json";
    private const string LABEL = "설정 저장";

    private readonly List<Vector2Int> _resolutions = new();
    private SoundManager _sound;
    private float _masterVolume;
    private float _bgmVolume;
    private float _sfxVolume;
    private bool _isVSyncOn;
    private bool _isDirty;

    public float MasterVolume { get { return _masterVolume; } }
    public float BgmVolume { get { return _bgmVolume; } }
    public float SfxVolume { get { return _sfxVolume; } }
    public bool IsVSyncOn { get { return _isVSyncOn; } }
    public bool IsFullScreen { get { return FullScreenMode.Windowed != Screen.fullScreenMode; } }
    // 화면 주사율만 다른 해상도는 하나로 묶는다. 작은 것부터 놓인다.
    public IReadOnlyList<Vector2Int> Resolutions { get { return _resolutions; } }

    // 첫 실행이면 지금 품질 설정의 수직 동기화를 그대로 쓴다.
    public void Init(SoundManager sound)
    {
        _sound = sound;

        SettingsSaveData save = JsonFileStorage.TryLoad(FILE_NAME, LABEL, out SettingsSaveData loaded)
            ? loaded
            : new SettingsSaveData(1f, 1f, 1f, 0 < QualitySettings.vSyncCount);

        _masterVolume = Mathf.Clamp01(save.MasterVolume);
        _bgmVolume = Mathf.Clamp01(save.BgmVolume);
        _sfxVolume = Mathf.Clamp01(save.SfxVolume);
        _isVSyncOn = save.IsVSyncOn;

        ApplyVolumes();
        ApplyVSync();
        CollectResolutions();

        _isDirty = false;
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetBgmVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetVSync(bool isOn)
    {
        _isVSyncOn = isOn;
        _isDirty = true;
        ApplyVSync();
    }

    // 창 모드로 바꿀 때도 지금 해상도는 그대로 둔다.
    public void SetFullScreen(bool isOn)
    {
        Screen.fullScreenMode = isOn ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= _resolutions.Count)
        {
            return;
        }

        Screen.SetResolution(_resolutions[index].x, _resolutions[index].y, Screen.fullScreenMode);
    }

    // 지금 화면 크기와 같은 해상도가 목록에 없으면(창 크기를 직접 바꾼 경우) 가장 큰 것을 가리킨다.
    public int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < _resolutions.Count; i++)
        {
            if (Screen.width == _resolutions[i].x && Screen.height == _resolutions[i].y)
            {
                return i;
            }
        }

        return _resolutions.Count - 1;
    }

    // 슬라이더를 끄는 동안 매번 쓰지 않도록 바뀐 것이 있을 때 한 번만 쓴다.
    public void Save()
    {
        if (false == _isDirty)
        {
            return;
        }

        JsonFileStorage.Save(FILE_NAME, new SettingsSaveData(_masterVolume, _bgmVolume, _sfxVolume, _isVSyncOn));
        _isDirty = false;
    }

    private void ApplyVolumes()
    {
        _isDirty = true;
        _sound.SetVolumes(_masterVolume * _bgmVolume, _masterVolume * _sfxVolume);
    }

    private void ApplyVSync()
    {
        QualitySettings.vSyncCount = _isVSyncOn ? 1 : 0;
    }

    // Screen.resolutions는 작은 해상도부터, 같은 크기는 주사율 순으로 놓여 있다.
    private void CollectResolutions()
    {
        Resolution[] resolutions = Screen.resolutions;

        for (int i = 0; i < resolutions.Length; i++)
        {
            Vector2Int size = new Vector2Int(resolutions[i].width, resolutions[i].height);

            if (0 == _resolutions.Count || size != _resolutions[_resolutions.Count - 1])
            {
                _resolutions.Add(size);
            }
        }
    }
}
