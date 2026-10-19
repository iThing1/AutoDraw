using UnityEngine;

// 목표 근처를 지나는 무작위 직선 하나. 방향은 무작위이고 목표에서 옆으로 조금 비켜 지나, 선과 선 사이 빈자리를 찾게 한다.
// 화면을 가로지르는 선 공격(먹선 긋기, 그림자 돌진)이 함께 쓴다.
public readonly struct LinePlacement
{
    public readonly Vector3 Center;
    public readonly float Angle;
    public readonly Vector3 Along;

    private LinePlacement(Vector3 center, float angle, Vector3 along)
    {
        Center = center;
        Angle = angle;
        Along = along;
    }

    // 직선은 양쪽으로 뻗으므로 0~180도만 골라도 모든 방향이 나온다.
    public static LinePlacement PickNear(Vector3 target, float offsetRange)
    {
        float angle = Random.Range(0f, 180f);
        Vector3 along = Geometry2D.ToDirection(angle);
        Vector3 side = new Vector3(-along.y, along.x, 0f);
        Vector3 center = target + side * Random.Range(-offsetRange, offsetRange);

        return new LinePlacement(center, angle, along);
    }
}
