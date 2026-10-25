using UnityEngine;

// 막기 칸. 막기 비용만큼 게이지가 차면 쓸 수 있고, 막기를 세우면 한 번 튄다.
public class GuardSlotViewModel : ActionSlotViewModel
{
    private GuardState _state;

    public GuardSlotViewModel(string keyLabel, GuardState state, Sprite icon)
        : base(keyLabel, state.Preset.DisplayName, icon, state.Preset.IconColor, true)
    {
        _state = state;

        _state.OnStarted += HandleStarted;
        _state.OnReadinessChanged += HandleReadinessChanged;
        HandleReadinessChanged();
    }

    public override void UnBind()
    {
        if (null == _state)
        {
            return;
        }

        _state.OnStarted -= HandleStarted;
        _state.OnReadinessChanged -= HandleReadinessChanged;
        _state = null;
    }

    private void HandleReadinessChanged()
    {
        SetGauge(_state.GaugeReadyRatio, _state.HasEnoughGauge);
    }

    private void HandleStarted()
    {
        NotifyUsed();
    }
}
