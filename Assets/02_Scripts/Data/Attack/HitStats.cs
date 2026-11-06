// 한 번 맞힐 때의 수치. 에셋(HitSettings)이나 표(무기·스킬 타격) 어디서 왔든 같은 모양으로 굴린다.
public readonly struct HitStats
{
    public readonly IntRange Damage;
    public readonly float CriticalChance;
    public readonly float CriticalMultiplier;

    public HitStats(IntRange damage, float criticalChance, float criticalMultiplier)
    {
        Damage = damage;
        CriticalChance = criticalChance;
        CriticalMultiplier = criticalMultiplier;
    }
}
