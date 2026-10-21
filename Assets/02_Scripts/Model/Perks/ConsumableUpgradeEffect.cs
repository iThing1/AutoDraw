// 소모품 하나의 세기·횟수·쿨타임 중 하나에 더한다.
public class ConsumableUpgradeEffect : PerkEffect
{
    private readonly string _consumableId;
    private readonly ConsumableStat _stat;

    public ConsumableUpgradeEffect(string consumableId, ConsumableStat stat)
    {
        _consumableId = consumableId;
        _stat = stat;
    }

    public override void Apply(PlayerStats stats, PlayerLoadout loadout, float value)
    {
        stats.AddConsumable(_consumableId, _stat, value);
    }
}
