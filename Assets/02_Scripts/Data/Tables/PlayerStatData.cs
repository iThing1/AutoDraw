using System;
using UnityEngine;

[Serializable]
public class PlayerStatData : BaseData
{
    [SerializeField] private int _maxHp;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _hitRadius;
    [SerializeField] private float _guardDuration;
    [SerializeField] private float _guardCost;
    [SerializeField] private float _guardCooldown;
    [SerializeField] private float _guardRadius;
    [SerializeField] private float _skillGaugeMax;
    [SerializeField] private float _dashDistance;
    [SerializeField] private float _dashCooldown;

    public int MaxHp { get { return _maxHp; } }
    public float MoveSpeed { get { return _moveSpeed; } }
    public float HitRadius { get { return _hitRadius; } }
    public float GuardDuration { get { return _guardDuration; } }
    public float GuardCost { get { return _guardCost; } }
    public float GuardCooldown { get { return _guardCooldown; } }
    public float GuardRadius { get { return _guardRadius; } }
    public float SkillGaugeMax { get { return _skillGaugeMax; } }
    public float DashDistance { get { return _dashDistance; } }
    public float DashCooldown { get { return _dashCooldown; } }
}
