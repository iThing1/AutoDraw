// 특수 스킬 칸. 스킬 게이지가 다 차면 쓸 수 있고, 발동하면 한 번 튄다.
public class SkillSlotViewModel : AttackSlotViewModel
{
    private PlayerAttackInput _input;
    private SkillGauge _gauge;

    public SkillSlotViewModel(PlayerAttackInput input)
        : base(input.SkillKeyLabel, input.SkillStyle, true)
    {
        _input = input;
        _gauge = input.SkillGauge;

        _input.OnSkillFired += HandleSkillFired;
        _gauge.OnChanged += HandleGaugeChanged;
        HandleGaugeChanged();
    }

    public override void UnBind()
    {
        if (null != _input)
        {
            _input.OnSkillFired -= HandleSkillFired;
            _input = null;
        }

        if (null != _gauge)
        {
            _gauge.OnChanged -= HandleGaugeChanged;
            _gauge = null;
        }
    }

    private void HandleGaugeChanged()
    {
        SetGauge(_gauge.Ratio, _gauge.IsFull);
    }

    private void HandleSkillFired()
    {
        NotifyUsed();
    }
}
