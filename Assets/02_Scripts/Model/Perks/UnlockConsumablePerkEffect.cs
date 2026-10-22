// 소모품을 장비 화면에서 끼울 수 있게 한다.
public class UnlockConsumablePerkEffect : PerkEffect
{
    private readonly string _consumableId;

    public UnlockConsumablePerkEffect(string consumableId)
    {
        _consumableId = consumableId;
    }

    public override void Apply(PlayerStats stats, PlayerLoadout loadout, float value)
    {
        loadout.AddPerkConsumable(_consumableId);
    }
}
