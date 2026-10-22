using System;
using System.Collections.Generic;
using UnityEngine;

// 효과 i번은 EffectIds[i]와 EffectValues[i]가 짝이다.
[Serializable]
public class PerkNodeData : BaseData
{
    [SerializeField] private string _name;
    [SerializeField] private PerkTreeType _tree;
    [SerializeField] private int _row;
    [SerializeField] private int _column;
    [SerializeField] private int _cost;
    [SerializeField] private string[] _parentId;
    [SerializeField] private string[] _effectId;
    [SerializeField] private float[] _effectValue;
    [SerializeField] private string _iconKey;
    [SerializeField] private string _description;

    public string Name { get { return _name; } }
    public PerkTreeType Tree { get { return _tree; } }
    public int Row { get { return _row; } }
    public int Column { get { return _column; } }
    public int Cost { get { return _cost; } }
    public IReadOnlyList<string> ParentIds { get { return _parentId; } }
    public IReadOnlyList<string> EffectIds { get { return _effectId; } }
    public IReadOnlyList<float> EffectValues { get { return _effectValue; } }
    public string IconKey { get { return _iconKey; } }
    public string Description { get { return _description; } }
}
