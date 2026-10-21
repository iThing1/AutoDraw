using UnityEngine;

// 무기 칸(기본·화력)에 끼운 무기의 최대 피해에 더한다. 무기를 바꿔 끼워도 칸에 남는다.
public class WeaponDamageAddEffect : PerkEffect
{
    private readonly WeaponType _type;

    public WeaponDamageAddEffect(WeaponType type)
    {
        _type = type;
    }

    public override void Apply(PlayerStats stats, PlayerLoadout loadout, float value)
    {
        stats.AddWeaponDamage(_type, Mathf.RoundToInt(value));
    }
}
