// 대시 칸. 쿨타임이 차오르고, 대시하면 한 번 튀고, 쿨타임 중에 누르면 흔들린다.
public class DashSlotViewModel : ActionSlotViewModel
{
    private DashState _state;

    public DashSlotViewModel(string keyLabel, DashState state)
        : base(keyLabel, state.Preset.DisplayName, state.Preset.IconColor, true)
    {
        _state = state;

        _state.OnStarted += HandleStarted;
        _state.OnChanged += HandleChanged;
        _state.OnRejected += HandleRejected;
        HandleChanged();
    }

    public override void UnBind()
    {
        if (null == _state)
        {
            return;
        }

        _state.OnStarted -= HandleStarted;
        _state.OnChanged -= HandleChanged;
        _state.OnRejected -= HandleRejected;
        _state = null;
    }

    private void HandleChanged()
    {
        SetGauge(_state.CooldownRatio, 1f <= _state.CooldownRatio);
    }

    private void HandleStarted()
    {
        NotifyUsed();
    }

    private void HandleRejected()
    {
        NotifyRejected();
    }
}
