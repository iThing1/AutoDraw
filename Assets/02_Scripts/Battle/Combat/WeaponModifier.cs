// 무기가 공격 스타일에 얹는 값. 보스와 스킬은 NONE을 쓴다. 특성의 칸 보너스도 나중에 여기에 더한다.
public readonly struct WeaponModifier
{
    public static readonly WeaponModifier NONE = new(0, 1f);

    // 최대 피해에 더한다.
    public readonly int DamageBonus;
    // 공격 간격에 곱한다.
    public readonly float IntervalScale;

    public WeaponModifier(int damageBonus, float intervalScale)
    {
        DamageBonus = damageBonus;
        IntervalScale = intervalScale;
    }
}
