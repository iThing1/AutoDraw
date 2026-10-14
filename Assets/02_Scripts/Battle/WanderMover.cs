using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 처음 자리 주변을 배회하다가, 목표가 너무 멀어지면 다가가 그 자리를 새 중심으로 삼는다.
// 게임 시간으로 움직여 히트 스톱 동안 함께 멈춘다.
public class WanderMover : UnitMover
{
    // 범위가 최소 이동 거리보다 좁을 때 끝없이 다시 고르지 않도록 시도 횟수를 제한한다.
    private const int MAX_PICK_ATTEMPTS = 10;
    private const float MIN_SPEED = 0.01f;

    private Vector3 _center;
    private Tween _moveTween;

    // 착지 지점에서 전투를 시작하므로 이때 위치를 배회 중심으로 쓴다.
    protected override void OnRunStarted()
    {
        _center = transform.position;
    }

    protected override async UniTask MoveOnceAsync(CancellationToken token)
    {
        WanderSettings settings = Unit.Data.Wander;

        if (false == settings.UseWander)
        {
            return;
        }

        Vector3 destination;
        float speed;

        if (IsTargetTooFar(settings))
        {
            // 다가간 자리를 새 배회 중심으로 삼아, 다가간 뒤 원래 자리로 되돌아가지 않게 한다.
            destination = ClampToStage(Unit.Attacker.Target.GetApproachPoint(transform.position, settings.ChaseStopDistance));
            _center = destination;
            speed = settings.Speed.Max;
        }
        else
        {
            destination = PickDestination(settings);
            speed = settings.Speed.PickRandom();
        }

        float distance = Vector3.Distance(transform.position, destination);
        float duration = distance / Mathf.Max(MIN_SPEED, speed);

        _moveTween = transform.DOMove(destination, duration).SetEase(settings.MoveEase);

        // 이동이 끊겨도 예외 대신 결과로 받고, 전투가 끝났을 때만 멈춘다.
        await _moveTween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token).SuppressCancellationThrow();

        token.ThrowIfCancellationRequested();

        if (UnitState.Move != Unit.State)
        {
            return;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(settings.Pause.PickRandom()), cancellationToken: token);
    }

    protected override void StopMoving()
    {
        _moveTween?.Kill();
    }

    private bool IsTargetTooFar(WanderSettings settings)
    {
        AttackTarget target = Unit.Attacker.Target;

        if (null == target || 0f >= settings.ChaseDistance)
        {
            return false;
        }

        return Vector3.Distance(transform.position, target.Unit.transform.position) > settings.ChaseDistance;
    }

    private Vector3 ClampToStage(Vector3 position)
    {
        Vector2 clamped = ClampToMoveArea(position);

        return new Vector3(clamped.x, clamped.y, position.z);
    }

    // 지금 위치에서 너무 가까운 지점은 움직임이 거의 보이지 않으므로, 최소 거리 이상 떨어진 지점을 고른다.
    private Vector3 PickDestination(WanderSettings settings)
    {
        Vector2 halfSize = settings.AreaHalfSize;
        Vector3 destination = transform.position;

        for (int i = 0; i < MAX_PICK_ATTEMPTS; i++)
        {
            float x = UnityEngine.Random.Range(-halfSize.x, halfSize.x);
            float y = UnityEngine.Random.Range(-halfSize.y, halfSize.y);

            // 배회 중심이 가장자리 가까이 옮겨 가면 범위가 종이 밖으로 걸칠 수 있어, 고른 지점을 이동 범위 안으로 자른다.
            destination = ClampToStage(_center + new Vector3(x, y, 0f));

            if (Vector3.Distance(transform.position, destination) >= settings.MinMoveDistance)
            {
                return destination;
            }
        }

        return destination;
    }
}
