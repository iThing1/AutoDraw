using System;
using System.Collections.Generic;
using UnityEngine;

// 표의 기본값에 찍은 특성의 보너스를 더한 플레이어 수치. 보너스는 특성이 바뀔 때마다 비우고 다시 더한다.
// 시간·비용·거리는 0 아래로, 체력은 1 아래로 내려가지 않는다.
public class PlayerStats : IUnitStats
{
    private static readonly int STAT_COUNT = Enum.GetValues(typeof(StatType)).Length;
    private static readonly int WEAPON_TYPE_COUNT = Enum.GetValues(typeof(WeaponType)).Length;

    private readonly PlayerStatData _base;
    private readonly float[] _statBonuses = new float[STAT_COUNT];
    // 무기 칸(종류)마다 최대 피해에 더한다. 공격 값은 StatType에 두지 않아 따로 둔다.
    private readonly int[] _weaponDamageBonuses = new int[WEAPON_TYPE_COUNT];
    private readonly Dictionary<(string, ConsumableStat), float> _consumableBonuses = new();

    public PlayerStats(PlayerStatData baseStats)
    {
        _base = baseStats;
    }

    public int MaxHp { get { return Mathf.Max(1, _base.MaxHp + Mathf.RoundToInt(_statBonuses[(int)StatType.MaxHp])); } }
    public float MoveSpeed { get { return AddBonus(_base.MoveSpeed, StatType.MoveSpeed); } }
    public float HitRadius { get { return AddBonus(_base.HitRadius, StatType.HitRadius); } }

    public float GuardDuration { get { return AddBonus(_base.GuardDuration, StatType.GuardDuration); } }
    public float GuardCost { get { return AddBonus(_base.GuardCost, StatType.GuardCost); } }
    public float GuardCooldown { get { return AddBonus(_base.GuardCooldown, StatType.GuardCooldown); } }
    public float GuardRadius { get { return AddBonus(_base.GuardRadius, StatType.GuardRadius); } }

    public float SkillGaugeMax { get { return AddBonus(_base.SkillGaugeMax, StatType.SkillGaugeMax); } }

    public float DashDistance { get { return AddBonus(_base.DashDistance, StatType.DashDistance); } }
    public float DashCooldown { get { return AddBonus(_base.DashCooldown, StatType.DashCooldown); } }

    public int GetWeaponDamageBonus(WeaponType type)
    {
        return _weaponDamageBonuses[(int)type];
    }

    public float GetConsumableValue(ConsumableData consumable)
    {
        return Mathf.Max(0f, consumable.Value + GetConsumableBonus(consumable.Id, ConsumableStat.Value));
    }

    public int GetConsumableCharges(ConsumableData consumable)
    {
        return Mathf.Max(0, consumable.Charges + Mathf.RoundToInt(GetConsumableBonus(consumable.Id, ConsumableStat.Charges)));
    }

    public float GetConsumableCooldown(ConsumableData consumable)
    {
        return Mathf.Max(0f, consumable.Cooldown + GetConsumableBonus(consumable.Id, ConsumableStat.Cooldown));
    }

    public void ClearPerkBonuses()
    {
        Array.Clear(_statBonuses, 0, _statBonuses.Length);
        Array.Clear(_weaponDamageBonuses, 0, _weaponDamageBonuses.Length);
        _consumableBonuses.Clear();
    }

    public void AddStat(StatType type, float value)
    {
        _statBonuses[(int)type] += value;
    }

    public void AddWeaponDamage(WeaponType type, int value)
    {
        _weaponDamageBonuses[(int)type] += value;
    }

    public void AddConsumable(string consumableId, ConsumableStat stat, float value)
    {
        _consumableBonuses.TryGetValue((consumableId, stat), out float current);
        _consumableBonuses[(consumableId, stat)] = current + value;
    }

    private float AddBonus(float baseValue, StatType type)
    {
        return Mathf.Max(0f, baseValue + _statBonuses[(int)type]);
    }

    private float GetConsumableBonus(string consumableId, ConsumableStat stat)
    {
        return _consumableBonuses.TryGetValue((consumableId, stat), out float bonus) ? bonus : 0f;
    }
}
