using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

// 보통 탄의 움직임. 궤적(포물선·직선·물결) 트윈을 따라 날며 움직이는 목표를 조금씩 따라가고, 빗나가면 날아오던 방향으로 조금 더 날다 사라진다.
// 탄막은 한 번에 수십 발이 날아서, 탄마다 람다(클로저)를 새로 만들지 않도록 매 프레임 콜백은 처음 붙을 때 한 번만 만든다.
public class TrajectoryMotion : ProjectileMotion
{
    private TweenCallback _updateFlight;
    private TweenCallback _updatePass;

    private Tween _flight;
    private Tween _pass;
    private float _homing;
    private float _maxHomingSpeed;
    private Vector3 _targetStartPosition;
    private Vector3 _homingOffset;
    private Vector3 _velocity;

    protected override void Awake()
    {
        base.Awake();

        _updateFlight = UpdateFlight;
        _updatePass = UpdatePass;
    }

    // 크기·색·꼬리는 부르는 쪽이 먼저 맞춰 둔다. 목표가 없으면(null) 맞히지 않는 비행이라 느려지는 구역만 반영한다.
    public async UniTask<FlightResult> FlyAsync(ShotSettings shot, TrajectorySettings trajectory, Vector3 start, Vector3 end, AttackTarget target,
        CancellationToken token)
    {
        float height = trajectory.Height.PickRandom();
        float duration = trajectory.PickDuration(Vector3.Distance(start, end));
        float flightSpeed = 0f < duration ? Vector3.Distance(start, end) / duration : 0f;

        BeginContact(target);

        _homing = shot.Homing;
        _maxHomingSpeed = flightSpeed * _homing;
        _homingOffset = Vector3.zero;
        _velocity = 0f < flightSpeed ? (end - start).normalized * flightSpeed : Vector3.zero;

        if (null != target)
        {
            _targetStartPosition = target.FollowTransform.position;
        }

        _flight = CreateFlightTween(trajectory.Type, start, end, height, duration)
            .SetEase(Ease.Linear)
            .OnUpdate(_updateFlight);

        // 전투가 끝나 토큰이 취소되면 트윈을 멈추고, 예외를 던지는 대신 isCanceled를 true로 돌려받는다.
        bool isCanceled = await _flight.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token).SuppressCancellationThrow();

        Vector3 landingPoint = FlightOutcome.Missed != ContactOutcome ? ContactPoint : CachedTransform.position;
        FlightOutcome outcome = GetOutcome(isCanceled, landingPoint);

        // 도착 지점에서 비켜난 목표가 탄이 지나갈 길 위에 있을 수 있어, 지나가는 동안에도 닿았는지 본다.
        if (FlightOutcome.Missed == outcome)
        {
            FlightResult pastResult = await FlyPastAsync(shot.MissTravelTime, token);
            outcome = pastResult.Outcome;

            if (FlightOutcome.Missed != outcome)
            {
                landingPoint = pastResult.LandingPoint;
            }
        }

        _flight = null;
        _pass = null;
        EndContact();

