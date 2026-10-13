using UnityEngine;

// 공격이 목표를 어디로 겨누고, 맞았는지를 판정하는 곳을 한군데로 모았다.
// 플레이어 이동·회피가 들어오면 공격 스타일들은 그대로 두고 여기만 고친다.
public class AttackTarget
{
    private readonly BattleUnit _unit;

    public AttackTarget(BattleUnit unit)
    {
        _unit = unit;
    }

    public BattleUnit Unit { get { return _unit; } }
    public bool IsDefeated { get { return _unit.Health.IsDead; } }

    // 투사체가 날아가는 동안 따라갈 기준. 회피를 넣을 때는 null을 돌려 쏜 순간의 위치로 날아가게 한다.
    public Transform FollowTransform { get { return _unit.Body; } }

    public Vector3 GetAimPoint(Vector3 offset)
    {
        return _unit.Body.position + offset;
    }

    // 지금은 도착하면 항상 맞는다. 회피를 넣을 때 도착 지점과 목표의 현재 위치 사이 거리로 판정한다.
    public bool IsHit(Vector3 hitPoint)
    {
        return true;
    }

    // 근접 공격이 설 자리. 목표 기준으로 공격자가 있던 쪽에 distance만큼 떨어진 곳이다.
    public Vector3 GetApproachPoint(Vector3 from, float distance)
    {
        Vector3 position = _unit.transform.position;
        float side = from.x < position.x ? -1f : 1f;

        return position + new Vector3(side * distance, 0f, 0f);
    }
}
