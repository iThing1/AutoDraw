// 무기 칸(기본·화력). 키로 고른 칸이면 선택 표시가 켜진다.
public class BasicAttackSlotViewModel : AttackSlotViewModel
{
    private readonly int _index;
    private PlayerAttackInput _input;

    public BasicAttackSlotViewModel(PlayerAttackInput input, int index)
        : base(input.GetBasicKeyLabel(index), input.GetSlotStyle(index), false)
    {
        _input = input;
        _index = index;

        _input.OnBasicStyleChanged += HandleBasicStyleChanged;
        HandleBasicStyleChanged(_input.SelectedIndex);
    }

    public override void UnBind()
    {
        if (null == _input)
        {
            return;
        }

        _input.OnBasicStyleChanged -= HandleBasicStyleChanged;
        _input = null;
    }

    private void HandleBasicStyleChanged(int selectedIndex)
    {
        SetSelected(_index == selectedIndex);
    }
}
