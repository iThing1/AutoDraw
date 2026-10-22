using UnityEngine;

// 소모품 칸. 맨 앞의 None을 고르면 소모품 없이 들어간다. 설명에는 효과 값과 사용 횟수·쿨타임을 함께 쓴다.
public class ConsumableLoadoutRule : LoadoutRule
{
    private const string NONE_LABEL = "None";
    private const string USAGE_FORMAT = "Uses: {0} | Cooldown: {1}s";

    public ConsumableLoadoutRule(PlayerLoadout loadout, PlayerStats stats, DataTableManager tables, AssetManager assets)
        : base("Item", loadout, assets)
    {
        AddEntry(string.Empty, NONE_LABEL, string.Empty, string.Empty, DEFAULT_ICON_COLOR, string.Empty);

        foreach (ConsumableData consumable in tables.Consumables.All)
        {
            AddEntry(consumable.Id, consumable.Name, Describe(consumable, stats), consumable.IconKey, GetEffectColor(consumable.EffectKey), string.Empty);
        }
    }

    public override void Equip(LoadoutEntry entry)
    {
        Loadout.TryEquipConsumable(entry.Id);
    }

    protected override bool IsOwned(LoadoutEntry entry)
    {
        return IsNone(entry) || Loadout.HasConsumable(entry.Id);
    }

    protected override bool IsEquipped(LoadoutEntry entry)
    {
        return IsNone(entry) ? string.IsNullOrEmpty(Loadout.ConsumableId) : entry.Id == Loadout.ConsumableId;
    }

    private static bool IsNone(LoadoutEntry entry)
    {
        return string.IsNullOrEmpty(entry.Id);
    }

    // 표 설명의 {0}에 효과 값을 넣고, 아래에 사용 횟수와 쿨타임을 붙인다. 모두 특성 보너스를 더한 값이다.
    private static string Describe(ConsumableData consumable, PlayerStats stats)
    {
        string effect = string.Format(consumable.Description, stats.GetConsumableValue(consumable));
        string usage = string.Format(USAGE_FORMAT, stats.GetConsumableCharges(consumable), stats.GetConsumableCooldown(consumable));

        return $"{effect}\n{usage}";
    }

    private Color GetEffectColor(string effectKey)
    {
        ConsumableEffect effect = Assets.Get<ConsumableEffect>(effectKey);

        return null != effect ? effect.IconColor : DEFAULT_ICON_COLOR;
    }
}
