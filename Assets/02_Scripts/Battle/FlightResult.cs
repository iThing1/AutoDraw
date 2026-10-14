using UnityEngine;

// 실제로 떨어진 자리로 회피 여부를 판정하기 위한 비행 결과.
public readonly struct FlightResult
{
    public readonly bool HasArrived;
    public readonly Vector3 LandingPoint;
    // 날아오는 도중 방패에 닿아 그 자리에서 멈췄다.
    public readonly bool IsBlocked;

    public FlightResult(bool hasArrived, Vector3 landingPoint, bool isBlocked)
    {
        HasArrived = hasArrived;
        LandingPoint = landingPoint;
        IsBlocked = isBlocked;
    }
}
