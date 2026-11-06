// 특수 스킬 칸. 스킬 게이지가 다 차면 쓸 수 있고, 발동하면 한 번 튄다.
public class SkillSlotViewModel : ActionSlotViewModel
{
    private PlayerAttack _attack;
    private SkillGauge _gauge;

    public SkillSlotViewModel(PlayerAttack attack)
        : base(attack.SkillKeyLabel, attack.SkillStyle, attack.SkillIcon, true)
    {
        _attack = attack;
        _gauge = attack.SkillGauge;

        _attack.OnSkillFired += HandleSkillFired;
        _gauge.OnChanged += HandleGaugeChanged;
        HandleGaugeChanged();
    }

    public override void UnBind()
    {
        if (null != _attack)
        {
            _attack.OnSkillFired -= HandleSkillFired;
            _attack = null;
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
