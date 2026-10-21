using System;
using UnityEngine;

// 특성 노드 하나가 주는 효과. 세기는 노드마다 달라 Apply에서 받는다(PerkNodeData.EffectValues).
// 효과 표의 Target은 만들 때 한 번만 읽는다. 새 효과 종류는 하위 클래스 하나와 Create의 갈래 하나를 더한다.
public abstract class PerkEffect
{
    private const char FIELD_SEPARATOR = '.';

    public abstract void Apply(PlayerStats stats, PlayerLoadout loadout, float value);

    // Target을 읽지 못하면 오류를 남기고 null이다. 가져오기 도구가 미리 검사하므로 JSON을 손으로 고쳤을 때만 생긴다.
    public static PerkEffect Create(PerkEffectData data)
    {
        switch (data.EffectType)
        {
            case PerkEffectType.StatAdd:
                return TryParseTarget(data, data.Target, out StatType stat) ? new StatAddEffect(stat) : null;

            case PerkEffectType.WeaponDamageAdd:
                return TryParseTarget(data, data.Target, out WeaponType weaponType) ? new WeaponDamageAddEffect(weaponType) : null;

            case PerkEffectType.UnlockWeapon:
                return new UnlockWeaponEffect(data.Target);

            case PerkEffectType.UnlockConsumable:
                return new UnlockConsumableEffect(data.Target);

            case PerkEffectType.ConsumableUpgrade:
                return CreateConsumableUpgrade(data);

            case PerkEffectType.UnlockAbility:
                return TryParseTarget(data, data.Target, out UtilityType utility) ? new UnlockAbilityEffect(utility) : null;

            default:
                Debug.LogError($"특성 효과 : {data.Id}의 효과 종류 {data.EffectType}을(를) 만들 수 없습니다.");
                return null;
        }
    }

    // Target은 "소모품Id.ConsumableStat" 꼴이다.
    private static PerkEffect CreateConsumableUpgrade(PerkEffectData data)
    {
        int separator = data.Target.IndexOf(FIELD_SEPARATOR);

        if (separator < 0)
        {
            Debug.LogError($"특성 효과 : {data.Id}의 Target {data.Target}이(가) '소모품Id.필드' 꼴이 아닙니다.");
            return null;
        }

        string consumableId = data.Target.Substring(0, separator);

        if (false == TryParseTarget(data, data.Target.Substring(separator + 1), out ConsumableStat stat))
        {
            return null;
        }

        return new ConsumableUpgradeEffect(consumableId, stat);
    }

    // Enum.TryParse는 숫자 문자열도 받아들이므로, 이름이 있는지 먼저 본다.
    private static bool TryParseTarget<T>(PerkEffectData data, string text, out T value) where T : struct, Enum
    {
        if (Enum.IsDefined(typeof(T), text))
        {
            value = (T)Enum.Parse(typeof(T), text);
            return true;
        }

        value = default;
        Debug.LogError($"특성 효과 : {data.Id}의 Target {text}은(는) {typeof(T).Name}에 없습니다.");
        return false;
    }
}
