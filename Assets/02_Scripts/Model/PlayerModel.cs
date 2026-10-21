public class PlayerModel
{
    private readonly PlayerStats _stats;
    private readonly PlayerLoadout _loadout;

    public PlayerStats Stats { get { return _stats; } }
    public PlayerLoadout Loadout { get { return _loadout; } }

    private PlayerModel(PlayerStats stats, PlayerLoadout loadout)
    {
        _stats = stats;
        _loadout = loadout;
    }

    public static PlayerModel CreateNew(PlayerStatData baseStats, DataTableManager tables)
    {
        return new PlayerModel(new PlayerStats(baseStats), PlayerLoadout.CreateNew(tables));
    }

    // 스탯은 저장하지 않으므로 표의 기본값으로 다시 만들고, 장비는 저장된 선택을 표에 맞춰 되살린다.
    public static PlayerModel FromSave(PlayerSaveData save, PlayerStatData baseStats, DataTableManager tables)
    {
        return new PlayerModel(new PlayerStats(baseStats), PlayerLoadout.FromSave(save.Loadout, tables));
    }

    public PlayerSaveData ToSaveData()
    {
        return new PlayerSaveData(_loadout.ToSaveData());
    }
}
