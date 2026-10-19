using System;
using System.Collections.Generic;
using System.Globalization;

// 부모는 3개까지, 있는 노드여야 하고, 자식보다 위 줄(Row가 작음)에 있어야 한다.
// 줄이 위로만 이어지므로 순환도 함께 막힌다.
public class PerkTreeRule : IDataSheetRule
{
    private const string NODE_TABLE = "PerkNodeData";
    private const string EFFECT_TABLE = "PerkEffectData";
    private const int MAX_PARENT_COUNT = 3;

    public void Check(IReadOnlyDictionary<string, DataSheetTable> tables, List<string> errors)
    {
        if (false == tables.TryGetValue(NODE_TABLE, out DataSheetTable nodes))
        {
            return;
        }

        tables.TryGetValue(EFFECT_TABLE, out DataSheetTable effects);

        for (int row = 0; row < nodes.RowCount; row++)
        {
            string prefix = $"{NODE_TABLE} {nodes.GetLineNumber(row)}줄";

            CheckParents(nodes, row, prefix, errors);
            CheckEffects(nodes, effects, row, prefix, errors);
        }
    }

    private void CheckParents(DataSheetTable nodes, int row, string prefix, List<string> errors)
    {
        string id = nodes.GetId(row);
        string[] parents = nodes.GetArray(row, "ParentId");

        if (MAX_PARENT_COUNT < parents.Length)
        {
            errors.Add($"{prefix} ParentId : 부모가 {parents.Length}개입니다. {MAX_PARENT_COUNT}개까지만 됩니다.");
        }

        bool hasRow = TryGetRow(nodes, row, out int childRow);

        for (int i = 0; i < parents.Length; i++)
        {
            string parent = parents[i];

            if (parent == id)
            {
                errors.Add($"{prefix} ParentId : 자기 자신을 부모로 가리킵니다.");
                continue;
            }

            if (0 <= Array.IndexOf(parents, parent, 0, i))
            {
                errors.Add($"{prefix} ParentId : {parent}이(가) 두 번 있습니다.");
                continue;
            }

            int parentIndex = FindRow(nodes, parent);

            if (parentIndex < 0)
            {
                errors.Add($"{prefix} ParentId : {parent} 노드가 없습니다.");
                continue;
            }

            if (hasRow && TryGetRow(nodes, parentIndex, out int parentRow) && childRow <= parentRow)
            {
                errors.Add($"{prefix} ParentId : 부모 {parent}(Row {parentRow})가 이 노드(Row {childRow})보다 위 줄에 있어야 합니다.");
            }
        }
    }

    private void CheckEffects(DataSheetTable nodes, DataSheetTable effects, int row, string prefix, List<string> errors)
    {
        string[] effectIds = nodes.GetArray(row, "EffectId");
        string[] effectValues = nodes.GetArray(row, "EffectValue");

        if (effectIds.Length != effectValues.Length)
        {
            errors.Add($"{prefix} : EffectId {effectIds.Length}개와 EffectValue {effectValues.Length}개의 수가 다릅니다.");
        }

        if (null == effects)
        {
            return;
        }

        foreach (string effectId in effectIds)
        {
            if (false == effects.ContainsId(effectId))
            {
                errors.Add($"{prefix} EffectId : {EFFECT_TABLE}에 {effectId}이(가) 없습니다.");
            }
        }
    }

    private bool TryGetRow(DataSheetTable nodes, int row, out int value)
    {
        return int.TryParse(nodes.GetCell(row, "Row"), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private int FindRow(DataSheetTable nodes, string id)
    {
        for (int row = 0; row < nodes.RowCount; row++)
        {
            if (id == nodes.GetId(row))
            {
                return row;
            }
        }

        return -1;
    }
}
