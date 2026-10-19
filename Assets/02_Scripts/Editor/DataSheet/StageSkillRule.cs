using System.Collections.Generic;
using UnityEditor;

public class StageSkillRule : IDataSheetRule
{
    private const string SKILL_TABLE = "SkillData";

    public void Check(IReadOnlyDictionary<string, DataSheetTable> tables, List<string> errors)
    {
        if (false == tables.TryGetValue(SKILL_TABLE, out DataSheetTable skills))
        {
            return;
        }

        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(StageData)}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);

            if (null == stage || string.IsNullOrEmpty(stage.ClearSkillId))
            {
                continue;
            }

            if (false == skills.ContainsId(stage.ClearSkillId))
            {
                errors.Add($"{path} : 깨면 얻는 스킬 {stage.ClearSkillId}이(가) {SKILL_TABLE}에 없습니다.");
            }
        }
    }
}
