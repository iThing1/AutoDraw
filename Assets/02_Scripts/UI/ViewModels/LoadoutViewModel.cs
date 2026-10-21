using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

// 장비 화면에 보일 값. 칸마다 표의 장비를 모두 아이템 ViewModel로 만들어 두고, 장비가 바뀌면 상태만 다시 매긴다.
// 화면을 여는 쪽이 만들고, 닫을 때 UnBind로 장비 구독을 푼다.
public class LoadoutViewModel : ViewModelBase
{
    private const string LOCKED_NOTE = "Locked.";
    private const string NOT_READY_NOTE = "Coming soon.";
    private const string NO_CONSUMABLE_LABEL = "None";
    private const string CONSUMABLE_USAGE_FORMAT = "Uses: {0} | Cooldown: {1}s";

    private static readonly UtilityType[] UTILITIES = (UtilityType[])Enum.GetValues(typeof(UtilityType));
    // 공격 방식 색이 없는 장비(유틸리티·소모품 등)의 임시 아이콘 색.
    private static readonly Color DEFAULT_ICON_COLOR = new Color(0.6f, 0.6f, 0.65f, 1f);

    private readonly PlayerLoadout _loadout;
    private readonly DataTableManager _tables;
    private readonly AssetManager _assets;
    private readonly List<LoadoutSlotViewModel> _slots = new();
    private string _description = string.Empty;
    private VideoClip _preview;

    // 닫기를 눌렀다. 화면을 닫는 것은 연 쪽이 한다.
    public event Action OnCloseRequested;

    // 표 순서로 늘어놓아, 얻은 순서와 상관없이 늘 같은 자리에 보이게 한다.
    public IReadOnlyList<LoadoutSlotViewModel> Slots { get { return _slots; } }
    // 마지막으로 고른 장비의 설명.
    public string Description { get { return _description; } }
    // 마지막으로 고른 장비의 공격 예시 영상. 없으면 null이다.
    public VideoClip Preview { get { return _preview; } }

    public LoadoutViewModel(PlayerLoadout loadout, DataTableManager tables, AssetManager assets)
    {
        _loadout = loadout;
        _tables = tables;
        _assets = assets;

        _slots.Add(CreateWeaponSlot("Basic", WeaponType.Basic));
        _slots.Add(CreateWeaponSlot("Heavy", WeaponType.Heavy));
        _slots.Add(CreateSkillSlot());
        _slots.Add(CreateUtilitySlot());
        _slots.Add(CreateConsumableSlot());

        _loadout.OnChanged += HandleLoadoutChanged;
    }

