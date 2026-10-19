using System;
using UnityEngine;

// 전투 중에 쓰는 소모품 하나(물감 = 체력 회복, 리무버 = 검은 잉크 지우기).
[Serializable]
public class ConsumableData : BaseData
{
    [SerializeField] private string _name;
    // Heal·RemoveInk. 쓰는 쪽에서 enum으로 바꾼다.
    [SerializeField] private string _effectType;
    [SerializeField] private float _value;
    [SerializeField] private int _charges;
    [SerializeField] private float _cooldown;
    [SerializeField] private string _iconKey;
    [SerializeField] private string _description;

    public string Name { get { return _name; } }
    public string EffectType { get { return _effectType; } }
    public float Value { get { return _value; } }
    public int Charges { get { return _charges; } }
    public float Cooldown { get { return _cooldown; } }
    public string IconKey { get { return _iconKey; } }
    public string Description { get { return _description; } }
}
