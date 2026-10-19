using System;
using UnityEngine;

// 스탯 값은 넣지 않고 고른 것만 넣어, 표의 밸런스를 바꾸면 예전 저장에도 적용되게 한다.
[Serializable]
public class PlayerSaveData
{
    // 저장 형식이 바뀌면 올린다.
    public const int CURRENT_VERSION = 1;

    [SerializeField] private int _version = CURRENT_VERSION;

    public int Version { get { return _version; } }
}
