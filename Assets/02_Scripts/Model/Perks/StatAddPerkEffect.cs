// 플레이어 몸 스탯(체력·이동·막기·대시 등)에 더한다. 빼려면 음수를 쓴다.
public class StatAddPerkEffect : PerkEffect
{
    private readonly StatType _stat;

    public StatAddPerkEffect(StatType stat)
    {
        _stat = stat;
    }

    public override void Apply(PlayerStats stats, PlayerLoadout loadout, float value)
    {
        stats.AddStat(_stat, value);
    }
}
