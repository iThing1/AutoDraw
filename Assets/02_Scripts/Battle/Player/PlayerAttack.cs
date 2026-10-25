using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 무기 칸 키로 공격을 고르고, 게이지가 가득 차면 스킬 키로 특수 스킬을 발동한다.
[RequireComponent(typeof(BattleUnit), typeof(SkillGauge))]
public class PlayerAttack : MonoBehaviour
{
    private const int BASIC_SLOT = 0;
    private const int HEAVY_SLOT = 1;
    private const int WEAPON_SLOT_COUNT = 2;

    // 무기 칸과 같은 순서로 넣는다(0 = 기본 무기, 1 = 화력 무기).
    [SerializeField] private InputActionReference[] _weaponActions;
    [SerializeField] private InputActionReference _skillAction;

    public event Action<int> OnWeaponSelected;
    public event Action OnSkillFired;

    private readonly AttackStyle[] _slotStyles = new AttackStyle[WEAPON_SLOT_COUNT];
    private readonly WeaponModifier[] _slotModifiers = new WeaponModifier[WEAPON_SLOT_COUNT];
    private readonly Sprite[] _slotIcons = new Sprite[WEAPON_SLOT_COUNT];

    private BattleUnit _unit;
    private SkillGauge _skillGauge;
    private AttackStyle _skillStyle;
    private Sprite _skillIcon;
    private int _selectedIndex = -1;

    public SkillGauge SkillGauge { get { return _skillGauge; } }
    public int SelectedIndex { get { return _selectedIndex; } }
    public string SkillKeyLabel { get { return _skillAction.GetKeyLabel(); } }
    // 끼운 스킬의 공격 방식이 아직 없으면 null이다.
    public AttackStyle SkillStyle { get { return _skillStyle; } }
    public Sprite SkillIcon { get { return _skillIcon; } }

    // 무기 칸과 입력 중 적은 쪽까지만 쓴다. 키가 없는 칸은 고를 수 없다.
    public int SlotCount { get { return null != _weaponActions ? Mathf.Min(WEAPON_SLOT_COUNT, _weaponActions.Length) : 0; } }

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();
        _skillGauge = GetComponent<SkillGauge>();

        // 기본값(0, 0)이면 간격이 0이 되므로 표에 없는 칸도 보정 없음으로 둔다.
        for (int i = 0; i < WEAPON_SLOT_COUNT; i++)
        {
            _slotModifiers[i] = WeaponModifier.NONE;
        }
    }

    public void Setup(PlayerLoadout loadout, PlayerStats stats, DataTableManager tables, AssetManager assets)
    {
        _skillGauge.Init(stats.SkillGaugeMax);

        ReadWeapon(BASIC_SLOT, loadout.BasicWeaponId, stats, tables, assets);
        ReadWeapon(HEAVY_SLOT, loadout.HeavyWeaponId, stats, tables, assets);

        if (tables.Skills.TryGet(loadout.SkillId, out SkillData skill))
        {
            _skillStyle = assets.Get<AttackStyle>(skill.AttackStyleKey);
            _skillIcon = assets.Get<Sprite>(skill.IconKey);
        }
    }

    private void OnEnable()
    {
        if (null != _weaponActions)
        {
            for (int i = 0; i < _weaponActions.Length; i++)
            {
                _weaponActions[i].Enable();
            }
        }

        _skillAction.Enable();
    }

    private void Start()
    {
        if (0 < SlotCount)
        {
            SelectWeapon(BASIC_SLOT);
        }
    }

    private void Update()
    {
        int slotCount = SlotCount;

        for (int i = 0; i < slotCount; i++)
        {
            if (_weaponActions[i].WasPressedThisFrame())
            {
                SelectWeapon(i);
                return;
            }
        }

        if (_skillAction.WasPressedThisFrame())
        {
            TryUseSkill();
        }
    }

    public AttackStyle GetSlotStyle(int index)
    {
        return 0 <= index && index < WEAPON_SLOT_COUNT ? _slotStyles[index] : null;
    }

    public Sprite GetSlotIcon(int index)
    {
        return 0 <= index && index < WEAPON_SLOT_COUNT ? _slotIcons[index] : null;
    }

    public string GetWeaponKeyLabel(int index)
    {
        if (null == _weaponActions || index >= _weaponActions.Length)
        {
            return string.Empty;
        }

        return _weaponActions[index].GetKeyLabel();
    }

    private void ReadWeapon(int slot, string weaponId, PlayerStats stats, DataTableManager tables, AssetManager assets)
    {
        if (false == tables.Weapons.TryGet(weaponId, out WeaponData weapon))
        {
            Debug.LogError($"플레이어 공격 : {slot}번 칸의 무기 {weaponId}이(가) 표에 없습니다.");
            return;
        }

        _slotStyles[slot] = assets.Get<AttackStyle>(weapon.AttackStyleKey);
        _slotIcons[slot] = assets.Get<Sprite>(weapon.IconKey);
        _slotModifiers[slot] = new WeaponModifier(weapon.DamageBonus + stats.GetWeaponDamageBonus(weapon.Type), weapon.IntervalScale);
    }

    public void Prewarm()
    {
        for (int i = 0; i < WEAPON_SLOT_COUNT; i++)
        {
            PrewarmStyle(_slotStyles[i]);
        }

        PrewarmStyle(_skillStyle);
    }

    private static void PrewarmStyle(AttackStyle style)
    {
        if (null != style)
        {
            style.Prewarm();
        }
    }

    private void SelectWeapon(int index)
    {
        AttackStyle style = _slotStyles[index];

        if (null == style || index == _selectedIndex)
        {
            return;
        }

        _selectedIndex = index;
        _unit.OverrideAttackStyle(style, _slotModifiers[index]);

        OnWeaponSelected?.Invoke(index);
    }

    // 발동에 성공했을 때만 게이지를 비워, 전투 밖에서 눌러도 게이지가 날아가지 않게 한다.
    private void TryUseSkill()
    {
        if (false == _skillGauge.IsFull)
        {
            return;
        }

        if (false == _unit.Attacker.TryFireSkill(_skillStyle))
        {
            return;
        }

        _skillGauge.TryConsumeAll();

        OnSkillFired?.Invoke();
    }
}
