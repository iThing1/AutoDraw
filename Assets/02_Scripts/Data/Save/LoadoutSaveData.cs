using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LoadoutSaveData
{
    [SerializeField] private string[] _weaponIds;
    [SerializeField] private string[] _skillIds;
    [SerializeField] private string[] _consumableIds;
    [SerializeField] private UtilityType[] _utilities;

    [SerializeField] private string _basicWeaponId;
    [SerializeField] private string _heavyWeaponId;
    [SerializeField] private string _skillId;
    [SerializeField] private UtilityType _utility;
    [SerializeField] private string _consumableId;

    public IReadOnlyList<string> WeaponIds { get { return _weaponIds ?? Array.Empty<string>(); } }
    public IReadOnlyList<string> SkillIds { get { return _skillIds ?? Array.Empty<string>(); } }
    public IReadOnlyList<string> ConsumableIds { get { return _consumableIds ?? Array.Empty<string>(); } }
    public IReadOnlyList<UtilityType> Utilities { get { return _utilities ?? Array.Empty<UtilityType>(); } }

    public string BasicWeaponId { get { return _basicWeaponId; } }
    public string HeavyWeaponId { get { return _heavyWeaponId; } }
    public string SkillId { get { return _skillId; } }
    public UtilityType Utility { get { return _utility; } }
    public string ConsumableId { get { return _consumableId; } }

    // JsonUtility가 불러올 때 쓴다.
    public LoadoutSaveData()
    {
    }

    public LoadoutSaveData(PlayerLoadout loadout)
    {
        _weaponIds = ToArray(loadout.WeaponIds);
        _skillIds = ToArray(loadout.SkillIds);
        _consumableIds = ToArray(loadout.ConsumableIds);
        _utilities = ToArray(loadout.Utilities);

        _basicWeaponId = loadout.BasicWeaponId;
        _heavyWeaponId = loadout.HeavyWeaponId;
        _skillId = loadout.SkillId;
        _utility = loadout.Utility;
        _consumableId = loadout.ConsumableId;
    }

    private static T[] ToArray<T>(IReadOnlyList<T> list)
    {
        T[] array = new T[list.Count];

        for (int i = 0; i < list.Count; i++)
        {
            array[i] = list[i];
        }

        return array;
    }
}
