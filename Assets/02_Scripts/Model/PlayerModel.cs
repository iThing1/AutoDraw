public class PlayerModel
{
    private readonly PlayerStats _stats;
    private readonly PlayerLoadout _loadout;
    private readonly PlayerProgress _progress;

    public PlayerStats Stats { get { return _stats; } }
    public PlayerLoadout Loadout { get { return _loadout; } }
    public PlayerProgress Progress { get { return _progress; } }

    private PlayerModel(PlayerStats stats, PlayerLoadout loadout, PlayerProgress progress)
    {
        _stats = stats;
        _loadout = loadout;
        _progress = progress;
    }

    public static PlayerModel CreateNew(PlayerStatData baseStats, DataTableManager tables)
    {
        return new PlayerModel(new PlayerStats(baseStats), PlayerLoadout.CreateNew(tables), PlayerProgress.CreateNew());
    }

    // 스탯은 저장하지 않으므로 표의 기본값으로 다시 만들고, 장비는 저장된 선택을 표에 맞춰 되살린다.
    public static PlayerModel FromSave(PlayerSaveData save, PlayerStatData baseStats, DataTableManager tables)
    {
        return new PlayerModel(new PlayerStats(baseStats), PlayerLoadout.FromSave(save.Loadout, tables), PlayerProgress.FromSave(save.Progress));
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
}
