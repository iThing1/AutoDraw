using UnityEngine;

// 공격이 목표를 어디로 겨누고, 맞았는지를 판정하는 곳을 한군데로 모았다.
// 목표가 움직이는 것을 전제로 하며, 회피 규칙을 바꿀 때는 여기만 고친다.
public class AttackTarget
{
    // 공격자와 목표가 겹치면 방향을 정할 수 없어 기본 방향을 쓴다.
    private const float MIN_DIRECTION_SQR_LENGTH = 0.0001f;

    private readonly BattleUnit _unit;

    public AttackTarget(BattleUnit unit)
    {
        _unit = unit;
    }

    public BattleUnit Unit { get { return _unit; } }
    public bool IsDefeated { get { return _unit.Health.IsDead; } }

    public Transform FollowTransform { get { return _unit.Body; } }

    public Vector3 GetAimPoint(Vector3 offset)
    {
        return _unit.Body.position + offset;
    }

    // 명중 반경 밖으로 움직였으면 피한 것이다.
    public bool IsHit(Vector3 hitPoint)
    {
        return Vector2.Distance(hitPoint, _unit.Body.position) <= _unit.Data.HitRadius;
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
