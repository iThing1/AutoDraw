using System;
using UnityEngine;

[Serializable]
public class WeaponData : BaseData
{
    [SerializeField] private string _name;
    [SerializeField] private WeaponType _type;
    [SerializeField] private string _attackStyleKey;
    // 공격 스타일의 기본 피해에 더한다.
    [SerializeField] private int _damageBonus;
    // 공격 간격에 곱한다. 1보다 작으면 빨라진다.
    [SerializeField] private float _intervalScale;
    [SerializeField] private bool _isStarting;
    [SerializeField] private string _iconKey;
    [SerializeField] private string _description;
    [SerializeField] private string _previewKey;

    public string Name { get { return _name; } }
    public WeaponType Type { get { return _type; } }
    public string AttackStyleKey { get { return _attackStyleKey; } }
    public int DamageBonus { get { return _damageBonus; } }
    public float IntervalScale { get { return _intervalScale; } }
    public bool IsStarting { get { return _isStarting; } }
    public string IconKey { get { return _iconKey; } }
    public string Description { get { return _description; } }
    // 비어 있으면 미리보기 영상이 없다.
    public string PreviewKey { get { return _previewKey; } }
}
