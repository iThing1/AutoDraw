using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

// 막기 키를 누르는 순간 짧게 방패를 세운다. 그동안 닿은 공격은 피해 없이 막히고, 스킬 게이지를 조금 쓴다.
// 쓸 수 있는지(쿨타임·게이지)는 GuardState가 정하고, 여기는 입력을 받고 방패를 보여 주는 일만 한다.
[RequireComponent(typeof(BattleUnit), typeof(SkillGauge))]
public class PlayerGuard : MonoBehaviour
{
    [SerializeField] private InputActionReference _guardAction;
    [SerializeField] private GuardPreset _preset;
    [SerializeField] private SpriteRenderer _shield;

    private BattleUnit _unit;
    private GuardState _state;
    private PlayerStats _stats;
    private Vector3 _shieldScale;
    private Color _shieldColor;

    // 프리셋이나 입력이 비어 있으면 null이다.
    public GuardState State { get { return _state; } }

    public string KeyLabel { get { return _guardAction.GetKeyLabel(); } }

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();

        if (null != _shield)
        {
            _shield.enabled = false;
        }

        if (null == _guardAction || null == _preset)
        {
            Debug.LogError($"플레이어 막기 : {name}에 막기 입력 또는 GuardPreset이 지정되지 않았습니다.");
            enabled = false;
        }
    }

    // 전투를 시작하는 쪽이 플레이어를 만든 직후 부른다. 유틸리티 칸에 막기를 끼웠을 때만 쓴다.
    // 꺼 두면 입력도 받지 않고, State가 null이라 HUD도 막기 칸을 숨긴다.
    public void Setup(UtilityType utility, PlayerStats stats)
    {
        if (false == enabled || UtilityType.Guard != utility)
        {
            enabled = false;
            return;
        }

        _stats = stats;
        _state = new GuardState(_preset, _stats, GetComponent<SkillGauge>());

        if (null != _shield && null != _shield.sprite)
        {
            // 방패 그림 지름을 막기 반경의 두 배로 맞춰, 보이는 방패와 막히는 범위를 같게 한다.
            float spriteDiameter = _shield.sprite.bounds.size.x;
            _shieldScale = Vector3.one * (_stats.GuardRadius * 2f / spriteDiameter);
            _shield.transform.localScale = _shieldScale;
            _shieldColor = _shield.color;
        }
    }

    private void OnEnable()
    {
        _unit.OnBlocked += HandleBlocked;

        _guardAction.Enable();
    }

    private void OnDisable()
    {
        _unit.OnBlocked -= HandleBlocked;
    }

    private void OnDestroy()
    {
        if (null != _state)
        {
            _state.Dispose();
        }

        if (null != _shield)
        {
            _shield.transform.DOKill();
            _shield.DOKill();
        }
    }

    private void Update()
    {
        if (null == _state)
        {
            return;
        }

        if (_guardAction.WasPressedThisFrame())
        {
            TryStartGuard();
        }
    }

    // 직접 움직일 수 있을 때만 쓴다. 컷신(Idle)이나 다른 동작(Action, 막는 중 포함) 중에는 조용히 넘긴다.
    private void TryStartGuard()
    {
        if (UnitState.Move != _unit.State)
        {
            return;
        }

        GuardStartResult result = _state.CheckStart(Time.time);

        if (GuardStartResult.Ready != result)
        {
            _state.Reject(result);
            FlashRejected();
            return;
        }

        if (false == _unit.TryBeginAction())
        {
            return;
        }

        _state.Begin(Time.time);

        GuardAsync(destroyCancellationToken).Forget();
    }

    // 게임 시간으로 재서, 히트 스톱으로 공격이 멈춘 동안에는 방패도 함께 기다린다.
    private async UniTaskVoid GuardAsync(CancellationToken token)
    {
        ShowShield();

        try
        {
            using (_unit.BeginGuard(_stats.GuardRadius))
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_stats.GuardDuration), cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            // 플레이어가 사라져 막기가 끊긴 경우라 따로 처리하지 않는다.
        }
        finally
        {
            HideShield();
            _unit.EndAction();
        }
    }

    // 눌렀는데 막기가 안 나가면 방패를 회색으로 잠깐 비춰, 막을 수 있었는데 안 막힌 것처럼 보이지 않게 한다.
    private void FlashRejected()
    {
        if (null == _shield)
        {
            return;
        }

        _shield.transform.DOKill();
        _shield.DOKill();
        _shield.transform.localScale = _shieldScale;
        _shield.color = _preset.RejectColor;
        _shield.enabled = true;
        _shield.DOFade(0f, _preset.RejectFlashDuration).OnComplete(HideShield);
    }

    // 짧은 히트 스톱으로 막는 순간의 손맛을 준다.
    private void HandleBlocked(Vector3 hitPoint)
    {
        _state.ResetCooldown();

        GameManager.TimeScale.PlayHitStop(_preset.BlockHitStopDuration, _preset.BlockHitStopTimeScale);
        ImpactPlayer.Play(_preset.BlockImpact, hitPoint);

        if (null == _shield)
        {
            return;
        }

        _shield.transform.DOComplete();
        _shield.transform.DOPunchScale(_shieldScale * _preset.ShieldPunch, _preset.ShieldPunchDuration, PunchTween.VIBRATO, PunchTween.ELASTICITY);
    }

    private void ShowShield()
    {
        if (null == _shield)
        {
            return;
        }

        Transform shieldTransform = _shield.transform;

        _shield.DOKill();
        _shield.color = _shieldColor;
        _shield.enabled = true;
        shieldTransform.DOKill();
        shieldTransform.localScale = Vector3.zero;
        shieldTransform.DOScale(_shieldScale, _preset.ShieldAppearDuration).SetEase(_preset.ShieldAppearEase);
    }

    private void HideShield()
    {
        if (null == _shield)
        {
            return;
        }

        _shield.transform.DOKill();
        _shield.DOKill();
        _shield.transform.localScale = _shieldScale;
        _shield.color = _shieldColor;
        _shield.enabled = false;
    }
}
