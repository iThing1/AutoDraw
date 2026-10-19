using System;
using UnityEngine;

// 플레이어 저장 파일의 모양. 스탯 값은 넣지 않고 플레이어가 고른 것(찍은 특성 등)만 넣어, 표의 밸런스를 바꾸면 예전 저장에도 바로 적용되게 한다.
[Serializable]
public class PlayerSaveData
{
    // 저장 형식이 바뀌면 올린다. 예전 형식 파일을 불러와 옮길 때 기준으로 쓴다.
    public const int CURRENT_VERSION = 1;

    [SerializeField] private int _version = CURRENT_VERSION;

    public int Version { get { return _version; } }
}
