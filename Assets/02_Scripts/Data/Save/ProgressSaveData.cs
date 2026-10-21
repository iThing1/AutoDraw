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
        _clearedStageKeys = ToArray(progress.ClearedStageKeys);
        _perkIds = ToArray(progress.PerkIds);
        _earnedPoints = progress.EarnedPoints;
    }

    private static string[] ToArray(IReadOnlyList<string> list)
    {
        string[] array = new string[list.Count];

        for (int i = 0; i < list.Count; i++)
        {
            array[i] = list[i];
        }

        return array;
    }
}
