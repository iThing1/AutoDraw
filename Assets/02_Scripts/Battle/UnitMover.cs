using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 상태가 Move일 때만 움직인다. 새 이동 방식은 MoveOnceAsync와 StopMoving만 구현하면 된다.
[RequireComponent(typeof(BattleUnit))]
public abstract class UnitMover : MonoBehaviour
{
    private BattleUnit _unit;

    protected BattleUnit Unit { get { return _unit; } }

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
