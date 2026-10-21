// 기본 무기 칸 또는 화력 무기 칸. 표의 무기 중 칸과 같은 종류만 늘어놓는다.
public class WeaponLoadoutSlot : LoadoutSlot
{
    private readonly WeaponType _type;

    public WeaponLoadoutSlot(string title, WeaponType type, PlayerLoadout loadout, DataTableManager tables, AssetManager assets)
        : base(title, loadout, assets)
    {
        _type = type;

        foreach (WeaponData weapon in tables.Weapons.All)
        {
            if (type == weapon.Type)
            {
                AddEntry(weapon.Id, weapon.Name, weapon.Description, weapon.IconKey, GetStyleColor(weapon.AttackStyleKey), weapon.PreviewKey);
            }
        }
    }

    public override void Equip(LoadoutEntry entry)
    {
        Loadout.TryEquipWeapon(entry.Id);
    }

    protected override bool IsOwned(LoadoutEntry entry)
    {
        return Loadout.HasWeapon(entry.Id);
    }

    protected override bool IsEquipped(LoadoutEntry entry)
    {
        string equippedId = WeaponType.Basic == _type ? Loadout.BasicWeaponId : Loadout.HeavyWeaponId;

        return entry.Id == equippedId;
    }
}
