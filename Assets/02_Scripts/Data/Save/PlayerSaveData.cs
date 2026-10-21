using System;
using UnityEngine;

// 스탯 값은 넣지 않고 고른 것만 넣어, 표의 밸런스를 바꾸면 예전 저장에도 적용되게 한다.
[Serializable]
public class PlayerSaveData
{
    // 저장 형식이 바뀌면 올린다.
    public const int CURRENT_VERSION = 1;

    [SerializeField] private int _version = CURRENT_VERSION;
    [SerializeField] private LoadoutSaveData _loadout;
    [SerializeField] private ProgressSaveData _progress;

    public int Version { get { return _version; } }
    // 장비를 저장하기 전 파일이면 비어 있거나 null이다.
    public LoadoutSaveData Loadout { get { return _loadout; } }
    // 진행을 저장하기 전 파일이면 비어 있거나 null이다.
    public ProgressSaveData Progress { get { return _progress; } }

    // JsonUtility가 불러올 때 쓴다.
    public PlayerSaveData()
    {
    }

    public PlayerSaveData(LoadoutSaveData loadout, ProgressSaveData progress)
    {
        _loadout = loadout;
        _progress = progress;
    }
}
