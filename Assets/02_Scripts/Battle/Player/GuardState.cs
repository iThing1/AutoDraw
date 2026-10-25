using System;
using UnityEngine;

// 막기의 상태와 규칙. 쿨타임, 게이지 비용, 지금 막기를 쓸 수 있는지를 정한다. 시간은 부르는 쪽이 넘긴다(게임 시간).
public class GuardState : IDisposable
{
    private readonly GuardPreset _preset;
    private readonly PlayerStats _stats;
    private readonly SkillGauge _gauge;
    private readonly Cooldown _cooldown = new();

    public event Action OnStarted;
    public event Action<GuardStartResult> OnRejected;
    // 게이지가 바뀌어 막기 비용을 채웠는지가 달라질 수 있을 때.
    public event Action OnReadinessChanged;

    public GuardPreset Preset { get { return _preset; } }
    public bool HasEnoughGauge { get { return _gauge.CanConsume(_stats.GuardCost); } }
    // 막기 비용 대비 지금 게이지. 1이면 막기를 쓸 수 있다.
    public float GaugeReadyRatio
    {
        get
        {
            float cost = _stats.GuardCost;

            return 0f < cost ? Mathf.Clamp01(_gauge.Current / cost) : 1f;
        }
    }

    public GuardState(GuardPreset preset, PlayerStats stats, SkillGauge gauge)
    {
        _preset = preset;
        _stats = stats;
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

        return _cooldown.IsReady(now) ? GuardStartResult.Ready : GuardStartResult.OnCooldown;
    }

    // CheckStart가 Ready일 때만 부른다. 게이지를 쓰고, 막는 시간과 쿨타임이 지나야 다시 쓸 수 있다.
    public void Begin(float now)
    {
        _gauge.TryConsume(_stats.GuardCost);
        _cooldown.Start(now, _stats.GuardDuration + _stats.GuardCooldown);

        OnStarted?.Invoke();
    }

    public void Reject(GuardStartResult reason)
    {
        OnRejected?.Invoke(reason);
    }

    // 막아 냈으면 쿨타임을 지워, 지금 방패가 내려가는 즉시 다음 막기를 쓸 수 있게 한다. 연달아 오는 공격을 막기로 받아치는 보상이다.
    public void ResetCooldown()
    {
        _cooldown.Clear();
    }

    private void HandleGaugeChanged()
    {
        OnReadinessChanged?.Invoke();
    }
}
