using UnityEngine;

// 실제로 떨어진 자리로 회피 여부를 판정하기 위한 비행 결과.
public readonly struct FlightResult
{
    public readonly bool HasArrived;
    public readonly Vector3 LandingPoint;

    public FlightResult(bool hasArrived, Vector3 landingPoint)
    {
        HasArrived = hasArrived;
        LandingPoint = landingPoint;
    }
}
