using System;
using System.Collections.Generic;
using UnityEngine;

// 기본 무기 칸에는 Basic, 화력 무기 칸에는 Heavy 무기만 끼운다. 처음 장비(IsStarting)와 막기는 늘 가진 것으로 본다.
// 특성으로 얻은 장비는 저장하지 않고, 찍은 특성에서 다시 만든다. 그래서 특성을 초기화하면 함께 빠진다.
public class PlayerLoadout
{
    public event Action OnChanged;

    private readonly DataTableManager _tables;
    private readonly List<string> _weaponIds = new();
    private readonly List<string> _skillIds = new();
    private readonly List<string> _consumableIds = new();
    private readonly List<UtilityType> _utilities = new();
    private readonly List<string> _perkWeaponIds = new();
    private readonly List<string> _perkConsumableIds = new();
    private readonly List<UtilityType> _perkUtilities = new();

    private string _basicWeaponId;
    private string _heavyWeaponId;
    private string _skillId;
    private UtilityType _utility = UtilityType.Guard;
    // 비어 있으면 소모품 없이 들어간다.
    private string _consumableId;

    // 저장하는 가진 것이다. 특성으로 얻은 것은 들어 있지 않다.
    public IReadOnlyList<string> WeaponIds { get { return _weaponIds; } }
    public IReadOnlyList<string> SkillIds { get { return _skillIds; } }
    public IReadOnlyList<string> ConsumableIds { get { return _consumableIds; } }
    public IReadOnlyList<UtilityType> Utilities { get { return _utilities; } }

    public string BasicWeaponId { get { return _basicWeaponId; } }
    public string HeavyWeaponId { get { return _heavyWeaponId; } }
    public string SkillId { get { return _skillId; } }
    public UtilityType Utility { get { return _utility; } }
    public string ConsumableId { get { return _consumableId; } }

    public bool HasWeapon(string weaponId)
    {
        return _weaponIds.Contains(weaponId) || _perkWeaponIds.Contains(weaponId);
    }

    public bool HasSkill(string skillId)
    {
        return _skillIds.Contains(skillId);
    }

    public bool HasConsumable(string consumableId)
    {
        return _consumableIds.Contains(consumableId) || _perkConsumableIds.Contains(consumableId);
    }

    public bool HasUtility(UtilityType utility)
    {
        return _utilities.Contains(utility) || _perkUtilities.Contains(utility);
    }

    // 공격 방식이 아직 없는 스킬은 끼우면 전투에서 쓸 수 없다.
    public bool IsSkillReady(string skillId)
    {
        return _tables.Skills.TryGet(skillId, out SkillData skill) && false == string.IsNullOrEmpty(skill.AttackStyleKey);
    }

    // 대시는 아직 전투에 없어 끼우면 막기만 빠진다. 대시를 만들면 지운다.
    public bool IsUtilityReady(UtilityType utility)
    {
        return UtilityType.Dash != utility;
    }

    private PlayerLoadout(DataTableManager tables)
    {
        _tables = tables;
        AddStartingItems();
    }

    public static PlayerLoadout CreateNew(DataTableManager tables)
    {
        PlayerLoadout loadout = new(tables);
        loadout.EquipDefaults();

        return loadout;
    }

    // 표에서 사라진 Id는 버리고, 비게 된 칸은 처음 장비로 채운다. 시트에서 행을 지워도 저장 파일 때문에 깨지지 않게 한다.
    // 끼운 것은 특성으로 얻은 장비가 들어온 뒤에 RestoreEquipped로 따로 되살린다. 그 전에는 처음 장비를 끼워 둔다.
    public static PlayerLoadout FromSave(LoadoutSaveData save, DataTableManager tables)
    {
        PlayerLoadout loadout = new(tables);

        if (null != save)
        {
            loadout.RestoreOwned(save);
        }

        loadout.EquipDefaults();

        return loadout;
    }

    // 지금 가진 것으로 끼울 수 없는 칸은 그대로 둔다.
    public void RestoreEquipped(LoadoutSaveData save)
    {
        if (null == save)
        {
            return;
        }

        TryEquipWeapon(save.BasicWeaponId);
        TryEquipWeapon(save.HeavyWeaponId);
        TryEquipSkill(save.SkillId);
        TryEquipUtility(save.Utility);
        TryEquipConsumable(save.ConsumableId);
    }

    public LoadoutSaveData ToSaveData()
    {
        return new LoadoutSaveData(this);
    }

    public bool AddWeapon(string weaponId)
    {
        return AddOwned(_weaponIds, weaponId, _tables.Weapons.TryGet(weaponId, out _), "무기");
    }

    public bool AddSkill(string skillId)
    {
        return AddOwned(_skillIds, skillId, _tables.Skills.TryGet(skillId, out _), "스킬");
    }

    public bool AddConsumable(string consumableId)
    {
        return AddOwned(_consumableIds, consumableId, _tables.Consumables.TryGet(consumableId, out _), "소모품");
    }

    public bool AddUtility(UtilityType utility)
    {
        if (_utilities.Contains(utility))
        {
            return false;
        }

        _utilities.Add(utility);
        OnChanged?.Invoke();

        return true;
    }

    // 무기의 Type이 들어갈 칸을 정한다.
    public bool TryEquipWeapon(string weaponId)
    {
        if (false == HasWeapon(weaponId) || false == _tables.Weapons.TryGet(weaponId, out WeaponData weapon))
        {
            return false;
        }

        switch (weapon.Type)
        {
            case WeaponType.Basic:
                _basicWeaponId = weaponId;
                break;

            case WeaponType.Heavy:
                _heavyWeaponId = weaponId;
                break;

            default:
                return false;
        }

        OnChanged?.Invoke();

        return true;
    }

