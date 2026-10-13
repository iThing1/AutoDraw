using System;
using UnityEngine;

// 투사체 한 발의 성질. 단발·연사·파편 조각이 모두 이 설정을 재사용한다.
[Serializable]
public class ShotSettings
{
    [SerializeField, Min(0.1f)] private float _projectileScale = 1f;
    [SerializeField] private TrajectorySettings[] _trajectories = { new() };

    // 꼬리 길이(초)가 0이면 궤적을 그리지 않는다.
    [SerializeField, Min(0f)] private float _trailTime = 0.2f;
    [SerializeField, Min(0f)] private float _trailWidth = 0.2f;

    [SerializeField] private IntRange _damage = new(5, 10);
    [SerializeField, Range(0f, 1f)] private float _criticalChance = 0.2f;
    [SerializeField, Min(1f)] private float _criticalMultiplier = 2f;

    [SerializeField] private HitFeedbackPreset _normalHit;
    [SerializeField] private HitFeedbackPreset _criticalHit;

    public float ProjectileScale { get { return _projectileScale; } }
    public TrajectorySettings[] Trajectories { get { return _trajectories; } }

    public float TrailTime { get { return _trailTime; } }
    public float TrailWidth { get { return _trailWidth; } }

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
