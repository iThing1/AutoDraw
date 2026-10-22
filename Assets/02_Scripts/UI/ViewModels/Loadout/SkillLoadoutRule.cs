// 스킬 칸. 공격 방식이 아직 없는 스킬은 얻었어도 준비 중으로 막는다.
public class SkillLoadoutRule : LoadoutRule
{
    public SkillLoadoutRule(PlayerLoadout loadout, DataTableManager tables, AssetManager assets)
        : base("Skill", loadout, assets)
    {
        foreach (SkillData skill in tables.Skills.All)
        {
            AddEntry(skill.Id, skill.Name, skill.Description, skill.IconKey, GetStyleColor(skill.AttackStyleKey), skill.PreviewKey);
        }
    }

    public override void Equip(LoadoutEntry entry)
    {
        Loadout.TryEquipSkill(entry.Id);
    }

    protected override bool IsOwned(LoadoutEntry entry)
    {
        return Loadout.HasSkill(entry.Id);
    }

    protected override bool IsEquipped(LoadoutEntry entry)
    {
        return entry.Id == Loadout.SkillId;
    }

    protected override bool IsReady(LoadoutEntry entry)
    {
        return Loadout.IsSkillReady(entry.Id);
    }
}