    public bool TryEquipSkill(string skillId)
    {
        if (false == _skillIds.Contains(skillId) || false == IsSkillReady(skillId))
        {
            return false;
        }

        _skillId = skillId;
        OnChanged?.Invoke();

        return true;
    }

    public bool TryEquipUtility(UtilityType utility)
    {
        if (false == HasUtility(utility) || false == IsUtilityReady(utility))
        {
            return false;
        }

        _utility = utility;
        OnChanged?.Invoke();

        return true;
    }

    // 빈 Id는 소모품을 빼는 것이다.
    public bool TryEquipConsumable(string consumableId)
    {
        if (false == string.IsNullOrEmpty(consumableId) && false == HasConsumable(consumableId))
        {
            return false;
        }

        _consumableId = string.IsNullOrEmpty(consumableId) ? null : consumableId;
        OnChanged?.Invoke();

        return true;
    }

    // 특성을 다시 계산할 때 처음에 부른다. 끼운 칸은 다 넣은 뒤 RefreshEquipped에서 맞춘다.
    public void ClearPerkItems()
    {
        _perkWeaponIds.Clear();
        _perkConsumableIds.Clear();
        _perkUtilities.Clear();
    }

    public void AddPerkWeapon(string weaponId)
    {
        TryAddId(_perkWeaponIds, weaponId, _tables.Weapons.TryGet(weaponId, out _), "무기");
    }

    public void AddPerkConsumable(string consumableId)
    {
        TryAddId(_perkConsumableIds, consumableId, _tables.Consumables.TryGet(consumableId, out _), "소모품");
    }

    public void AddPerkUtility(UtilityType utility)
    {
        if (false == _perkUtilities.Contains(utility))
        {
            _perkUtilities.Add(utility);
        }
    }

    // 특성을 다시 계산한 뒤 부른다. 특성을 되돌려 더는 갖지 않은 장비를 끼고 있으면 빼고 처음 장비로 채운다.
    public void RefreshEquipped()
    {
        if (false == HasWeapon(_basicWeaponId))
        {
            _basicWeaponId = null;
        }

        if (false == HasWeapon(_heavyWeaponId))
        {
            _heavyWeaponId = null;
        }

        if (false == HasUtility(_utility))
        {
            _utility = UtilityType.Guard;
        }

        if (false == string.IsNullOrEmpty(_consumableId) && false == HasConsumable(_consumableId))
        {
            _consumableId = null;
        }

        EquipDefaults();
        OnChanged?.Invoke();
    }

    private void AddStartingItems()
    {
        foreach (WeaponData weapon in _tables.Weapons.All)
        {
            if (weapon.IsStarting)
            {
                _weaponIds.Add(weapon.Id);
            }
        }

        foreach (SkillData skill in _tables.Skills.All)
        {
            if (skill.IsStarting)
            {
                _skillIds.Add(skill.Id);
            }
        }

        _utilities.Add(UtilityType.Guard);
    }

    private void RestoreOwned(LoadoutSaveData save)
    {
        foreach (string weaponId in save.WeaponIds)
        {
            AddWeapon(weaponId);
        }

        foreach (string skillId in save.SkillIds)
        {
            AddSkill(skillId);
        }

        foreach (string consumableId in save.ConsumableIds)
        {
            AddConsumable(consumableId);
        }

        foreach (UtilityType utility in save.Utilities)
        {
            AddUtility(utility);
        }
    }

    // 빈 칸만 표 순서로 처음 가진 것을 끼운다.
    private void EquipDefaults()
    {
        if (string.IsNullOrEmpty(_basicWeaponId))
        {
            _basicWeaponId = FindOwnedWeapon(WeaponType.Basic);
        }

        if (string.IsNullOrEmpty(_heavyWeaponId))
        {
            _heavyWeaponId = FindOwnedWeapon(WeaponType.Heavy);
        }

        if (string.IsNullOrEmpty(_skillId) && 0 < _skillIds.Count)
        {
            _skillId = _skillIds[0];
        }

        if (string.IsNullOrEmpty(_basicWeaponId) || string.IsNullOrEmpty(_heavyWeaponId) || string.IsNullOrEmpty(_skillId))
        {
            Debug.LogError("플레이어 장비 : 처음부터 가진(IsStarting) 기본 무기·화력 무기·스킬이 표에 모두 있어야 합니다.");
        }
    }

    private string FindOwnedWeapon(WeaponType type)
    {
        foreach (WeaponData weapon in _tables.Weapons.All)
        {
            if (type == weapon.Type && HasWeapon(weapon.Id))
            {
                return weapon.Id;
            }
        }

        return null;
    }

    private bool AddOwned(List<string> owned, string id, bool existsInTable, string kindName)
    {
        if (false == TryAddId(owned, id, existsInTable, kindName))
        {
            return false;
        }

        OnChanged?.Invoke();

        return true;
    }

    private static bool TryAddId(List<string> ids, string id, bool existsInTable, string kindName)
    {
        if (string.IsNullOrEmpty(id) || ids.Contains(id))
        {
            return false;
        }

        if (false == existsInTable)
        {
            Debug.LogWarning($"플레이어 장비 : {kindName} {id}이(가) 표에 없어 뺍니다.");
            return false;
        }

        ids.Add(id);

        return true;
    }
}
