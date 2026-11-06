using System;
using UnityEngine;

// 맞았을 때의 피해와 타격 연출. 탄은 ShotSettings 안에 들고, 근접·장판·기믹처럼 날아가지 않는 공격은 이것만 쓴다.
[Serializable]
public class HitSettings
{
    // 플레이어 스킬의 타격이면 SkillHitData 표의 Id. 있으면 아래 수치 대신 표 값을 쓴다. 무기는 무기 표 값을 쓴다.
    [SerializeField] private string _skillHitId;
    [SerializeField] private IntRange _damage = new(5, 10);
    [SerializeField, Range(0f, 1f)] private float _criticalChance = 0.2f;
    [SerializeField, Min(1f)] private float _criticalMultiplier = 2f;

    [SerializeField] private HitFeedbackPreset _normalHit;
    [SerializeField] private HitFeedbackPreset _criticalHit;

    public string SkillHitId { get { return _skillHitId; } }
    public HitStats Stats { get { return new HitStats(_damage, _criticalChance, _criticalMultiplier); } }

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
