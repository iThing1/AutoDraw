using System;
using UnityEngine;

// 값은 노드 쪽(PerkNodeData.EffectValues)에 있어 같은 효과를 여러 노드가 다른 값으로 쓴다.
[Serializable]
public class PerkEffectData : BaseData
{
    [SerializeField] private PerkEffectType _effectType;
    [SerializeField] private string _target;
    // {0} 자리에 효과 값이 들어간다.
    [SerializeField] private string _description;

    public PerkEffectType EffectType { get { return _effectType; } }
    public string Target { get { return _target; } }
    public string Description { get { return _description; } }
}
