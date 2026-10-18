using System;
using UnityEngine;

// 막기의 상태와 규칙. 쿨타임, 게이지 비용, 지금 막기를 쓸 수 있는지를 정한다.
// 입력과 방패 연출은 PlayerGuard가 맡고, 화면은 이 상태만 보고 막기 칸을 그린다. 시간은 부르는 쪽이 넘긴다(게임 시간).
public class GuardState : IDisposable
{
    private readonly GuardPreset _preset;
    private readonly SkillGauge _gauge;
    private float _readyTime;

    public event Action OnStarted;
    public event Action<GuardStartResult> OnRejected;
    // 게이지가 바뀌어 막기 비용을 채웠는지가 달라질 수 있을 때.
    public event Action OnReadinessChanged;

    public GuardPreset Preset { get { return _preset; } }
    public bool HasEnoughGauge { get { return _gauge.CanConsume(_preset.GaugeCost); } }
    // 막기 비용 대비 지금 게이지. 1이면 막기를 쓸 수 있다.
    public float GaugeReadyRatio
    {
        get
        {
            float cost = _preset.GaugeCost;

            return 0f < cost ? Mathf.Clamp01(_gauge.Current / cost) : 1f;
        }
    }

    public GuardState(GuardPreset preset, SkillGauge gauge)
    {
        _preset = preset;
        _gauge = gauge;
        _gauge.OnChanged += HandleGaugeChanged;
    }

    public void Dispose()
    {
        _gauge.OnChanged -= HandleGaugeChanged;
    }

    // 게이지 부족이 먼저다. 쿨타임 중이어도 게이지가 모자라면 게이지 부족으로 알려, 왜 못 막는지 화면에 띄우게 한다.
    public GuardStartResult CheckStart(float now)
    {
        if (false == HasEnoughGauge)
        {
            return GuardStartResult.OutOfGauge;
        }

        return now < _readyTime ? GuardStartResult.OnCooldown : GuardStartResult.Ready;
    }

    // CheckStart가 Ready일 때만 부른다. 게이지를 쓰고, 막는 시간과 쿨타임이 지나야 다시 쓸 수 있다.
    public void Begin(float now)
    {
        _gauge.TryConsume(_preset.GaugeCost);
        _readyTime = now + _preset.GuardDuration + _preset.Cooldown;

        OnStarted?.Invoke();
    }

    public void Reject(GuardStartResult reason)
    {
        OnRejected?.Invoke(reason);
    }

    // 막아 냈으면 쿨타임을 지워, 지금 방패가 내려가는 즉시 다음 막기를 쓸 수 있게 한다. 연달아 오는 공격을 막기로 받아치는 보상이다.
    public void ResetCooldown()
    {
        _readyTime = 0f;
    }

    private void HandleGaugeChanged()
    {
        OnReadinessChanged?.Invoke();
    }
}
