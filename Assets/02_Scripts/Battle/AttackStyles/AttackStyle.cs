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
    // 켜면 이 공격이 끝나야 다음 공격 간격을 센다. 긴 패턴끼리 겹치지 않게 한다.
    [SerializeField] private bool _waitUntilFinished;
    [SerializeField] private bool _stopsMovement;

    [Header("예고")]
    // 손 달린 보스만 쓴다. 손이 없는 유닛은 예고 없이 바로 쏜다.
    [SerializeField] private HandGesture _tell;
    [SerializeField, Min(0f)] private float _tellDuration = 0.5f;

    public string DisplayName { get { return _displayName; } }
    public Color IconColor { get { return _iconColor; } }

    public float AttackInterval { get { return _attackInterval; } }
    public float GaugeGainPerHit { get { return _gaugeGainPerHit; } }
    public bool WaitUntilFinished { get { return _waitUntilFinished; } }
    public bool StopsMovement { get { return _stopsMovement; } }

    public HandGesture Tell { get { return _tell; } }
    public float TellDuration { get { return _tellDuration; } }

    // 이번 공격에 쓸 스타일. 여러 패턴을 묶은 스타일은 쏘는 쪽 상황(거리·쿨타임)을 보고 이 중 하나를 골라 돌려준다.
    // 쏘는 쪽을 모르면 null을 넘기고, 그때는 조건 없이 고른다.
    public virtual AttackStyle PickStyle(AutoAttacker picker)
    {
        return this;
    }

    // 첫 공격 때 한꺼번에 생성되며 끊기지 않도록, 이 공격만 쓰는 프리팹(포대, 칼끝 등)을 풀에 미리 만들어 둔다.
    public virtual void Prewarm()
    {
    }

    public abstract UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token);
}
