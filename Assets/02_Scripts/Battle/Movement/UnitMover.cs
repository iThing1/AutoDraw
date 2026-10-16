using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 상태가 Move일 때만 움직인다. 새 이동 방식은 MoveOnceAsync와 StopMoving만 구현하면 된다.
[RequireComponent(typeof(BattleUnit))]
public abstract class UnitMover : MonoBehaviour
{
    private BattleUnit _unit;

    private bool _hasMoveArea;
    private Rect _moveArea;
    // 예측 사격용 지금 속도. 직접 조작하는 이동만 채우고, 나머지는 0이다.
    private Vector2 _velocity;
    // 소용돌이처럼 바깥에서 끌거나 미는 힘(초당 거리). 직접 조작하는 이동에 더해진다.
    private Vector2 _externalVelocity;

    protected BattleUnit Unit { get { return _unit; } }
    protected Vector2 ExternalVelocity { get { return _externalVelocity; } }

    // 지금 속도 그대로 seconds초 뒤에 있을 자리. 이동 범위 밖은 범위 끝으로 자른다.
    public Vector3 PredictPosition(float seconds)
    {
        Vector2 predicted = ClampToMoveArea((Vector2)transform.position + _velocity * seconds);

        return new Vector3(predicted.x, predicted.y, transform.position.z);
    }

    public void SetExternalVelocity(Vector2 velocity)
    {
        _externalVelocity = velocity;
    }

    // 순간이동처럼 직접 옮길 자리를 이동 범위 안으로 자른다.
    public Vector3 ClampPosition(Vector3 position)
    {
        Vector2 clamped = ClampToMoveArea(position);

        return new Vector3(clamped.x, clamped.y, position.z);
    }

    protected void SetVelocity(Vector2 velocity)
    {
        _velocity = velocity;
    }

    protected virtual void Awake()
    {
        _unit = GetComponent<BattleUnit>();
    }

    private void OnEnable()
    {
        _unit.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        _unit.OnStateChanged -= HandleStateChanged;
    }

    // 스테이지가 정해 준 범위. 지정하지 않으면 제한 없이 움직인다.
    public void SetMoveArea(Rect area)
    {
        _moveArea = area;
        _hasMoveArea = true;
    }

    public async UniTask RunAsync(CancellationToken token)
    {
        OnRunStarted();

        try
        {
            while (UnitState.Dead != _unit.State)
            {
                // 이동이 바로 끝나도 같은 프레임에서 무한 반복되지 않게 한 프레임 쉰다.
                await UniTask.Yield(PlayerLoopTiming.Update, token);

                if (UnitState.Move != _unit.State)
                {
                    continue;
                }

                await MoveOnceAsync(token);
            }
        }
        catch (OperationCanceledException)
        {
            // 전투가 끝나 이동이 멈춘 경우라 따로 처리하지 않는다.
        }
    }

    protected virtual void OnRunStarted()
    {
    }

    protected Vector2 ClampToMoveArea(Vector2 position)
    {
        if (false == _hasMoveArea)
        {
            return position;
        }

        float x = Mathf.Clamp(position.x, _moveArea.xMin, _moveArea.xMax);
        float y = Mathf.Clamp(position.y, _moveArea.yMin, _moveArea.yMax);

        return new Vector2(x, y);
    }

    protected abstract UniTask MoveOnceAsync(CancellationToken token);

    // 패턴과 위치를 동시에 움직이지 않도록 진행 중인 이동을 끊는다.
    protected abstract void StopMoving();

    private void HandleStateChanged(UnitState state)
    {
        if (UnitState.Move != state)
        {
            StopMoving();
        }
    }
}
