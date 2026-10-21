// 무기를 장비 화면에서 끼울 수 있게 한다.
public class UnlockWeaponEffect : PerkEffect
{
    private readonly string _weaponId;

    public UnlockWeaponEffect(string weaponId)
    {
        _weaponId = weaponId;
    }

    public override void Apply(PlayerStats stats, PlayerLoadout loadout, float value)
    {
        loadout.AddPerkWeapon(_weaponId);
    }
}