        return new FlightResult(outcome, landingPoint);
    }

    // 보정이 따라가는 속도를 탄 속도 × 추적 강도로 묶어, 목표가 갑자기 움직여도 탄이 홱 꺾이지 않게 한다.
    // 도착 지점만 보면 방패를 가로질러 지나가도 막히지 않아, 날아오는 동안 매 프레임 방패에 닿았는지 본다.
    private void UpdateFlight()
    {
        float slowScale = GetSlowScale();
        _flight.timeScale = slowScale;

        if (null == Target)
        {
            return;
        }

        Vector3 desiredOffset = (Target.FollowTransform.position - _targetStartPosition) * (_flight.ElapsedPercentage() * _homing);
        _homingOffset = Vector3.MoveTowards(_homingOffset, desiredOffset, _maxHomingSpeed * Time.deltaTime * slowScale);
        CachedTransform.position += _homingOffset;

        // 빗나간 뒤 같은 속도로 이어 가도록 실제 속도를 기억한다. 마지막 프레임은 덜 움직여 넘긴다.
        // 느려지는 구역 안에서 잰 속도는 배율로 되돌려, 구역을 벗어난 뒤에는 원래 속도로 날아가게 한다.
        if (0f < Time.deltaTime && 0f < slowScale && 1f > _flight.ElapsedPercentage())
        {
            _velocity = (CachedTransform.position - LastPosition) / (Time.deltaTime * slowScale);
        }

        if (CheckContact())
        {
            _flight.Kill();
        }
    }

    // 외곽선 같은 자식까지 함께 사라지도록 색 대신 크기를 줄인다.
    private async UniTask<FlightResult> FlyPastAsync(float travelTime, CancellationToken token)
    {
        if (0f >= travelTime)
        {
            return new FlightResult(FlightOutcome.Missed, CachedTransform.position);
        }

        Vector3 destination = CachedTransform.position + _velocity * travelTime;

        ResetContactPath();

        _pass = DOTween.Sequence()
            .Join(CachedTransform.DOMove(destination, travelTime).SetEase(Ease.Linear))
            .Join(CachedTransform.DOScale(0f, travelTime).SetEase(Ease.InQuad))
            .OnUpdate(_updatePass);

        bool isCanceled = await _pass.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token).SuppressCancellationThrow();

        if (FlightOutcome.Missed != ContactOutcome)
        {
            return new FlightResult(ContactOutcome, ContactPoint);
        }

        return new FlightResult(isCanceled ? FlightOutcome.Canceled : FlightOutcome.Missed, CachedTransform.position);
    }

    private void UpdatePass()
    {
        _pass.timeScale = GetSlowScale();

        if (CheckContact())
        {
            _pass.Kill();
        }
    }

    // 방패나 몸에 닿아 직접 멈춘 경우는 트윈이 취소로 끝나더라도 닿은 것으로 본다.
    private FlightOutcome GetOutcome(bool isCanceled, Vector3 landingPoint)
    {
        if (FlightOutcome.Missed != ContactOutcome)
        {
            return ContactOutcome;
        }

        if (isCanceled)
        {
            return FlightOutcome.Canceled;
        }

        if (null != Target && false == Target.IsHit(landingPoint))
        {
            return FlightOutcome.Missed;
        }

        return FlightOutcome.Arrived;
    }

    // 경로 트윈은 탄마다 경로를 새로 만들어 탄막에서 할당이 몰리므로, 경유점이 없는 직선은 위치 트윈으로 날린다.
    private Tween CreateFlightTween(TrajectoryType type, Vector3 start, Vector3 end, float height, float duration)
    {
        if (TrajectoryType.Straight == type)
        {
            return CachedTransform.DOMove(end, duration);
        }

        return CachedTransform.DOPath(BuildPath(type, start, end, height), duration, PathType.CatmullRom);
    }

    // 경로의 시작점은 투사체의 현재 위치라서 경유점과 도착점만 넣는다.
    private static Vector3[] BuildPath(TrajectoryType type, Vector3 start, Vector3 end, float height)
    {
        switch (type)
        {
            case TrajectoryType.Straight:
                return new Vector3[] { end };

            case TrajectoryType.Wave:
                // 날아가는 방향에 수직인 방향으로 번갈아 비켜 가며 물결을 만든다.
                Vector3 normal = Vector3.Cross((end - start).normalized, Vector3.forward);

                return new Vector3[]
                {
                    Vector3.Lerp(start, end, 0.33f) + normal * height,
                    Vector3.Lerp(start, end, 0.66f) - normal * height,
                    end,
                };

            default:
                return new Vector3[]
                {
                    Vector3.Lerp(start, end, 0.5f) + Vector3.up * height,
                    end,
                };
        }
    }
}
