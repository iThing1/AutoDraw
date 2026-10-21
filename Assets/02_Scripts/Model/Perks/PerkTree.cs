using System.Collections.Generic;
using UnityEngine;

// 특성 표를 읽어 해금 규칙과 효과를 들고 있다. 찍은 기록은 PlayerProgress에 있고, 여기는 그것을 보고 답하기만 한다.
// 부모가 연결된 노드는 부모를 모두 찍어야 찍을 수 있다.
public class PerkTree
{
    private readonly DataTable<PerkNodeData> _nodes;
    // 노드 Id마다 EffectIds와 같은 순서의 효과. 만들지 못한 효과 자리는 null이다.
    private readonly Dictionary<string, PerkEffect[]> _effects = new();

    public IReadOnlyList<PerkNodeData> Nodes { get { return _nodes.All; } }

    public PerkTree(DataTableManager tables)
    {
        _nodes = tables.PerkNodes;

        foreach (PerkNodeData node in _nodes.All)
        {
            _effects.Add(node.Id, CreateEffects(node, tables.PerkEffects));
        }
    }

    public bool Contains(string nodeId)
    {
        return _nodes.TryGet(nodeId, out _);
    }

    public bool TryGetNode(string nodeId, out PerkNodeData node)
    {
        return _nodes.TryGet(nodeId, out node);
    }

    public int GetAvailablePoints(PlayerProgress progress)
    {
        return progress.EarnedPoints - GetSpentPoints(progress);
    }

    public PerkNodeState GetState(PlayerProgress progress, string nodeId)
    {
        if (progress.HasPerk(nodeId))
        {
            return PerkNodeState.Unlocked;
        }

        if (false == _nodes.TryGet(nodeId, out PerkNodeData node) || false == AreParentsUnlocked(progress, node))
        {
            return PerkNodeState.Locked;
        }

        return node.Cost <= GetAvailablePoints(progress) ? PerkNodeState.Available : PerkNodeState.Unaffordable;
    }

    // 시트를 고쳐 기록이 규칙에 어긋나면(빠진 부모, 얻은 것보다 많이 쓴 포인트) false다. 표에 없는 노드는 먼저 걸러 둔다.
    public bool IsValid(PlayerProgress progress)
    {
        foreach (string perkId in progress.PerkIds)
        {
            if (false == _nodes.TryGet(perkId, out PerkNodeData node) || false == AreParentsUnlocked(progress, node))
            {
                return false;
            }
        }

        return GetSpentPoints(progress) <= progress.EarnedPoints;
    }

    // 찍은 노드의 효과를 모두 더한다. 부르는 쪽이 먼저 특성분을 비워 둔다.
    public void Apply(PlayerProgress progress, PlayerStats stats, PlayerLoadout loadout)
    {
        foreach (string perkId in progress.PerkIds)
        {
            if (false == _nodes.TryGet(perkId, out PerkNodeData node))
            {
                continue;
            }

            PerkEffect[] effects = _effects[perkId];

            for (int i = 0; i < effects.Length; i++)
            {
                if (null != effects[i])
                {
                    effects[i].Apply(stats, loadout, node.EffectValues[i]);
                }
            }
        }
    }

    private int GetSpentPoints(PlayerProgress progress)
    {
        int spent = 0;

        foreach (string perkId in progress.PerkIds)
        {
            if (_nodes.TryGet(perkId, out PerkNodeData node))
            {
                spent += node.Cost;
            }
        }

        return spent;
    }

    private static bool AreParentsUnlocked(PlayerProgress progress, PerkNodeData node)
    {
        IReadOnlyList<string> parentIds = node.ParentIds;

        for (int i = 0; i < parentIds.Count; i++)
        {
            if (false == progress.HasPerk(parentIds[i]))
            {
                return false;
            }
        }

        return true;
    }

    // 효과 Id와 값의 수가 다르면 짝이 맞는 앞쪽만 쓴다.
    private static PerkEffect[] CreateEffects(PerkNodeData node, DataTable<PerkEffectData> effectTable)
    {
        IReadOnlyList<string> effectIds = node.EffectIds;
        int count = Mathf.Min(effectIds.Count, node.EffectValues.Count);

        if (effectIds.Count != node.EffectValues.Count)
        {
            Debug.LogError($"특성 트리 : {node.Id}의 효과 Id {effectIds.Count}개와 값 {node.EffectValues.Count}개가 짝이 맞지 않습니다.");
        }

        PerkEffect[] effects = new PerkEffect[count];

        for (int i = 0; i < count; i++)
        {
            if (effectTable.TryGet(effectIds[i], out PerkEffectData effect))
            {
                effects[i] = PerkEffect.Create(effect);
            }
            else
            {
                Debug.LogError($"특성 트리 : {node.Id}의 효과 {effectIds[i]}이(가) 효과 표에 없습니다.");
            }
        }

        return effects;
    }
}
