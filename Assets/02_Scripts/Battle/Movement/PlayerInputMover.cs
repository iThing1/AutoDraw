using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

// 입력 방향으로 플레이어를 움직인다. 키 배치는 입력 에셋(InputSystem_Actions)에서 바꾼다.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerInputMover : UnitMover
{
    [SerializeField] private InputActionReference _moveAction;

    private Rigidbody2D _rigidbody;
    private PlayerStats _stats;

    protected override void Awake()
    {
        base.Awake();

        _rigidbody = GetComponent<Rigidbody2D>();
    }

    // 전투를 시작하는 쪽이 플레이어를 만든 직후 부른다. 이동 속도는 특성으로 바뀔 수 있어 스탯에서 매번 읽는다.
    public void Setup(PlayerStats stats)
    {
        _stats = stats;
    }

    protected override void OnRunStarted()
    {
        if (null == _moveAction)
        {
            Debug.LogError($"플레이어 이동 : {name}에 이동 입력이 연결되지 않았습니다.");
            return;
        }

        if (null == _stats)
        {
            Debug.LogError($"플레이어 이동 : {name}에 스탯이 넘어오지 않았습니다. PlayerSetup.Setup을 먼저 불러야 합니다.");
        }

        _moveAction.Enable();
    }

    // 보스 등장 트리거 같은 충돌이 감지되도록 물리 프레임마다 Rigidbody로 옮긴다.
    // 화면 프레임보다 물리 프레임이 잦을 때 걸음이 빠지지 않도록, Move인 동안은 물리 프레임 안에서 계속 돈다.
    protected override async UniTask MoveOnceAsync(CancellationToken token)
    {
        if (null == _moveAction || null == _stats)
        {
            return;
        }

        while (UnitState.Move == Unit.State)
        {
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);

            // 기다리는 사이 막기나 컷신으로 상태가 바뀌었으면 한 걸음 더 가지 않고 멈춘다.
            if (UnitState.Move != Unit.State)
            {
                return;
            }

            Vector2 input = Vector2.ClampMagnitude(_moveAction.action.ReadValue<Vector2>(), 1f);
            Vector2 velocity = (input * _stats.MoveSpeed + ExternalVelocity) * SlowZone.GetMoveScale(_rigidbody.position);
            Vector2 step = velocity * Time.fixedDeltaTime;

            SetVelocity(velocity);

            _rigidbody.MovePosition(ClampToMoveArea(_rigidbody.position + step));
        }
    }

    // 매 물리 프레임마다 상태를 확인해 멈추므로 따로 끊을 이동은 없고, 예측 사격이 멈춘 자리를 노리게 속도만 지운다.
    protected override void StopMoving()
    {
        SetVelocity(Vector2.zero);
    }
}
