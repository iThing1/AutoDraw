// 플레이어의 지금 스탯. 표의 기본값을 내놓고, 특성 보너스는 나중에 여기서 더한다(쓰는 쪽은 그대로 둔다).
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
    // 이 반경 안에 들어온 공격을 막는다. 방패 그림도 같은 크기로 맞춘다.
    public float GuardRadius { get { return _base.GuardRadius; } }

    public float SkillGaugeMax { get { return _base.SkillGaugeMax; } }
}
