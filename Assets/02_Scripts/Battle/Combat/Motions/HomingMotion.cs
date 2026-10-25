using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 유도탄의 움직임. 일정한 빠르기로 방향만 꺾어 목표를 쫓아, 플레이어가 빨리 달려도 빨라지지 않는다.
public class HomingMotion : ProjectileMotion
{
    // 방패나 몸에 닿으면 그 자리에서 멈추고, 수명이 다하면 빗나간 것이다. 꺼내고 돌려주는 일은 부르는 쪽이 한다.
    public async UniTask<FlightResult> FlyAsync(AttackTarget target, HomingFlight flight, CancellationToken token)
    {
        BeginContact(target, flight.Radius, flight.EndsOnDodge);

        Vector3 direction = flight.StartDirection;
        float elapsed = 0f;

        try
        {
            while (elapsed < flight.Lifetime)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);

                float deltaTime = Time.deltaTime * GetSlowScale();
                elapsed += Time.deltaTime;

                Vector3 toTarget = target.GetAimPoint(Vector3.zero) - CachedTransform.position;
                direction = Vector3.RotateTowards(direction, toTarget.normalized, flight.TurnSpeed * Mathf.Deg2Rad * deltaTime, 0f);

                CachedTransform.position += direction * (flight.Speed * deltaTime);
                CachedTransform.rotation = Quaternion.Euler(0f, 0f, Geometry2D.ToAngle(direction));

                if (CheckContact())
                {
                    return new FlightResult(ContactOutcome, ContactPoint);
                }
            }

            return new FlightResult(FlightOutcome.Missed, CachedTransform.position);
        }
        finally
        {
            EndContact();
        }
    }
}
