using UnityEngine;
using UnityEngine.InputSystem;

// 소모품 키를 누르면 끼운 소모품을 쓴다. 쓸 수 있는지(횟수·쿨타임)는 ConsumableState가 정하고, 여기는 입력을 받아 효과를 일으킨다.
[RequireComponent(typeof(BattleUnit))]
public class PlayerConsumable : MonoBehaviour
{
    [SerializeField] private InputActionReference _itemAction;

    private BattleUnit _unit;
    private ConsumableState _state;
    private ConsumableEffect _effect;

    // 소모품을 끼우지 않았으면 null이다.
    public ConsumableState State { get { return _state; } }

    public string KeyLabel { get { return _itemAction.GetKeyLabel(); } }
    public Color IconColor { get { return _effect.IconColor; } }

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();

        if (null == _itemAction)
        {
            Debug.LogError($"플레이어 소모품 : {name}에 소모품 입력이 지정되지 않았습니다.");
            enabled = false;
            return;
        }

        // 소모품 칸이 비어 있으면 꺼 두고, State가 null이라 HUD도 소모품 칸을 숨긴다.
        if (false == GameManager.Tables.Consumables.TryGet(GameManager.Player.Loadout.ConsumableId, out ConsumableData data))
        {
            enabled = false;
            return;
        }

        // 효과 에셋이 없으면(키가 비었거나 레지스트리에 없으면) 쓸 수 없어 끈다.
        _effect = GameManager.Assets.Get<ConsumableEffect>(data.EffectKey);

        if (null == _effect)
        {
            enabled = false;
            return;
        }

        _state = new ConsumableState(data);
    }

    private void OnEnable()
    {
        _itemAction.Enable();
    }

    private void Update()
    {
        _state.Tick(Time.time);

        if (_itemAction.WasPressedThisFrame())
        {
            TryUse();
        }
    }

    // 직접 움직일 수 있을 때만 쓴다. 컷신(Idle)이나 다른 동작(막는 중 등) 중에는 조용히 넘긴다.
    private void TryUse()
    {
        AttackContext context = _unit.Attacker.Context;

        if (UnitState.Move != _unit.State || null == context || false == _state.CanUse(Time.time))
        {
            return;
        }

        _state.Use(Time.time);
        _effect.Apply(context, _state.Data.Value);
    }
}
