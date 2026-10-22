using System;
using System.Collections.Generic;

// 유틸리티 칸(막기·대시). 표가 없어 종류 이름을 그대로 보여 준다.
public class UtilityLoadoutRule : LoadoutRule
{
    private readonly Dictionary<LoadoutEntry, UtilityType> _types = new();

    public UtilityLoadoutRule(PlayerLoadout loadout, AssetManager assets)
        : base("Utility", loadout, assets)
    {
        foreach (UtilityType utility in Enum.GetValues(typeof(UtilityType)))
        {
            LoadoutEntry entry = AddEntry(utility.ToString(), utility.ToString(), string.Empty, string.Empty, DEFAULT_ICON_COLOR, string.Empty);
            _types.Add(entry, utility);
        }
    }

    public override void Equip(LoadoutEntry entry)
    {
        Loadout.TryEquipUtility(_types[entry]);
    }

    protected override bool IsOwned(LoadoutEntry entry)
    {
        return Loadout.HasUtility(_types[entry]);
    }

    protected override bool IsEquipped(LoadoutEntry entry)
    {
        return _types[entry] == Loadout.Utility;
    }

    protected override bool IsReady(LoadoutEntry entry)
    {
        return Loadout.IsUtilityReady(_types[entry]);
    }
}
