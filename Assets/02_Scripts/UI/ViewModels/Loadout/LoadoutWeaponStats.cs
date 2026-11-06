// 장비 화면에 보일 무기 스펙. 무기 표의 값과 특성으로 오른 값을 나눠 들고 있는다.
public class LoadoutWeaponStats
{
    private readonly int _minDamage;
    private readonly int _maxDamage;
    private readonly int _hitCount;
    private readonly int _perkDamage;
    private readonly float _criticalChance;
    private readonly float _attacksPerSecond;

    public int MinDamage { get { return _minDamage; } }
    public int MaxDamage { get { return _maxDamage; } }
    // 한 번 공격의 타수. 1이면 표시하지 않는다.
    public int HitCount { get { return _hitCount; } }
    // 특성으로 오른 최대 피해. MaxDamage에 들어 있지 않다.
    public int PerkDamage { get { return _perkDamage; } }
    public float CriticalChance { get { return _criticalChance; } }
    public float AttacksPerSecond { get { return _attacksPerSecond; } }

    public LoadoutWeaponStats(WeaponData weapon, int perkDamage)
    {
        _minDamage = weapon.MinDamage;
        _maxDamage = weapon.MaxDamage;
        _hitCount = weapon.HitCount;
        _perkDamage = perkDamage;
        _criticalChance = weapon.CriticalChance;
        _attacksPerSecond = 0f < weapon.AttackInterval ? 1f / weapon.AttackInterval : 0f;
    }
}
