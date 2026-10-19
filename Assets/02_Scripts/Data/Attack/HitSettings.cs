using System;
using UnityEngine;

// 맞았을 때의 피해와 타격 연출. 탄은 ShotSettings 안에 들고, 근접·장판·기믹처럼 날아가지 않는 공격은 이것만 쓴다.
[Serializable]
public class HitSettings
{
    [SerializeField] private IntRange _damage = new(5, 10);
    [SerializeField, Range(0f, 1f)] private float _criticalChance = 0.2f;
    [SerializeField, Min(1f)] private float _criticalMultiplier = 2f;

    [SerializeField] private HitFeedbackPreset _normalHit;
    [SerializeField] private HitFeedbackPreset _criticalHit;

    public IntRange Damage { get { return _damage; } }
    public float CriticalChance { get { return _criticalChance; } }
    public float CriticalMultiplier { get { return _criticalMultiplier; } }

    // 치명타 프리셋이 비어 있으면 일반 프리셋으로 대신한다.
    public HitFeedbackPreset GetHitFeedback(bool isCritical)
    {
        if (isCritical && null != _criticalHit)
        {
            return _criticalHit;
        }

        return _normalHit;
    }
}
