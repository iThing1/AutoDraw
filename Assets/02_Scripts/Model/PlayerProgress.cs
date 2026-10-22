using System;
using System.Collections.Generic;

// 깬 스테이지, 얻은 특성 포인트, 찍은 특성, 주운 팔레트. 남은 포인트는 찍은 특성의 비용을 빼서 구해, 표의 비용을 바꿔도 저장과 어긋나지 않게 한다.
// 찍을 수 있는지는 PerkTree가 정하고, 여기는 기록만 한다.
public class PlayerProgress
{
    private readonly List<string> _clearedStageKeys = new();
    private readonly List<string> _perkIds = new();
    private int _earnedPoints;
    private readonly List<string> _paletteIds = new();
    private int _usedPalettes;

    public IReadOnlyList<string> ClearedStageKeys { get { return _clearedStageKeys; } }
    // 찍은 순서대로다.
    public IReadOnlyList<string> PerkIds { get { return _perkIds; } }
    public int EarnedPoints { get { return _earnedPoints; } }
    public IReadOnlyList<string> PaletteIds { get { return _paletteIds; } }
    public int UsedPalettes { get { return _usedPalettes; } }
    public int AvailablePalettes { get { return Math.Max(0, _paletteIds.Count - _usedPalettes); } }

    private PlayerProgress()
    {
    }

    public static PlayerProgress CreateNew()
    {
        return new PlayerProgress();
    }

    // 진행을 저장하기 전 파일이면 save가 null이라 처음 상태로 시작한다.
    public static PlayerProgress FromSave(ProgressSaveData save)
    {
        PlayerProgress progress = new();

        if (null != save)
        {
            foreach (string stageKey in save.ClearedStageKeys)
            {
                progress.AddCleared(stageKey);
            }

            foreach (string perkId in save.PerkIds)
            {
                progress.AddPerk(perkId);
            }

            foreach (string paletteId in save.PaletteIds)
            {
                progress.TryAddPalette(paletteId);
            }

            progress._earnedPoints = save.EarnedPoints;
            progress._usedPalettes = save.UsedPalettes;
        }

        return progress;
    }

    public ProgressSaveData ToSaveData()
    {
        return new ProgressSaveData(this);
    }

    public bool IsCleared(string stageKey)
    {
        return _clearedStageKeys.Contains(stageKey);
    }

    // 처음 깬 스테이지만 기록하고 포인트를 더한다. 이미 깬 곳이면 false다.
    public bool TryMarkCleared(string stageKey, int rewardPoints)
    {
        if (false == AddCleared(stageKey))
        {
            return false;
        }

        _earnedPoints += rewardPoints;

        return true;
    }

    public bool HasPerk(string perkId)
    {
        return _perkIds.Contains(perkId);
    }

    public void AddPerk(string perkId)
    {
        if (false == string.IsNullOrEmpty(perkId) && false == _perkIds.Contains(perkId))
        {
            _perkIds.Add(perkId);
        }
    }

    public void RemovePerk(string perkId)
    {
        _perkIds.Remove(perkId);
    }

    public void ClearPerks()
    {
        _perkIds.Clear();
    }

    public bool HasPalette(string paletteId)
    {
        return _paletteIds.Contains(paletteId);
    }

    public bool TryAddPalette(string paletteId)
    {
        if (string.IsNullOrEmpty(paletteId) || _paletteIds.Contains(paletteId))
        {
            return false;
        }

        _paletteIds.Add(paletteId);

        return true;
    }

    // 남은 팔레트가 있을 때만 부른다.
    public void UsePalette()
    {
        _usedPalettes++;
    }

    // 시험 메뉴에서만 쓴다. 실제 포인트는 처음 깬 스테이지에서만 얻는다.
    public void AddPoints(int points)
    {
        _earnedPoints += points;
    }

    private bool AddCleared(string stageKey)
    {
        if (string.IsNullOrEmpty(stageKey) || _clearedStageKeys.Contains(stageKey))
        {
            return false;
        }

        _clearedStageKeys.Add(stageKey);

        return true;
    }
}
