using System;
using UnityEngine;

// 스킬 타격 하나의 수치. 에셋의 HitSettings가 SkillHitId로 이 줄을 가리킨다.
[Serializable]
public class SkillHitData : BaseData
{
    [SerializeField] private int _minDamage;
    [SerializeField] private int _maxDamage;
    [SerializeField] private float _criticalChance;
    [SerializeField] private float _criticalMultiplier;

    public HitStats Stats { get { return new HitStats(new IntRange(_minDamage, _maxDamage), _criticalChance, _criticalMultiplier); } }
}
