// 특성 보너스는 나중에 여기서 더한다.
public class PlayerStats : IUnitStats
{
    private readonly PlayerStatData _base;

    public PlayerStats(PlayerStatData baseStats)
    {
        _base = baseStats;
    }

    public int MaxHp { get { return _base.MaxHp; } }
    public float MoveSpeed { get { return _base.MoveSpeed; } }
    public float HitRadius { get { return _base.HitRadius; } }

    public float GuardDuration { get { return _base.GuardDuration; } }
    public float GuardCost { get { return _base.GuardCost; } }
    public float GuardCooldown { get { return _base.GuardCooldown; } }
    public float GuardRadius { get { return _base.GuardRadius; } }

    public float SkillGaugeMax { get { return _base.SkillGaugeMax; } }

    public float DashDistance { get { return _base.DashDistance; } }
    public float DashCooldown { get { return _base.DashCooldown; } }
}
