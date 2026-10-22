// 무기 칸(기본·화력). 키로 고른 칸이면 선택 표시가 켜진다.
public class WeaponSlotViewModel : ActionSlotViewModel
{
    private readonly int _index;
    private PlayerAttack _attack;

    public WeaponSlotViewModel(PlayerAttack attack, int index)
        : base(attack.GetWeaponKeyLabel(index), attack.GetSlotStyle(index), false)
    {
        _attack = attack;
        _index = index;

        _attack.OnWeaponSelected += HandleWeaponSelected;
        HandleWeaponSelected(_attack.SelectedIndex);
    }

    public override void UnBind()
    {
        if (null == _attack)
        {
            return;
        }

        _attack.OnWeaponSelected -= HandleWeaponSelected;
        _attack = null;
    }

    private void HandleWeaponSelected(int selectedIndex)
    {
        SetSelected(_index == selectedIndex);
    }
}
