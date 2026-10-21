using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProgressSaveData
{
    [SerializeField] private string[] _clearedStageKeys;
    [SerializeField] private int _earnedPoints;

    public IReadOnlyList<string> ClearedStageKeys { get { return _clearedStageKeys ?? Array.Empty<string>(); } }
    public int EarnedPoints { get { return _earnedPoints; } }

    // JsonUtility가 불러올 때 쓴다.
    public ProgressSaveData()
    {
    }

    public ProgressSaveData(PlayerProgress progress)
    {
        _clearedStageKeys = new string[progress.ClearedStageKeys.Count];

        for (int i = 0; i < _clearedStageKeys.Length; i++)
        {
            _clearedStageKeys[i] = progress.ClearedStageKeys[i];
        }

        _earnedPoints = progress.EarnedPoints;
    }
}
