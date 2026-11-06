using System;
using UnityEngine;

[Serializable]
public class WeaponData : BaseData
{
    [SerializeField] private string _name;
    [SerializeField] private WeaponType _type;
    [SerializeField] private string _attackStyleKey;
    [SerializeField] private int _minDamage;
    [SerializeField] private int _maxDamage;
    // 0~1.
    [SerializeField] private float _criticalChance;
    // 초 단위. 물감 구슬은 구슬을 내보내는 간격이다.
    [SerializeField] private float _attackInterval;
    [SerializeField] private float _criticalMultiplier;
    // 한 번 공격의 발 수나 타수. 물감 구슬은 쓰지 않는다.
    [SerializeField] private int _hitCount;
    [SerializeField] private float _gaugeGain;
    [SerializeField] private bool _isStarting;
    [SerializeField] private string _iconKey;
    [SerializeField] private string _description;
    [SerializeField] private string _previewKey;

    public string Name { get { return _name; } }
    public WeaponType Type { get { return _type; } }
    public string AttackStyleKey { get { return _attackStyleKey; } }
    public int MinDamage { get { return _minDamage; } }
    public int MaxDamage { get { return _maxDamage; } }
    public float CriticalChance { get { return _criticalChance; } }
    public float AttackInterval { get { return _attackInterval; } }
    public float CriticalMultiplier { get { return _criticalMultiplier; } }
    public int HitCount { get { return _hitCount; } }
    public float GaugeGain { get { return _gaugeGain; } }
    public bool IsStarting { get { return _isStarting; } }
    public string IconKey { get { return _iconKey; } }
    public string Description { get { return _description; } }
    // 비어 있으면 미리보기 영상이 없다.
    public string PreviewKey { get { return _previewKey; } }
}
