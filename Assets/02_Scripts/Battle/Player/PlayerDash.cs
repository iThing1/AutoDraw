using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

// 유틸리티 키를 누르면 이동 방향으로 짧게 미끄러지고, 그동안 공격이 몸을 지나간다.
[RequireComponent(typeof(BattleUnit), typeof(PlayerInputMover), typeof(Rigidbody2D))]
public class PlayerDash : PlayerUtility
{
    [SerializeField] private InputActionReference _dashAction;
    [SerializeField] private DashPreset _preset;

    private BattleUnit _unit;
    private PlayerInputMover _mover;
    private Rigidbody2D _rigidbody;
    // 피했을 때 채울 게이지. 게이지가 없는 유닛이면 null이다.
    private SkillGauge _skillGauge;
    private DashState _state;
    private PlayerStats _stats;

    // 유틸리티 칸에 대시를 끼우지 않았으면 null이다.
    public DashState State { get { return _state; } }

    public override UtilityType Type { get { return UtilityType.Dash; } }
    public override bool IsEquipped { get { return null != _state; } }
    public override string KeyLabel { get { return _dashAction.GetKeyLabel(); } }

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();
        _mover = GetComponent<PlayerInputMover>();
        _rigidbody = GetComponent<Rigidbody2D>();
        TryGetComponent(out _skillGauge);

        if (null == _dashAction || null == _preset)
        {
            Debug.LogError($"플레이어 대시 : {name}에 대시 입력 또는 DashPreset이 지정되지 않았습니다.");
            enabled = false;
        }
    }

    protected override void Equip(PlayerStats stats)
    {
        _stats = stats;
        _state = new DashState(_preset, stats);

        if (null != _preset.AfterimagePrefab)
        {
            GameManager.Pool.Prewarm(_preset.AfterimagePrefab, Mathf.CeilToInt(_preset.AfterimageFadeDuration / _preset.AfterimageInterval) + 1);
        }
    }

    private void OnEnable()
    {
        _unit.OnDodged += HandleDodged;

        _dashAction.Enable();
    }

    private void OnDisable()
    {
        _unit.OnDodged -= HandleDodged;
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
        CountUse();

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

        GameManager.Feedback.PlayImpact(_preset.StartImpact, _unit.Body.position);

        bool hasEnded = false;

        try
        {
            // 유예까지 한 번의 회피로 묶어, 회피 문구가 대시 한 번에 한 번만 뜨게 한다.
            using (_unit.BeginDodge())
            {
                while (elapsed < _preset.Duration)
                {
                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);

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

                EndDash(stroke);
                hasEnded = true;

                // 움직임은 먼저 돌려주고 잠깐 더 피하는 상태로 두어, 끝나는 순간 닿은 공격에 맞지 않게 한다.
                if (0f < _preset.DodgeGrace)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_preset.DodgeGrace), cancellationToken: token);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (false == hasEnded)
            {
                EndDash(stroke);
            }
        }
    }

    // 회피 알림은 대시 한 번에 한 번만 오므로, 여러 공격을 한꺼번에 피해도 한 번만 채운다.
    private void HandleDodged(Vector3 point)
    {
        if (null != _skillGauge)
        {
            _skillGauge.Add(_preset.DodgeGaugeGain);
        }
    }

    // 끊겨도 지나간 데까지는 남긴다.
    private void EndDash(PaintStripe stroke)
    {
        if (null != stroke)
        {
            stroke.Finish();
        }

        _unit.EndAction();
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
        SpriteRenderer afterimage = GameManager.Pool.SpawnSprite(_preset.AfterimagePrefab, visual.position, visual.eulerAngles.z, visual.lossyScale, _preset.AfterimageColor);
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
        }
        finally
        {
            GameManager.Pool.DespawnSprite(afterimage);
        }
    }
}
