using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 숫자 키로 기본 공격을 고르고, 게이지가 가득 차면 다음 숫자 키로 특수 스킬을 발동한다.
[RequireComponent(typeof(BattleUnit), typeof(SkillGauge))]
public class PlayerAttackInput : MonoBehaviour
{
    // 앞에서부터 기본 공격에 차례로 쓰고, 기본 공격 다음 키를 특수 스킬이 쓴다.
    // 그래서 기본 공격은 키 개수보다 하나 적은 8개까지 받는다.
    private static readonly Key[] NUMBER_KEYS =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
    };

    [SerializeField] private PlayerAttackLoadout _loadout;

    public event Action<int> OnBasicStyleChanged;
    public event Action OnSkillFired;

    private BattleUnit _unit;
    private SkillGauge _skillGauge;
    private int _selectedIndex = -1;

    public PlayerAttackLoadout Loadout { get { return _loadout; } }
    public SkillGauge SkillGauge { get { return _skillGauge; } }
    public int SelectedIndex { get { return _selectedIndex; } }

    // 기본 공격 다음 번호가 특수 스킬 키다. 기본 공격이 둘이면 3번이 된다.
    public int SkillKeyNumber { get { return BasicStyleCount + 1; } }

    private int BasicStyleCount
    {
        get
        {
            if (null == _loadout || null == _loadout.BasicStyles)
            {
                return 0;
            }

            return Mathf.Min(_loadout.BasicStyles.Length, NUMBER_KEYS.Length - 1);
        }
    }

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();
        _skillGauge = GetComponent<SkillGauge>();

        if (null == _loadout)
        {
            Debug.LogError($"플레이어 공격 입력 : {name}에 PlayerAttackLoadout이 지정되지 않았습니다.");
            return;
        }

        _skillGauge.Init(_loadout.SkillGaugeMax);
    }

    private void Start()
    {
        if (0 < BasicStyleCount)
        {
            SelectBasic(0);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (null == keyboard || null == _loadout)
        {
            return;
        }

        int basicCount = BasicStyleCount;

        for (int i = 0; i < basicCount; i++)
        {
            if (keyboard[NUMBER_KEYS[i]].wasPressedThisFrame)
            {
                SelectBasic(i);
                return;
            }
        }

        if (keyboard[NUMBER_KEYS[basicCount]].wasPressedThisFrame)
        {
            TryUseSkill();
        }
    }

    private void SelectBasic(int index)
    {
        AttackStyle style = _loadout.BasicStyles[index];

        if (null == style || index == _selectedIndex)
        {
            return;
        }

        _selectedIndex = index;
        _unit.OverrideAttackStyle(style);

        OnBasicStyleChanged?.Invoke(index);
    }

    // 발동에 성공했을 때만 게이지를 비워, 전투 밖에서 눌러도 게이지가 날아가지 않게 한다.
    private void TryUseSkill()
    {
        if (false == _skillGauge.IsFull)
        {
            return;
        }

        if (false == _unit.Attacker.TryFireSkill(_loadout.SkillStyle))
        {
            return;
        }

        _skillGauge.TryConsumeAll();

        OnSkillFired?.Invoke();
    }
}
