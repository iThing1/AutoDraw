// 보스가 목표를 지켜보고 알아낼 수 있는 것(고른 무기, 막기·대시를 쓴 횟수). 맞았는지 판정하는 AttackTarget과 나눠, 지켜볼 거리가 늘어도 판정 규칙이 커지지 않게 한다.
// 공격하는 쪽마다 하나씩 만들어, 반응한 뒤 횟수를 다시 세는 기준도 여기 둔다. 기록하는 부품이 없는 목표는 아무것도 알려 주지 않는다.
public class TargetObserver
{
    // 무기를 골라 쓰고 막기·대시를 하는 목표(플레이어)만 있다.
    private readonly PlayerAttack _weapons;
    private readonly PlayerGuard _guard;
    private readonly PlayerDash _dash;
    // 마지막으로 반응했을 때의 사용 횟수. 그 뒤로 쓴 횟수만 센다.
    private int _guardMark;
    private int _dashMark;

    public TargetObserver(BattleUnit unit)
    {
        unit.TryGetComponent(out _weapons);
        unit.TryGetComponent(out _guard);
        unit.TryGetComponent(out _dash);
    }

    // 목표가 지금 고른 무기의 종류. 무기를 골라 쓰지 않는 목표면 false다.
    public bool TryGetWeaponType(out WeaponType type)
    {
        if (null == _weapons)
        {
            type = WeaponType.Basic;
            return false;
        }

        return _weapons.TryGetSelectedWeaponType(out type);
    }

    // 마지막으로 반응한 뒤 목표가 이 유틸리티를 쓴 횟수.
    public int GetUsesSinceMark(UtilityType utility)
    {
        return GetUseCount(utility) - (UtilityType.Guard == utility ? _guardMark : _dashMark);
    }

    // 반응했으니 지금부터 다시 센다.
    public void MarkUses(UtilityType utility)
    {
        if (UtilityType.Guard == utility)
        {
            _guardMark = GetUseCount(utility);
        }
        else
        {
            _dashMark = GetUseCount(utility);
        }
    }

    private int GetUseCount(UtilityType utility)
    {
        if (UtilityType.Guard == utility)
        {
            return null != _guard ? _guard.UseCount : 0;
        }

        return null != _dash ? _dash.UseCount : 0;
    }
}
