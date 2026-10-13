using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 공격 하나의 공통 정보(표시, 간격, 게이지). 어떻게 쏘는지는 하위 클래스가 정해,
// 새 공격 방식은 기존 코드를 고치지 않고 하위 클래스 하나만 추가하면 된다.
public abstract class AttackStyle : ScriptableObject
{
    [Header("표시")]
    [SerializeField] private string _displayName;
    [SerializeField] private Color _iconColor = Color.white;

    [Header("공격")]
    [SerializeField, Min(0.05f)] private float _attackInterval = 1f;
    [SerializeField, Min(0f)] private float _gaugeGainPerHit = 5f;

    public string DisplayName { get { return _displayName; } }
    public Color IconColor { get { return _iconColor; } }

    public float AttackInterval { get { return _attackInterval; } }
    public float GaugeGainPerHit { get { return _gaugeGainPerHit; } }

    // 한 번 맞을 때 들어올 수 있는 가장 큰 피해. 플레이어 자동 회복 기준이 충분한지 확인할 때 쓴다.
    public abstract int MaxHitDamage { get; }

    public abstract UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token);
}
