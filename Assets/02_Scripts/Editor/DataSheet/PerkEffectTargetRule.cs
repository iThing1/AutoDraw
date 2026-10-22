using System;
using System.Collections.Generic;

public class PerkEffectTargetRule : IDataSheetRule
{
    private const string EFFECT_TABLE = "PerkEffectData";
    private const string WEAPON_TABLE = "WeaponData";
    private const string CONSUMABLE_TABLE = "ConsumableData";
    private const char FIELD_SEPARATOR = '.';

    public void Check(IReadOnlyDictionary<string, DataSheetTable> tables, List<string> errors)
    {
        if (false == tables.TryGetValue(EFFECT_TABLE, out DataSheetTable effects))
        {
            return;
        }

        tables.TryGetValue(WEAPON_TABLE, out DataSheetTable weapons);
        tables.TryGetValue(CONSUMABLE_TABLE, out DataSheetTable consumables);

        for (int row = 0; row < effects.RowCount; row++)
        {
            // 효과 종류 오류는 JSON으로 바꿀 때 잡힌다.
            if (false == TryGetEnum(effects.GetCell(row, "EffectType"), out PerkEffectType effectType))
            {
                continue;
            }

            string target = effects.GetCell(row, "Target");
            string error = CheckTarget(effectType, target, weapons, consumables);

            if (null != error)
            {
                errors.Add($"{EFFECT_TABLE} {effects.GetLineNumber(row)}줄 Target : {error}");
            }
        }
    }

    private string CheckTarget(PerkEffectType effectType, string target, DataSheetTable weapons, DataSheetTable consumables)
    {
        switch (effectType)
        {
            case PerkEffectType.StatAdd:
                return IsName<StatType>(target) ? null : $"'{target}'은(는) StatType에 없습니다.";

            case PerkEffectType.UnlockWeapon:
                return CheckId(weapons, WEAPON_TABLE, target);

            case PerkEffectType.UnlockConsumable:
                return CheckId(consumables, CONSUMABLE_TABLE, target);

            case PerkEffectType.ConsumableUpgrade:
                return CheckConsumableField(consumables, target);

            case PerkEffectType.UnlockUtility:
                return IsName<UtilityType>(target) ? null : $"'{target}'은(는) UtilityType에 없습니다.";

            case PerkEffectType.WeaponDamageAdd:
                return IsName<WeaponType>(target) ? null : $"'{target}'은(는) WeaponType에 없습니다.";

            // 새 효과 종류를 더하면 여기에 검사도 더한다.
            default:
                return null;
        }
    }

    private string CheckId(DataSheetTable table, string tableName, string id)
    {
        if (null == table || table.ContainsId(id))
        {
            return null;
        }

        return $"{tableName}에 {id}이(가) 없습니다.";
    }

    private string CheckConsumableField(DataSheetTable consumables, string target)
    {
        int separator = target.IndexOf(FIELD_SEPARATOR);

        if (separator < 0)
        {
            return $"'{target}'은(는) '소모품Id.필드' 꼴이 아닙니다.";
        }

        string field = target.Substring(separator + 1);

        if (false == IsName<ConsumableStat>(field))
        {
            return $"소모품 필드 {field}은(는) 올릴 수 없습니다. {string.Join("·", Enum.GetNames(typeof(ConsumableStat)))}만 됩니다.";
        }

        return CheckId(consumables, CONSUMABLE_TABLE, target.Substring(0, separator));
    }

    private static bool IsName<T>(string text) where T : struct, Enum
    {
        return 0 <= Array.IndexOf(Enum.GetNames(typeof(T)), text);
    }

    private static bool TryGetEnum<T>(string text, out T value) where T : struct, Enum
    {
        value = default;

        return IsName<T>(text) && Enum.TryParse(text, out value);
    }
}
