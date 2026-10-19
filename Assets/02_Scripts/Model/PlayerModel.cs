public class PlayerModel
{
    private readonly PlayerStats _stats;

    public PlayerStats Stats { get { return _stats; } }

    private PlayerModel(PlayerStats stats)
    {
        _stats = stats;
    }

    public static PlayerModel CreateNew(PlayerStatData baseStats)
    {
        return new PlayerModel(new PlayerStats(baseStats));
    }

    // 스탯은 저장하지 않으므로 표의 기본값에 저장된 선택을 얹어 다시 만든다. 아직 저장하는 선택은 없다.
    public static PlayerModel FromSave(PlayerSaveData save, PlayerStatData baseStats)
    {
        return new PlayerModel(new PlayerStats(baseStats));
    }

    public PlayerSaveData ToSaveData()
    {
        return new PlayerSaveData();
    }
}
