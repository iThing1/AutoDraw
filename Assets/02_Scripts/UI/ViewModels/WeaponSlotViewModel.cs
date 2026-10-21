// 무기 칸(기본·화력). 키로 고른 칸이면 선택 표시가 켜진다.
public class WeaponSlotViewModel : ActionSlotViewModel
{
    private readonly int _index;
    private PlayerAttackInput _input;

    public WeaponSlotViewModel(PlayerAttackInput input, int index)
        : base(input.GetWeaponKeyLabel(index), input.GetSlotStyle(index), false)
    {
        _input = input;
        _index = index;

        _input.OnWeaponSelected += HandleWeaponSelected;
        HandleWeaponSelected(_input.SelectedIndex);
    }

    public override void UnBind()
    {
        if (null == _input)
        {
            return;
        }

        _input.OnWeaponSelected -= HandleWeaponSelected;
        _input = null;
    }

    private void HandleWeaponSelected(int selectedIndex)
    {
        SetSelected(_index == selectedIndex);
    }
}
