using System.Collections.Generic;
using UnityEngine;

// 목표 옆을 조금 비켜 지나는 무작위 직선.
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
        return PickNear(target, offsetRange, Random.Range(0f, 180f));
    }

    // 방향은 정해 두고 옆으로 비켜 지나는 거리만 무작위로 고른다.
    public static LinePlacement PickNear(Vector3 target, float offsetRange, float angle)
    {
        Vector3 along = Geometry2D.ToDirection(angle);
        Vector3 side = new Vector3(-along.y, along.x, 0f);
        Vector3 center = target + side * Random.Range(-offsetRange, offsetRange);

        return new LinePlacement(center, angle, along);
    }

    // 가운데에서 양쪽으로 절반씩 뻗은 길. 뒤집으면 반대쪽 끝에서 출발한다.
    public Polyline ToPath(float length, bool isReversed)
    {
        Vector3 half = Along * (length * 0.5f * (isReversed ? -1f : 1f));

        return new Polyline(new List<Vector3> { Center - half, Center + half });
    }
}
