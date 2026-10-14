using System;
using UnityEngine;

// 투사체 한 발의 성질. 단발·연사·파편 조각이 모두 이 설정을 재사용한다.
[Serializable]
public class ShotSettings
{
    [SerializeField, Min(0.1f)] private float _projectileScale = 1f;
    [SerializeField] private TrajectorySettings[] _trajectories = { new() };
    // 움직이는 목표를 따라가는 정도. 1이면 반드시 맞고, 낮출수록 피하기 쉽다.
    [SerializeField, Range(0f, 1f)] private float _homing = 1f;
    // 빗나간 뒤 날아오던 방향으로 더 날아가는 시간. 0이면 도착 지점에서 바로 사라진다.
    [SerializeField, Min(0f)] private float _missTravelTime = 0.3f;

    // 꼬리 길이(초)가 0이면 궤적을 그리지 않는다.
    [SerializeField, Min(0f)] private float _trailTime = 0.2f;
    [SerializeField, Min(0f)] private float _trailWidth = 0.2f;
    // 끄면 궤적이 물감 색을 따라가고, 켜면 아래 색으로 고정한다. 보스 탄은 물감이 아닌 잉크 줄기로 보이도록 검게 고정한다.
    [SerializeField] private bool _useTrailColor;
    [SerializeField] private Color _trailColor = Color.black;

    [SerializeField] private IntRange _damage = new(5, 10);
    [SerializeField, Range(0f, 1f)] private float _criticalChance = 0.2f;
    [SerializeField, Min(1f)] private float _criticalMultiplier = 2f;

    [SerializeField] private HitFeedbackPreset _normalHit;
    [SerializeField] private HitFeedbackPreset _criticalHit;

    public float ProjectileScale { get { return _projectileScale; } }
    public TrajectorySettings[] Trajectories { get { return _trajectories; } }
    public float Homing { get { return _homing; } }
    public float MissTravelTime { get { return _missTravelTime; } }

    public float TrailTime { get { return _trailTime; } }
    public float TrailWidth { get { return _trailWidth; } }
    public bool UseTrailColor { get { return _useTrailColor; } }
    public Color TrailColor { get { return _trailColor; } }

    public IntRange Damage { get { return _damage; } }
    public float CriticalChance { get { return _criticalChance; } }
    public float CriticalMultiplier { get { return _criticalMultiplier; } }

    // 치명타가 나올 수 없는 공격까지 배율을 곱하면 자동 회복 경고가 실제보다 크게 잡히므로 뺀다.
    public int MaxHitDamage
    {
        get
        {
            if (0f >= _criticalChance)
            {
                return Damage.Max;
            }

            return Mathf.RoundToInt(Damage.Max * _criticalMultiplier);
        }
    }

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
