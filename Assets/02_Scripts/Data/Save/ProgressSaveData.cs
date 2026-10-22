using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProgressSaveData
{
    [SerializeField] private string[] _clearedStageKeys;
    [SerializeField] private string[] _perkIds;
    [SerializeField] private int _earnedPoints;

    public IReadOnlyList<string> ClearedStageKeys { get { return _clearedStageKeys ?? Array.Empty<string>(); } }
    public IReadOnlyList<string> PerkIds { get { return _perkIds ?? Array.Empty<string>(); } }
    public int EarnedPoints { get { return _earnedPoints; } }

    // JsonUtility가 불러올 때 쓴다.
    public ProgressSaveData()
    {
    }

    public ProgressSaveData(PlayerProgress progress)
    {
        _clearedStageKeys = progress.ClearedStageKeys.ToArray();
        _perkIds = progress.PerkIds.ToArray();
        _earnedPoints = progress.EarnedPoints;
    }
}
