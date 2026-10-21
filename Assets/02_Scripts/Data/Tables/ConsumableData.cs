using System;
using UnityEngine;

[Serializable]
public class ConsumableData : BaseData
{
    [SerializeField] private string _name;
    // AssetRegistry의 ConsumableEffect 키. 효과와 겉모습은 이 에셋이 정한다.
    [SerializeField] private string _effectKey;
    // 회복 효과면 회복량, 잉크 지우기 효과면 지우는 반경이다.
    [SerializeField] private float _value;
    [SerializeField] private int _charges;
    [SerializeField] private float _cooldown;
    [SerializeField] private string _iconKey;
    [SerializeField] private string _description;

    public string Name { get { return _name; } }
    public string EffectKey { get { return _effectKey; } }
    public float Value { get { return _value; } }
    public int Charges { get { return _charges; } }
    public float Cooldown { get { return _cooldown; } }
    public string IconKey { get { return _iconKey; } }
    public string Description { get { return _description; } }
}
