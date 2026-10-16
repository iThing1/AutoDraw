using UnityEngine;

// 실제로 떨어진 자리로 회피 여부를 판정하기 위한 비행 결과.
public readonly struct FlightResult
{
    public readonly FlightOutcome Outcome;
    public readonly Vector3 LandingPoint;

    public FlightResult(FlightOutcome outcome, Vector3 landingPoint)
    {
        Outcome = outcome;
        LandingPoint = landingPoint;
    }
}
