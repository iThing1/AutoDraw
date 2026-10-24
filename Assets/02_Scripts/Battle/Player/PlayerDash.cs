using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

// 유틸리티 키를 누르면 이동 방향으로 짧게 미끄러지고, 그동안 공격이 몸을 지나간다.
// 쓸 수 있는지(쿨타임)는 DashState가 정하고, 여기는 입력을 받아 움직이는 일만 한다.
[RequireComponent(typeof(BattleUnit), typeof(PlayerInputMover), typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [SerializeField] private InputActionReference _dashAction;
    [SerializeField] private DashPreset _preset;

    private BattleUnit _unit;
    private PlayerInputMover _mover;
    private Rigidbody2D _rigidbody;
    private DashState _state;
    private PlayerStats _stats;
    private Sprite _icon;

    // 유틸리티 칸에 대시를 끼우지 않았으면 null이다.
    public DashState State { get { return _state; } }

    public string KeyLabel { get { return _dashAction.GetKeyLabel(); } }
    public Sprite Icon { get { return _icon; } }

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();
        _mover = GetComponent<PlayerInputMover>();
        _rigidbody = GetComponent<Rigidbody2D>();

        if (null == _dashAction || null == _preset)
        {
            Debug.LogError($"플레이어 대시 : {name}에 대시 입력 또는 DashPreset이 지정되지 않았습니다.");
            enabled = false;
        }
    }

    // 꺼 두면 입력도 받지 않고, State가 null이라 HUD도 대시 칸을 그리지 않는다.
    public void Setup(UtilityType utility, PlayerStats stats, AssetManager assets)
    {
        if (false == enabled || UtilityType.Dash != utility)
        {
            enabled = false;
            return;
        }

        _stats = stats;
        _state = new DashState(_preset, stats);
        _icon = assets.Get<Sprite>(utility.GetIconKey());

        if (null != _preset.AfterimagePrefab)
        {
            GameManager.Pool.Prewarm(_preset.AfterimagePrefab, Mathf.CeilToInt(_preset.AfterimageFadeDuration / _preset.AfterimageInterval) + 1);
        }
    }

    private void OnEnable()
    {
        _dashAction.Enable();
    }

    private void Update()
    {
        if (null == _state)
        {
            return;
        }

        _state.Tick(Time.time);

        if (_dashAction.WasPressedThisFrame())
        {
            TryStartDash();
        }
    }

    private void TryStartDash()
    {
        if (UnitState.Move != _unit.State)
        {
            return;
        }

        if (false == _state.CanStart(Time.time))
        {
            _state.Reject();
            return;
        }

        if (false == _unit.TryBeginAction())
        {
            return;
        }

        _state.Begin(Time.time);

        DashAsync(_mover.GetFacingDirection(), destroyCancellationToken).Forget();
    }

    // 물리 프레임마다 옮겨 등장 트리거 같은 충돌이 그대로 감지되고, 히트 스톱 동안에는 함께 멈춘다.
    // 느려지는 구역과 끌어당기는 힘은 받지 않아 빠져나가는 데 쓸 수 있다.
    private async UniTaskVoid DashAsync(Vector2 direction, CancellationToken token)
    {
        Vector2 start = _rigidbody.position;
        Vector2 end = _mover.ClampPosition(start + direction * _stats.DashDistance);
        float elapsed = 0f;
        float nextAfterimageTime = 0f;
        PaintStripe stroke = BeginStroke(start, end);

        ImpactPlayer.Play(_preset.StartImpact, _unit.Body.position);

        try
        {
            using (_unit.BeginDodge())
            {
                while (elapsed < _preset.Duration)
                {
                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);

                    // 컷신이 상태를 바꿨으면 그 자리에서 멈춘다.
                    if (UnitState.Action != _unit.State)
                    {
                        break;
                    }

                    elapsed += Time.fixedDeltaTime;

                    float progress = DOVirtual.EasedValue(0f, 1f, Mathf.Clamp01(elapsed / _preset.Duration), _preset.Ease);
                    Vector2 position = Vector2.Lerp(start, end, progress);
                    _rigidbody.MovePosition(position);

                    if (null != stroke)
                    {
                        stroke.SetLength(Vector2.Distance(start, position));
                    }

                    if (elapsed >= nextAfterimageTime)
                    {
                        SpawnAfterimage(token);
                        nextAfterimageTime += _preset.AfterimageInterval;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 플레이어가 사라져 대시가 끊긴 경우라 따로 처리하지 않는다.
        }
        finally
        {
            // 끊겨도 지나간 데까지는 남긴다.
            if (null != stroke)
            {
                stroke.Finish();
            }

            _unit.EndAction();
        }
    }

    // 전투가 시작되기 전(다가가는 중)에는 캔버스를 받기 전이라 자국을 남기지 않는다.
    private PaintStripe BeginStroke(Vector2 start, Vector2 end)
    {
        AttackContext context = _unit.Attacker.Context;

        if (null == context || null == _preset.StrokeTexture || start == end)
        {
            return null;
        }

        return context.PaintCanvas.BeginStripe(start, (end - start).normalized, _preset.StrokeWidth, _preset.StrokeTexture, _preset.StrokeTileLength,
            context.Damage.PickColor(), context.Damage.IsInk);
    }

    private void SpawnAfterimage(CancellationToken token)
    {
        if (null == _preset.AfterimagePrefab)
        {
            return;
        }

        // 지금 보이는 몸 그림(애니메이션으로 기울고 늘어난 모습 그대로)을 잔상으로 남긴다.
        SpriteRenderer body = _unit.BodyRenderer;

        if (null == body)
        {
            return;
        }

        Transform visual = body.transform;
        SpriteRenderer afterimage = PooledSprite.Spawn(_preset.AfterimagePrefab, visual.position, visual.eulerAngles.z, visual.lossyScale, _preset.AfterimageColor);
        afterimage.sprite = body.sprite;
        afterimage.flipX = body.flipX;

        FadeAfterimageAsync(afterimage, token).Forget();
    }

    private async UniTaskVoid FadeAfterimageAsync(SpriteRenderer afterimage, CancellationToken token)
    {
        try
        {
            await afterimage.DOFade(0f, _preset.AfterimageFadeDuration).ToUniTaskKillOnCancel(token);
        }
        catch (OperationCanceledException)
        {
            // 플레이어가 사라져 끊긴 경우라 따로 처리하지 않는다.
        }
        finally
        {
            PooledSprite.Despawn(afterimage);
        }
    }
}