    public override void UnBind()
    {
        _loadout.OnChanged -= HandleLoadoutChanged;

        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].UnBind();
        }
    }

    public void RequestClose()
    {
        OnCloseRequested?.Invoke();
    }

    private LoadoutSlotViewModel CreateWeaponSlot(string title, WeaponType type)
    {
        List<LoadoutItemViewModel> items = new();

        foreach (WeaponData weapon in _tables.Weapons.All)
        {
            if (type != weapon.Type)
            {
                continue;
            }

            string id = weapon.Id;
            items.Add(CreateItem(weapon.Name, weapon.Description, weapon.IconKey, GetStyleColor(weapon.AttackStyleKey), weapon.PreviewKey,
                () => GetState(_loadout.HasWeapon(id), id == GetEquippedWeaponId(type), true),
                () => _loadout.TryEquipWeapon(id)));
        }

        return new LoadoutSlotViewModel(title, items);
    }

    private LoadoutSlotViewModel CreateSkillSlot()
    {
        List<LoadoutItemViewModel> items = new();

        foreach (SkillData skill in _tables.Skills.All)
        {
            string id = skill.Id;
            items.Add(CreateItem(skill.Name, skill.Description, skill.IconKey, GetStyleColor(skill.AttackStyleKey), skill.PreviewKey,
                () => GetState(_loadout.HasSkill(id), id == _loadout.SkillId, _loadout.IsSkillReady(id)),
                () => _loadout.TryEquipSkill(id)));
        }

        return new LoadoutSlotViewModel("Skill", items);
    }

    private LoadoutSlotViewModel CreateUtilitySlot()
    {
        List<LoadoutItemViewModel> items = new();

        foreach (UtilityType utility in UTILITIES)
        {
            UtilityType type = utility;
            items.Add(CreateItem(utility.ToString(), string.Empty,
                () => GetState(_loadout.HasUtility(type), type == _loadout.Utility, _loadout.IsUtilityReady(type)),
                () => _loadout.TryEquipUtility(type)));
        }

        return new LoadoutSlotViewModel("Utility", items);
    }

    private LoadoutSlotViewModel CreateConsumableSlot()
    {
        List<LoadoutItemViewModel> items = new();

        items.Add(CreateItem(NO_CONSUMABLE_LABEL, string.Empty,
            () => GetState(true, string.IsNullOrEmpty(_loadout.ConsumableId), true),
            () => _loadout.TryEquipConsumable(null)));

        foreach (ConsumableData consumable in _tables.Consumables.All)
        {
            string id = consumable.Id;
            items.Add(CreateItem(consumable.Name, DescribeConsumable(consumable), consumable.IconKey, GetEffectColor(consumable.EffectKey), string.Empty,
                () => GetState(_loadout.HasConsumable(id), id == _loadout.ConsumableId, true),
                () => _loadout.TryEquipConsumable(id)));
        }

        return new LoadoutSlotViewModel("Item", items);
    }

    // 표에 없는 장비(유틸리티, 소모품 없음)는 아이콘·영상이 없고 기본색을 쓴다.
    private LoadoutItemViewModel CreateItem(string label, string description, Func<LoadoutItemState> getState, Action equip)
    {
        return CreateItem(label, description, string.Empty, DEFAULT_ICON_COLOR, string.Empty, getState, equip);
    }

    // 아이콘은 아직 없을 수 있어 조용히 찾고, 영상은 가져오기 때 검사한 키라 그대로 찾는다.
    private LoadoutItemViewModel CreateItem(string label, string description, string iconKey, Color iconColor, string previewKey,
        Func<LoadoutItemState> getState, Action equip)
    {
        _assets.TryGet(iconKey, out Sprite icon);

        VideoClip preview = _assets.Get<VideoClip>(previewKey);

        return new LoadoutItemViewModel(label, description, icon, iconColor, preview, getState, equip, HandleItemSelected);
    }

    // 공격 방식·효과 에셋의 색을 아이콘 대신 쓴다. 가져오기 때 검사한 키라 그대로 찾고, 키가 비었으면 기본색이다.
    private Color GetStyleColor(string attackStyleKey)
    {
        AttackStyle style = _assets.Get<AttackStyle>(attackStyleKey);

        return null != style ? style.IconColor : DEFAULT_ICON_COLOR;
    }

    private Color GetEffectColor(string effectKey)
    {
        ConsumableEffect effect = _assets.Get<ConsumableEffect>(effectKey);

        return null != effect ? effect.IconColor : DEFAULT_ICON_COLOR;
    }

    private string GetEquippedWeaponId(WeaponType type)
    {
        return WeaponType.Basic == type ? _loadout.BasicWeaponId : _loadout.HeavyWeaponId;
    }

    private void HandleLoadoutChanged()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].RefreshStates();
        }
    }

    private void HandleItemSelected(LoadoutItemViewModel item)
    {
        _description = Describe(item.Description, item.State);
        NotifyPropertyChanged(nameof(Description));

        _preview = item.Preview;
        NotifyPropertyChanged(nameof(Preview));
    }

    private static LoadoutItemState GetState(bool isOwned, bool isEquipped, bool isReady)
    {
        if (false == isOwned)
        {
            return LoadoutItemState.Locked;
        }

        if (false == isReady)
        {
            return LoadoutItemState.NotReady;
        }

        return isEquipped ? LoadoutItemState.Equipped : LoadoutItemState.Owned;
    }

    // 표 설명의 {0}에 효과 값을 넣고, 아래에 사용 횟수와 쿨타임을 붙인다.
    private static string DescribeConsumable(ConsumableData consumable)
    {
        string effect = string.Format(consumable.Description, consumable.Value);
        string usage = string.Format(CONSUMABLE_USAGE_FORMAT, consumable.Charges, consumable.Cooldown);

        return $"{effect}\n{usage}";
    }

    private static string Describe(string description, LoadoutItemState state)
    {
        string note = GetNote(state);

        if (string.IsNullOrEmpty(note))
        {
            return description;
        }

        return string.IsNullOrEmpty(description) ? note : $"{description}\n{note}";
    }

    private static string GetNote(LoadoutItemState state)
    {
        switch (state)
        {
            case LoadoutItemState.Locked:
                return LOCKED_NOTE;

            case LoadoutItemState.NotReady:
                return NOT_READY_NOTE;

            default:
                return string.Empty;
        }
    }
}
