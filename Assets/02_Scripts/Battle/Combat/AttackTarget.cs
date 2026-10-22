using UnityEngine;

// 공격이 목표를 어디로 겨누고, 맞았는지를 판정하는 곳을 한군데로 모았다.
// 목표가 움직이는 것을 전제로 하며, 회피 규칙을 바꿀 때는 여기만 고친다.
public class AttackTarget
{
    // 공격자와 목표가 겹치면 방향을 정할 수 없어 기본 방향을 쓴다.
    private const float MIN_DIRECTION_SQR_LENGTH = 0.0001f;

    private readonly BattleUnit _unit;
    private readonly float _bodyRadius;

    public AttackTarget(BattleUnit unit)
    {
        _unit = unit;
        _bodyRadius = unit.GetBodyRadius();
    }

    public BattleUnit Unit { get { return _unit; } }
    public bool IsDefeated { get { return _unit.Health.IsDead; } }

    public Transform FollowTransform { get { return _unit.Body; } }

    public Vector3 GetAimPoint(Vector3 offset)
    {
        return _unit.Body.position + offset;
    }

    // 지금 움직이는 방향으로 seconds초 뒤에 있을 자리. 떨어지기까지 시간이 걸리는 공격이 앞질러 노릴 때 쓴다.
    public Vector3 GetPredictedPoint(float seconds)
    {
        if (null == _unit.Mover)
        {
            return _unit.Body.position;
        }

        return _unit.Mover.PredictPosition(seconds) + (_unit.Body.position - _unit.transform.position);
    }

    // 명중 반경 밖으로 움직였거나 대시로 피하는 중이면 피한 것이다.
    public bool IsHit(Vector3 hitPoint)
    {
        return false == _unit.IsDodging && Vector2.Distance(hitPoint, _unit.Body.position) <= _unit.Stats.HitRadius;
    }

    // 날아가는 탄이 몸 그림과 겹쳤는지. 넉넉한 도착 판정(명중 반경)과 달리 실제 크기로 봐서,
    // 빠른 탄이 몸을 뚫고 지나가지 않으면서도 허공에서 미리 맞지 않게 한다.
    public bool IsTouching(Vector3 position, float attackRadius)
    {
        return false == _unit.IsDodging && Vector2.Distance(position, _unit.Body.position) <= _bodyRadius + attackRadius;
    }

    // 움직이는 공격이 지난 길 위에서 몸에 닿았는지. 이전 자리와 지금 자리를 잇는 선분으로 봐, 빠르게 지나가도 건너뛰지 않는다.
    public bool IsTouchingSegment(Vector3 from, Vector3 to, float attackRadius)
    {
        return IsTouching(Geometry2D.GetClosestPointOnSegment(from, to, _unit.Body.position), attackRadius);
    }

    public bool IsInBlast(Vector3 center, float radius)
    {
        return false == _unit.IsDodging && Vector2.Distance(center, _unit.Body.position) <= radius;
    }

    // 방패가 떠 있는 동안 방패에 닿은 공격은 막힌다. 투사체는 날아오는 도중에도 이걸로 확인한다.
    // attackRadius는 공격 자체의 크기로, 큰 탄은 중심보다 가장자리가 먼저 방패에 닿는다.
    public bool IsBlockedAt(Vector3 position, float attackRadius)
    {
        return _unit.IsGuarding && Vector2.Distance(position, _unit.Body.position) <= _unit.GuardRadius + attackRadius;
    }

    // 방패를 든 동안에는 맞을 자리에 온 공격도 막는다. 방패가 명중 반경보다 작아도 막기를 뚫고 맞지 않게 한다.
    public bool TryBlockHit(Vector3 hitPoint)
    {
        bool isOnShield = IsBlockedAt(hitPoint, 0f);
        bool isHitWhileGuarding = _unit.IsGuarding && IsHit(hitPoint);

        if (false == isOnShield && false == isHitWhileGuarding)
        {
            return false;
        }

        _unit.NotifyBlocked(hitPoint);
        return true;
    }

    // 날아오는 도중 이미 막힌 투사체용. 도착한 순간에는 방패가 내려가 있을 수 있어 다시 확인하지 않는다.
    public void NotifyBlocked(Vector3 hitPoint)
    {
        _unit.NotifyBlocked(hitPoint);
    }

    // 소용돌이처럼 목표를 끌거나 미는 힘(초당 거리). 직접 조작하는 이동에 더해지고, 움직이지 않는 목표에는 아무 일도 없다.
    // 다 쓰면 Vector2.zero를 넘겨 풀어 준다.
    public void SetPull(Vector2 velocity)
    {
        if (null != _unit.Mover)
        {
            _unit.Mover.SetExternalVelocity(velocity);
        }
    }

    // 근접 공격이 설 자리. 목표에서 공격자 쪽으로 distance만큼 떨어진 곳이다.
    public Vector3 GetApproachPoint(Vector3 from, float distance)
    {
        Vector3 position = _unit.transform.position;
        Vector3 direction = from - position;
        direction.z = 0f;

        if (direction.sqrMagnitude < MIN_DIRECTION_SQR_LENGTH)
        {
            direction = Vector3.right;
        }

        return position + direction.normalized * distance;
    }
}
