// 전투가 바뀌어도 이어지는 플레이어 상태. 스탯·특성·무기 같은 부분은 각자 클래스로 두고 여기서는 묶기만 한다.
// 파일은 모른다. 저장 파일을 읽고 쓰는 일은 PlayerSaveStorage가, 어느 쪽으로 만들지는 GameManager가 정한다.
public class PlayerModel
{
    private readonly PlayerStats _stats;

    public PlayerStats Stats { get { return _stats; } }

    private PlayerModel(PlayerStats stats)
    {
        _stats = stats;
    }

    // 저장 파일이 없을 때. 표의 기본값으로 시작한다.
    public static PlayerModel CreateNew(PlayerStatData baseStats)
    {
        return new PlayerModel(new PlayerStats(baseStats));
    }

    // 저장 파일이 있을 때. 스탯 값은 저장하지 않으므로, 표의 기본값에 저장된 선택(찍은 특성 등)을 얹어 다시 만든다.
    // 아직 저장하는 선택이 없어 새로 만들 때와 같다. 특성을 넣을 때 save에서 읽어 더한다.
    public static PlayerModel FromSave(PlayerSaveData save, PlayerStatData baseStats)
    {
        return new PlayerModel(new PlayerStats(baseStats));
    }

    public PlayerSaveData ToSaveData()
    {
        return new PlayerSaveData();
    }
}
