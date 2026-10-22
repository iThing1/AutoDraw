// 유틸리티 칸에 끼울 능력(대시 등)을 얻는다.
public class UnlockUtilityEffect : PerkEffect
{
    private readonly UtilityType _utility;

    public UnlockUtilityEffect(UtilityType utility)
    {
        _utility = utility;
    }

    public override void Apply(PlayerStats stats, PlayerLoadout loadout, float value)
    {
        loadout.AddPerkUtility(_utility);
    }
}
