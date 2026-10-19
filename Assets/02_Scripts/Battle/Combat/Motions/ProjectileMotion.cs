using UnityEngine;

// 날아가는 투사체의 움직임 하나. 방패·몸에 닿았는지 보는 판정은 여기서 함께 쓰고, 어떻게 움직이는지는 하위 클래스가 정한다.
// 풀에서 꺼낸 투사체에 붙어 비행 한 번 동안의 상태를 필드로 들어, 탄마다 새로 할당하지 않는다. 새 움직임은 하위 클래스 하나만 추가하면 된다.
public abstract class ProjectileMotion : MonoBehaviour
{
    private Transform _transform;
    private SpriteRenderer _renderer;

    private AttackTarget _target;
    private float _radius;
    private Vector3 _lastPosition;
    private Vector3 _lastTargetPosition;
    private FlightOutcome _contactOutcome;
    private Vector3 _contactPoint;

    protected Transform CachedTransform { get { return _transform; } }
    // 목표가 없는(null) 비행은 맞히지 않는다.
    protected AttackTarget Target { get { return _target; } }
    // 지난 판정 때의 자리. 판정 전에 이번 프레임 이동량을 잴 때 쓴다.
    protected Vector3 LastPosition { get { return _lastPosition; } }
    // 닿기 전에는 Missed다.
    protected FlightOutcome ContactOutcome { get { return _contactOutcome; } }
    protected Vector3 ContactPoint { get { return _contactPoint; } }

    protected virtual void Awake()
    {
        _transform = transform;
        TryGetComponent(out _renderer);
    }

    // 큰 탄은 가장자리가 먼저 방패에 닿으므로, 판정에 탄의 반지름을 더한다. 크기를 맞춘 뒤에 불러야 그림 크기가 맞게 잰다.
    protected void BeginContact(AttackTarget target)
    {
        BeginContact(target, null != _renderer ? _renderer.bounds.extents.x : 0f);
    }

    protected void BeginContact(AttackTarget target, float radius)
    {
        _target = target;
        _radius = radius;
        _contactOutcome = FlightOutcome.Missed;
        _contactPoint = _transform.position;

        ResetContactPath();
    }

    // 지나온 길을 지금 자리부터 다시 잰다. 빗나간 뒤 이어 날 때처럼 움직임이 바뀔 때 부른다.
    protected void ResetContactPath()
    {
        _lastPosition = _transform.position;

        if (null != _target)
        {
            _lastTargetPosition = _target.FollowTransform.position;
        }
    }

    // 탄과 목표가 같은 프레임에 함께 움직이면 서로 엇갈려 지나갈 수 있다.
    // 목표 기준으로 본 탄의 이동 선분에서 몸에 가장 가까운 점으로 판정하고, 닿았으면 결과와 그 자리를 기억해 true를 돌려준다.
    protected bool CheckContact()
    {
        if (null == _target)
        {
            return false;
        }

        Vector3 currentPosition = _transform.position;
        Vector3 targetPosition = _target.FollowTransform.position;
        Vector3 contactPoint = targetPosition + Geometry2D.GetClosestPointOnSegment(_lastPosition - _lastTargetPosition, currentPosition - targetPosition, Vector3.zero);

        _lastPosition = currentPosition;
        _lastTargetPosition = targetPosition;

        if (_target.IsBlockedAt(contactPoint, _radius))
        {
            _contactOutcome = FlightOutcome.Blocked;
        }
        else if (_target.IsTouching(contactPoint, _radius))
        {
            _contactOutcome = FlightOutcome.Arrived;
        }
        else
        {
            return false;
        }

        _contactPoint = contactPoint;
        return true;
    }

    // 느려지는 구역 안에 있는 동안의 이 탄 속도 배율. 게임 전체 시간은 건드리지 않는다.
    protected float GetSlowScale()
    {
        return GameManager.SlowZones.GetProjectileScale(_transform.position);
    }

    // 비행이 끝나면 목표를 놓아, 풀에 있는 동안 전투 객체를 붙잡지 않는다.
    protected void EndContact()
    {
        _target = null;
    }
}
