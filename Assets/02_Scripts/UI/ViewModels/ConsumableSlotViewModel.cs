using UnityEngine;

// 소모품 칸. 남은 횟수를 이름 옆에 붙이고, 횟수가 남았고 쿨타임이 다 찼을 때 쓸 수 있다. 쓰면 한 번 튄다.
public class ConsumableSlotViewModel : ActionSlotViewModel
{
    private const string NAME_FORMAT = "{0} x{1}";

    private ConsumableState _state;

    public ConsumableSlotViewModel(string keyLabel, ConsumableState state, Color iconColor)
        : base(keyLabel, FormatName(state), iconColor, true)
    {
        _state = state;

        _state.OnChanged += HandleChanged;
        _state.OnUsed += HandleUsed;
        HandleChanged();
    }

    public override void UnBind()
    {
        if (null == _state)
        {
            return;
        }

        _state.OnChanged -= HandleChanged;
        _state.OnUsed -= HandleUsed;
        _state = null;
    }

    private static string FormatName(ConsumableState state)
    {
        return string.Format(NAME_FORMAT, state.Data.Name, state.Charges);
    }

    // 다 쓰면 게이지를 비워, 쿨타임이 끝나도 다시 쓸 수 있는 것처럼 보이지 않게 한다.
    private void HandleChanged()
    {
        bool hasCharges = 0 < _state.Charges;

        SetDisplayName(FormatName(_state));
        SetGauge(hasCharges ? _state.CooldownRatio : 0f, hasCharges && 1f <= _state.CooldownRatio);
    }

    private void HandleUsed()
    {
        NotifyUsed();
    }
}
