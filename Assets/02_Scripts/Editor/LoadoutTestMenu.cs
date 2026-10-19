using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 장비 화면이 생기기 전까지 쓰는 시험용 메뉴. 플레이 중에만 쓸 수 있고, 바꾼 장비는 다음에 들어가는 전투부터 쓴다.
public static class LoadoutTestMenu
{
    private const string MENU_ROOT = "AutoDraw/테스트/";

    [MenuItem(MENU_ROOT + "모든 장비 얻기")]
    private static void GrantAllEquipment()
    {
        PlayerLoadout loadout = GameManager.Player.Loadout;
        DataTableManager tables = GameManager.Tables;

        foreach (WeaponData weapon in tables.Weapons.All)
        {
            loadout.AddWeapon(weapon.Id);
        }

        foreach (SkillData skill in tables.Skills.All)
        {
            loadout.AddSkill(skill.Id);
        }

        foreach (ConsumableData consumable in tables.Consumables.All)
        {
            loadout.AddConsumable(consumable.Id);
        }

        loadout.AddUtility(UtilityType.Dash);
        Debug.Log("장비 시험 : 모든 장비를 얻었습니다.");
    }

    [MenuItem(MENU_ROOT + "기본·화력 무기 다음 것으로")]
    private static void EquipNextWeapons()
    {
        PlayerLoadout loadout = GameManager.Player.Loadout;

        EquipNextWeapon(loadout, WeaponType.Basic, loadout.BasicWeaponId);
        EquipNextWeapon(loadout, WeaponType.Heavy, loadout.HeavyWeaponId);
        Debug.Log($"장비 시험 : 기본 무기 {loadout.BasicWeaponId}, 화력 무기 {loadout.HeavyWeaponId}");
    }

    [MenuItem(MENU_ROOT + "유틸리티 막기·대시 바꾸기")]
    private static void ToggleUtility()
    {
        PlayerLoadout loadout = GameManager.Player.Loadout;
        UtilityType next = UtilityType.Guard == loadout.Utility ? UtilityType.Dash : UtilityType.Guard;

        loadout.TryEquipUtility(next);
        Debug.Log($"장비 시험 : 유틸리티 {loadout.Utility}");
    }

    [MenuItem(MENU_ROOT + "모든 장비 얻기", true)]
    [MenuItem(MENU_ROOT + "기본·화력 무기 다음 것으로", true)]
    [MenuItem(MENU_ROOT + "유틸리티 막기·대시 바꾸기", true)]
    private static bool CanUse()
    {
        return Application.isPlaying && null != GameManager.Instance && null != GameManager.Player;
    }

    // 가진 무기 중 같은 종류를 표 순서로 돌아가며 끼운다.
    private static void EquipNextWeapon(PlayerLoadout loadout, WeaponType type, string currentId)
    {
        List<string> candidates = new();

        foreach (WeaponData weapon in GameManager.Tables.Weapons.All)
        {
            if (type == weapon.Type && loadout.HasWeapon(weapon.Id))
            {
                candidates.Add(weapon.Id);
            }
        }

        if (0 == candidates.Count)
        {
            return;
        }

        int next = (candidates.IndexOf(currentId) + 1) % candidates.Count;
        loadout.TryEquipWeapon(candidates[next]);
    }
}
