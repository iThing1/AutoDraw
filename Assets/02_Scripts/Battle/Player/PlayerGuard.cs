using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

// 막기 키를 누르는 순간 짧게 방패를 세운다. 그동안 닿은 공격은 피해 없이 막히고, 스킬 게이지를 조금 쓴다.
[RequireComponent(typeof(BattleUnit), typeof(SkillGauge))]
public class PlayerGuard : MonoBehaviour
{
    [SerializeField] private InputActionReference _guardAction;
    [SerializeField] private GuardPreset _preset;
    [SerializeField] private SpriteRenderer _shield;

    public event Action OnGuardStarted;

    private BattleUnit _unit;
    private SkillGauge _skillGauge;
    private Vector3 _shieldScale;
    private float _readyTime;

    public GuardPreset Preset { get { return _preset; } }
    public SkillGauge SkillGauge { get { return _skillGauge; } }

    public string KeyLabel { get { return _guardAction.GetKeyLabel(); } }

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();
        _skillGauge = GetComponent<SkillGauge>();

        if (null == _guardAction || null == _preset)
        {
            Debug.LogError($"플레이어 막기 : {name}에 막기 입력 또는 GuardPreset이 지정되지 않았습니다.");
            return;
        }

        if (null != _shield && null != _shield.sprite)
        {
            // 방패 그림 지름을 막기 반경의 두 배로 맞춰, 보이는 방패와 막히는 범위를 같게 한다.
            float spriteDiameter = _shield.sprite.bounds.size.x;
            _shieldScale = Vector3.one * (_preset.GuardRadius * 2f / spriteDiameter);
            _shield.transform.localScale = _shieldScale;
            _shield.enabled = false;
        }
    }

    private void OnEnable()
    {
        _unit.OnBlocked += HandleBlocked;

        if (null != _guardAction)
        {
            _guardAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        _unit.OnBlocked -= HandleBlocked;
    }

    private void OnDestroy()
    {
        if (null != _shield)
        {
            _shield.transform.DOKill();
        }
    }

    private void Update()
    {
        if (null == _guardAction || null == _preset)
        {
            return;
        }

        if (_guardAction.WasPressedThisFrame())
        {
            TryStartGuard();
        }
    }

    // 직접 움직일 수 있을 때만 쓴다. 컷신(Idle)이나 다른 동작(Action) 중에는 나가지 않는다.
    private void TryStartGuard()
    {
        if (UnitState.Move != _unit.State || Time.time < _readyTime || false == _skillGauge.CanConsume(_preset.GaugeCost))
        {
            return;
        }

        if (false == _unit.TryBeginAction())
        {
            return;
        }

        _skillGauge.TryConsume(_preset.GaugeCost);
        _readyTime = Time.time + _preset.GuardDuration + _preset.Cooldown;

        GuardAsync(destroyCancellationToken).Forget();
    }

    // 게임 시간으로 재서, 히트 스톱으로 공격이 멈춘 동안에는 방패도 함께 기다린다.
    private async UniTaskVoid GuardAsync(CancellationToken token)
    {
        ShowShield();
        OnGuardStarted?.Invoke();

        try
        {
            using (_unit.BeginGuard(_preset.GuardRadius))
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_preset.GuardDuration), cancellationToken: token);
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

    // 짧은 히트 스톱으로 막는 순간의 손맛을 준다.
    private void HandleBlocked(Vector3 hitPoint)
    {
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
        _shield.transform.localScale = _shieldScale;
        _shield.enabled = false;
    }
}
