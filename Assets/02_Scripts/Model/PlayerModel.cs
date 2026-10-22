using System;
using UnityEngine;

public class PlayerModel
{
    private readonly PlayerStats _stats;
    private readonly PlayerLoadout _loadout;
    private readonly PlayerProgress _progress;
    private readonly PerkTree _perkTree;

    // 특성을 찍거나 초기화했다. 스탯과 장비는 이미 다시 계산된 뒤다.
    public event Action OnPerksChanged;

    public PlayerStats Stats { get { return _stats; } }
    public PlayerLoadout Loadout { get { return _loadout; } }
    public PlayerProgress Progress { get { return _progress; } }
    public PerkTree PerkTree { get { return _perkTree; } }
    public int AvailablePerkPoints { get { return _perkTree.GetAvailablePoints(_progress); } }
    public int AvailablePalettes { get { return _progress.AvailablePalettes; } }
    public bool CanRespec { get { return 0 < _progress.AvailablePalettes && 0 < _progress.PerkIds.Count; } }

    private PlayerModel(PlayerStats stats, PlayerLoadout loadout, PlayerProgress progress, DataTableManager tables)
    {
        _stats = stats;
        _loadout = loadout;
        _progress = progress;
        _perkTree = new PerkTree(tables);
    }

    public static PlayerModel CreateNew(PlayerStatData baseStats, DataTableManager tables)
    {
        PlayerModel player = new(new PlayerStats(baseStats), PlayerLoadout.CreateNew(tables), PlayerProgress.CreateNew(), tables);
        player.ApplyPerks();

        return player;
    }

    // 스탯과 특성으로 얻은 장비는 저장하지 않으므로 찍은 특성에서 다시 만들고, 그다음 저장된 장비 선택을 되살린다.
    public static PlayerModel FromSave(PlayerSaveData save, PlayerStatData baseStats, DataTableManager tables)
    {
        PlayerModel player = new(new PlayerStats(baseStats), PlayerLoadout.FromSave(save.Loadout, tables), PlayerProgress.FromSave(save.Progress), tables);
        player.RemoveInvalidPerks();
        player.ApplyPerks();
        player._loadout.RestoreEquipped(save.Loadout);

        return player;
    }

    public PlayerSaveData ToSaveData()
    {
        return new PlayerSaveData(_loadout.ToSaveData(), _progress.ToSaveData());
    }

    // 처음 깼을 때만 포인트를 받고 그 보스의 스킬을 얻는다. 다시 깨면 false다. 보상 스킬이 비어 있으면 AddSkill이 넘긴다.
    public bool ClaimFirstClear(StageData stage)
    {
        if (false == _progress.TryMarkCleared(stage.SaveKey, stage.RewardPoints))
        {
            return false;
        }

        _loadout.AddSkill(stage.ClearSkillId);

        return true;
    }

    public PerkNodeState GetPerkState(string nodeId)
    {
        return _perkTree.GetState(_progress, nodeId);
    }

    public bool TryUnlockPerk(string nodeId)
    {
        if (PerkNodeState.Available != GetPerkState(nodeId))
        {
            return false;
        }

        _progress.AddPerk(nodeId);
        ApplyPerks();
        OnPerksChanged?.Invoke();

        return true;
    }

    // 저장은 부르는 쪽이 한다.
    public bool CollectPalette(string paletteId)
    {
        return _progress.TryAddPalette(paletteId);
    }

    // 저장은 특성이 바뀔 때 함께 된다.
    public bool TryRespec()
    {
        if (false == CanRespec)
        {
            return false;
        }

        _progress.UsePalette();
        ResetPerks();

        return true;
    }

    // 팔레트 없이 되돌린다. 시험 메뉴용이다.
    public void ResetPerks()
    {
        _progress.ClearPerks();
        ApplyPerks();
        OnPerksChanged?.Invoke();
    }

    private void ApplyPerks()
    {
        _stats.ClearPerkBonuses();
        _loadout.ClearPerkItems();
        _perkTree.Apply(_progress, _stats, _loadout);
        _loadout.RefreshEquipped();
    }

    // 시트를 고쳐 기록과 표가 어긋나면 표에 없는 노드는 버리고, 그래도 규칙에 맞지 않으면 모두 되돌려 포인트를 돌려준다.
    private void RemoveInvalidPerks()
    {
        for (int i = _progress.PerkIds.Count - 1; i >= 0; i--)
        {
            string perkId = _progress.PerkIds[i];

            if (false == _perkTree.Contains(perkId))
            {
                Debug.LogWarning($"플레이어 : 찍은 특성 {perkId}이(가) 표에 없어 뺍니다.");
                _progress.RemovePerk(perkId);
            }
        }

        if (false == _perkTree.IsValid(_progress))
        {
            Debug.LogWarning("플레이어 : 찍은 특성이 지금 표의 규칙과 맞지 않아 모두 되돌립니다.");
            _progress.ClearPerks();
        }
    }
}
