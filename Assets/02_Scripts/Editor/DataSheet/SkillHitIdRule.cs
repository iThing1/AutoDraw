using System.Collections.Generic;
using UnityEditor;

// 공격 에셋의 스킬 타격 Id와 표의 줄이 서로 맞는지 본다. 표에 없는 Id와 아무도 쓰지 않는 줄은 오류다.
public class SkillHitIdRule : IDataSheetRule
{
    private const string TABLE = "SkillHitData";
    private const string ID_PROPERTY = "_skillHitId";

    public void Check(IReadOnlyDictionary<string, DataSheetTable> tables, List<string> errors)
    {
        if (false == tables.TryGetValue(TABLE, out DataSheetTable table))
        {
            return;
        }

        HashSet<string> usedIds = new();

        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AttackStyle)}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AttackStyle style = AssetDatabase.LoadAssetAtPath<AttackStyle>(path);

            if (null != style)
            {
                CollectIds(style, path, table, usedIds, errors);
            }
        }

        for (int row = 0; row < table.RowCount; row++)
        {
            if (false == usedIds.Contains(table.GetId(row)))
            {
                errors.Add($"{TABLE} {table.GetLineNumber(row)}줄 : {table.GetId(row)}을(를) 쓰는 공격 에셋이 없습니다.");
            }
        }
    }

    private static void CollectIds(AttackStyle style, string path, DataSheetTable table, HashSet<string> usedIds, List<string> errors)
    {
        SerializedObject serialized = new(style);
        SerializedProperty property = serialized.GetIterator();

        while (property.NextVisible(true))
        {
            if (SerializedPropertyType.String != property.propertyType || ID_PROPERTY != property.name || 0 == property.stringValue.Length)
            {
                continue;
            }

            usedIds.Add(property.stringValue);

            if (false == table.ContainsId(property.stringValue))
            {
                errors.Add($"{path} : 스킬 타격 {property.stringValue}이(가) {TABLE}에 없습니다.");
            }
        }
    }
}
