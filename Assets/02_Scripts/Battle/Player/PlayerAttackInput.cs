using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 기본 공격 키로 공격을 고르고, 게이지가 가득 차면 스킬 키로 특수 스킬을 발동한다. 키 배치는 입력 에셋에서 바꾼다.
[RequireComponent(typeof(BattleUnit), typeof(SkillGauge))]
public class PlayerAttackInput : MonoBehaviour
{
    [SerializeField] private PlayerAttackLoadout _loadout;
    // 로드아웃의 기본 공격과 같은 순서로 넣는다. 0번 입력이 0번 기본 공격을 고른다.
    [SerializeField] private InputActionReference[] _basicAttackActions;
    [SerializeField] private InputActionReference _skillAction;

    public event Action<int> OnBasicStyleChanged;
    public event Action OnSkillFired;

    private BattleUnit _unit;
    private SkillGauge _skillGauge;
    private int _selectedIndex = -1;

    public PlayerAttackLoadout Loadout { get { return _loadout; } }
    public SkillGauge SkillGauge { get { return _skillGauge; } }
    public int SelectedIndex { get { return _selectedIndex; } }
    public string SkillKeyLabel { get { return _skillAction.GetKeyLabel(); } }

    // 기본 공격과 입력 중 적은 쪽까지만 쓴다. 키가 없는 공격은 고를 수 없다.
    public int BasicStyleCount
    {
        get
        {
            if (null == _loadout || null == _loadout.BasicStyles || null == _basicAttackActions)
            {
                return 0;
            }

            return Mathf.Min(_loadout.BasicStyles.Length, _basicAttackActions.Length);
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

    private void OnEnable()
    {
        if (null != _basicAttackActions)
        {
            for (int i = 0; i < _basicAttackActions.Length; i++)
            {
                _basicAttackActions[i].Enable();
            }
        }

        _skillAction.Enable();
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
        if (null == _loadout)
        {
            return;
        }

        int basicCount = BasicStyleCount;

        for (int i = 0; i < basicCount; i++)
        {
            if (_basicAttackActions[i].WasPressedThisFrame())
            {
                SelectBasic(i);
                return;
            }
        }

        if (_skillAction.WasPressedThisFrame())
        {
            TryUseSkill();
        }
    }

    public string GetBasicKeyLabel(int index)
    {
        if (null == _basicAttackActions || index >= _basicAttackActions.Length)
        {
            return string.Empty;
        }

        return _basicAttackActions[index].GetKeyLabel();
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
