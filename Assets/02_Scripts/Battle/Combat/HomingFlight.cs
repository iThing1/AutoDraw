using UnityEngine;

// 유도탄 한 발의 값. 쏠 때마다 만들어 HomingMotion에 넘긴다.
public readonly struct HomingFlight
{
    public readonly Vector3 StartDirection;
    public readonly float Speed;
    // 초당 꺾을 수 있는 각도(도). 작을수록 크게 돌아 들어와 피하기 쉽다.
    public readonly float TurnSpeed;
    public readonly float Lifetime;
    // 방패·몸에 닿았는지 볼 때 더하는 탄 반지름.
    public readonly float Radius;
    // 켜면 대시로 피했을 때 몸을 지나가지 않고 그 자리에서 멈춘다(FlightOutcome.Dodged).
    public readonly bool EndsOnDodge;

    public HomingFlight(Vector3 startDirection, float speed, float turnSpeed, float lifetime, float radius, bool endsOnDodge)
    {
        StartDirection = startDirection;
        Speed = speed;
        TurnSpeed = turnSpeed;
        Lifetime = lifetime;
        Radius = radius;
        EndsOnDodge = endsOnDodge;
    }
}
