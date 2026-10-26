using System;
using System.Collections.Generic;

// 유틸리티 칸(막기·대시). 표가 없어 종류 이름을 그대로 보여 주고, 아이콘은 종류 이름 키로 찾는다.
// 설명도 표가 없어 여기서 정하고, 지속·비용·쿨타임은 특성 보너스를 더한 지금 스탯으로 채운다.
public class UtilityLoadoutRule : LoadoutRule
{
    private const string GUARD_FORMAT = "Raises a shield for {0:0.##}s. Any attack that touches it is blocked, and a block resets the cooldown.\n"
        + "Gauge cost: {1:0.#} | Cooldown: {2:0.##}s";
    private const string DASH_FORMAT = "Slides {0:0.#} units in the direction you move. Attacks pass through you while dashing.\nCooldown: {1:0.##}s";

    private readonly Dictionary<LoadoutEntry, UtilityType> _types = new();

    public UtilityLoadoutRule(PlayerLoadout loadout, PlayerStats stats, AssetManager assets)
        : base("Utility", loadout, assets)
    {
        foreach (UtilityType utility in Enum.GetValues(typeof(UtilityType)))
        {
            LoadoutEntry entry = AddEntry(utility.ToString(), utility.ToString(), Describe(utility, stats), utility.GetIconKey(), DEFAULT_ICON_COLOR,
                string.Empty);
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

    private static string Describe(UtilityType utility, PlayerStats stats)
    {
        switch (utility)
        {
            case UtilityType.Guard:
                return string.Format(GUARD_FORMAT, stats.GuardDuration, stats.GuardCost, stats.GuardCooldown);

            case UtilityType.Dash:
                return string.Format(DASH_FORMAT, stats.DashDistance, stats.DashCooldown);

            default:
                return string.Empty;
        }
    }
}
