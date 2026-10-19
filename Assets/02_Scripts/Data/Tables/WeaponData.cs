using System;
using UnityEngine;

// 무기 하나. 공격 모양은 AttackStyle SO가 맡고, 표에는 그 키와 성장 수치만 둔다.
[Serializable]
public class WeaponData : BaseData
{
    [SerializeField] private string _name;
    [SerializeField] private string _attackStyleKey;
    // 공격 스타일의 기본 피해에 더한다.
    [SerializeField] private int _damageBonus;
    // 공격 간격에 곱한다. 1보다 작으면 빨라진다.
    [SerializeField] private float _intervalScale;
    [SerializeField] private string _iconKey;
    [SerializeField] private string _description;

    public string Name { get { return _name; } }
    public string AttackStyleKey { get { return _attackStyleKey; } }
    public int DamageBonus { get { return _damageBonus; } }
    public float IntervalScale { get { return _intervalScale; } }
    public string IconKey { get { return _iconKey; } }
    public string Description { get { return _description; } }
}
