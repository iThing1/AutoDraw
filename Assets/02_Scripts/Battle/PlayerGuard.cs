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

    // HUD 칸에 표시할 키 이름. 입력 에셋에서 키를 바꾸면 따라 바뀐다.
    public string KeyLabel { get { return null != _guardAction ? _guardAction.action.GetBindingDisplayString(0) : string.Empty; } }

    private void Awake()
    {
        _unit = GetComponent<BattleUnit>();
        _skillGauge = GetComponent<SkillGauge>();

        if (null != _shield)
        {
            _shieldScale = _shield.transform.localScale;
            _shield.enabled = false;
        }

        if (null == _guardAction || null == _preset)
        {
            Debug.LogError($"플레이어 막기 : {name}에 막기 입력 또는 GuardPreset이 지정되지 않았습니다.");
        }
    }

    private void OnEnable()
    {
        _unit.OnGuarded += HandleGuarded;

        if (null != _guardAction)
        {
            _guardAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        _unit.OnGuarded -= HandleGuarded;
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

        if (_guardAction.action.WasPressedThisFrame())
        {
            TryGuard();
        }
    }

    // 직접 움직일 수 있을 때만 쓴다. 컷신(Idle)이나 다른 동작(Action) 중에는 나가지 않는다.
    private void TryGuard()
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
            using (_unit.BeginGuard())
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

    private void HandleGuarded(Vector3 hitPoint)
    {
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
