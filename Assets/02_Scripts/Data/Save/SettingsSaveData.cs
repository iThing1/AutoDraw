using System;
using UnityEngine;

// 해상도·창 모드는 유니티가 스스로 기억하므로 넣지 않는다.
[Serializable]
public class SettingsSaveData
{
    [SerializeField] private float _masterVolume = 1f;
    [SerializeField] private float _bgmVolume = 1f;
    [SerializeField] private float _sfxVolume = 1f;
    [SerializeField] private bool _isVSyncOn = true;

    public float MasterVolume { get { return _masterVolume; } }
    public float BgmVolume { get { return _bgmVolume; } }
    public float SfxVolume { get { return _sfxVolume; } }
    public bool IsVSyncOn { get { return _isVSyncOn; } }

    // JsonUtility가 불러올 때 쓴다.
    public SettingsSaveData()
    {
    }

    public SettingsSaveData(float masterVolume, float bgmVolume, float sfxVolume, bool isVSyncOn)
    {
        _masterVolume = masterVolume;
        _bgmVolume = bgmVolume;
        _sfxVolume = sfxVolume;
        _isVSyncOn = isVSyncOn;
    }
}
