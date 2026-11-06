using UnityEngine;

// 무기 표의 수치. 무기로 쓸 때는 공격 스타일 에셋의 피해·치명타·타수·게이지 대신 이 값을 쓴다. 보스와 스킬은 NONE을 써서 영향받지 않는다.
public readonly struct WeaponModifier
{
    public static readonly WeaponModifier NONE = new(false, default, 0, 0f, 1f);

    private const float MIN_ATTACK_INTERVAL = 0.05f;

    public readonly bool IsWeapon;
    // 특성 보너스가 최대 피해에 더해져 있다.
    public readonly HitStats Hit;
    public readonly float GaugeGain;
    // 표의 공격 간격을 스타일 에셋의 간격으로 나눈 값. 공격 대기와 구슬 발사 간격에 함께 곱한다.
    public readonly float IntervalScale;

    private readonly int _hitCount;

    private WeaponModifier(bool isWeapon, HitStats hit, int hitCount, float gaugeGain, float intervalScale)
    {
        IsWeapon = isWeapon;
        Hit = hit;
        _hitCount = hitCount;
        GaugeGain = gaugeGain;
        IntervalScale = intervalScale;
    }

    public static WeaponModifier Create(WeaponData weapon, AttackStyle style, int perkDamage)
    {
        IntRange damage = new IntRange(weapon.MinDamage, weapon.MaxDamage + perkDamage);
        HitStats hit = new HitStats(damage, weapon.CriticalChance, weapon.CriticalMultiplier);
        float interval = Mathf.Max(MIN_ATTACK_INTERVAL, weapon.AttackInterval);

        return new WeaponModifier(true, hit, weapon.HitCount, weapon.GaugeGain, interval / style.WeaponIntervalBase);
    }

    public int ResolveHitCount(int assetValue)
    {
        return IsWeapon ? Mathf.Max(1, _hitCount) : assetValue;
    }
}
