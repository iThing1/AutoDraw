using System;
using UnityEngine;

[Serializable]
public class ConsumableData : BaseData
{
    [SerializeField] private string _name;
    [SerializeField] private ConsumableEffectType _effectType;
    // Heal이면 회복량, RemoveInk이면 지우는 반경이다.
    [SerializeField] private float _value;
    [SerializeField] private int _charges;
    [SerializeField] private float _cooldown;
    [SerializeField] private string _iconKey;
    [SerializeField] private string _description;

    public string Name { get { return _name; } }
    public ConsumableEffectType EffectType { get { return _effectType; } }
    public float Value { get { return _value; } }
    public int Charges { get { return _charges; } }
    public float Cooldown { get { return _cooldown; } }
    public string IconKey { get { return _iconKey; } }
    public string Description { get { return _description; } }
}
