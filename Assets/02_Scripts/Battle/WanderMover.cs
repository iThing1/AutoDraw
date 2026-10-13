using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(BattleUnit))]
public class WanderMover : MonoBehaviour
{
    // 범위가 최소 이동 거리보다 좁을 때 끝없이 다시 고르지 않도록 시도 횟수를 제한한다.
    private const int MAX_PICK_ATTEMPTS = 10;
    private const float MIN_SPEED = 0.01f;

    private BattleUnit _unit;

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();
    }

    // 게임 시간으로 움직여 히트 스톱 동안에는 함께 멈춘다. 설정은 매번 읽어 플레이 중에 바꾼 값이 다음 이동부터 반영된다.
    public async UniTask RunAsync(Vector3 center, CancellationToken token)
    {
        try
        {
            while (false == _unit.Health.IsDead)
            {
                WanderSettings settings = _unit.Data.Wander;

                if (false == settings.UseWander)
                {
                    return;
                }

                Vector3 destination = PickDestination(center, settings);
                float distance = Vector3.Distance(transform.position, destination);
                float duration = distance / Mathf.Max(MIN_SPEED, settings.Speed.Pick());

                await transform.DOMove(destination, duration)
                    .SetEase(settings.MoveEase)
                    .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

                await UniTask.Delay(TimeSpan.FromSeconds(settings.Pause.Pick()), cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            // 전투가 끝나 이동이 멈춘 경우라 따로 처리하지 않는다.
        }
    }

    // 지금 위치에서 너무 가까운 지점은 움직임이 거의 보이지 않으므로, 최소 거리 이상 떨어진 지점을 고른다.
    private Vector3 PickDestination(Vector3 center, WanderSettings settings)
    {
        Vector2 halfSize = settings.AreaHalfSize;
        Vector3 destination = transform.position;

        for (int i = 0; i < MAX_PICK_ATTEMPTS; i++)
        {
            float x = UnityEngine.Random.Range(-halfSize.x, halfSize.x);
            float y = UnityEngine.Random.Range(-halfSize.y, halfSize.y);

            destination = center + new Vector3(x, y, 0f);

            if (Vector3.Distance(transform.position, destination) >= settings.MinMoveDistance)
            {
                return destination;
            }
        }

        return destination;
    }
}
