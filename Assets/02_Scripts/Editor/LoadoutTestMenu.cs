using UnityEditor;
using UnityEngine;

// 장비를 얻는 길(특성, 스테이지 클리어 보상)이 생기기 전까지 쓰는 시험용 메뉴. 장비 교체는 갤러리 보관대에서 한다.
public static class LoadoutTestMenu
{
    private const string MENU_ROOT = "AutoDraw/테스트/";
    private const int TEST_POTION_CHARGES = 10;

    // 플레이 중에만 쓸 수 있다. 얻은 장비는 보관대 화면을 닫을 때 함께 저장된다.
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

    // 회복 효과 소모품을 모두 얻고 횟수를 늘린다. 회복약을 끼우지 않았으면 첫 회복약을 끼운다.
    // 횟수는 전투를 시작할 때 정해지므로 다음 전투부터 늘어난다. 특성 보너스에 얹으므로 저장되지 않고, 특성을 바꾸거나 플레이를 다시 시작하면 사라진다.
    [MenuItem(MENU_ROOT + "회복약 횟수 +10")]
    private static void AddPotionCharges()
    {
        PlayerModel player = GameManager.Player;
        PlayerLoadout loadout = player.Loadout;
        string firstPotionId = null;
        bool isPotionEquipped = false;

        foreach (ConsumableData consumable in GameManager.Tables.Consumables.All)
        {
            ConsumableEffect effect = GameManager.Assets.Get<ConsumableEffect>(consumable.EffectKey);

            if (false == (effect is HealConsumableEffect))
            {
                continue;
            }

            loadout.AddConsumable(consumable.Id);
            player.Stats.AddConsumable(consumable.Id, ConsumableStat.Charges, TEST_POTION_CHARGES);

            firstPotionId ??= consumable.Id;
            isPotionEquipped |= consumable.Id == loadout.ConsumableId;
        }

        if (null == firstPotionId)
        {
            Debug.LogWarning("장비 시험 : 회복 효과 소모품이 표에 없습니다.");
            return;
        }

        if (false == isPotionEquipped)
        {
            loadout.TryEquipConsumable(firstPotionId);
        }

        Debug.Log($"장비 시험 : 회복약 횟수를 {TEST_POTION_CHARGES} 늘렸습니다. 다음 전투부터 적용됩니다. 끼운 소모품 {loadout.ConsumableId}");
    }

    [MenuItem(MENU_ROOT + "모든 장비 얻기", true)]
    [MenuItem(MENU_ROOT + "회복약 횟수 +10", true)]
    private static bool CanGrantAllEquipment()
    {
        return Application.isPlaying && null != GameManager.Instance && null != GameManager.Player;
    }

    // 플레이 중에 지워도 지금 장비는 그대로이고, 다음에 플레이를 시작할 때 처음 장비로 시작한다.
    [MenuItem(MENU_ROOT + "저장 지우기")]
    private static void DeleteSave()
    {
        PlayerSaveStorage.Delete();
        Debug.Log("장비 시험 : 저장 파일을 지웠습니다.");
    }
}
