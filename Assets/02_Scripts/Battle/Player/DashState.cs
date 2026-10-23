using System;

// 대시의 쿨타임. 입력과 움직임은 PlayerDash가 맡고, 화면은 이 상태만 보고 대시 칸을 그린다. 시간은 부르는 쪽이 넘긴다(게임 시간).
public class DashState
{
    private readonly DashPreset _preset;
    private readonly PlayerStats _stats;
    private readonly Cooldown _cooldown = new();
    private float _cooldownRatio = 1f;

    public event Action OnStarted;
    public event Action OnChanged;
    public event Action OnRejected;

    public DashPreset Preset { get { return _preset; } }
    // 1이면 쿨타임이 끝났다.
    public float CooldownRatio { get { return _cooldownRatio; } }

    public DashState(DashPreset preset, PlayerStats stats)
    {
        _preset = preset;
        _stats = stats;
    }

    public bool CanStart(float now)
    {
        return _cooldown.IsReady(now);
    }

    // 막기처럼 움직이는 시간이 끝난 뒤부터 쿨타임을 잰다.
    public void Begin(float now)
    {
        _cooldown.Start(now, _preset.Duration + _stats.DashCooldown);
        _cooldownRatio = _cooldown.GetRatio(now);

        OnStarted?.Invoke();
        OnChanged?.Invoke();
    }

    public void Reject()
    {
        OnRejected?.Invoke();
    }

    public void Tick(float now)
    {
        if (1f <= _cooldownRatio)
        {
            return;
        }

        _cooldownRatio = _cooldown.GetRatio(now);
        OnChanged?.Invoke();
    }
}
