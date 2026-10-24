using System;

// 끼운 소모품 하나의 남은 횟수와 쿨타임. 시간은 부르는 쪽이 넘긴다(게임 시간).
public class ConsumableState
{
    private readonly ConsumableData _data;
    private readonly float _value;
    private readonly float _cooldownDuration;
    private readonly Cooldown _cooldown = new();
    private int _charges;
    private float _cooldownRatio = 1f;

    public event Action OnUsed;
    public event Action OnChanged;

    public ConsumableData Data { get { return _data; } }
    // 효과의 세기(회복량, 지우는 반경 등).
    public float Value { get { return _value; } }
    public int Charges { get { return _charges; } }
    // 1이면 쿨타임이 끝났다.
    public float CooldownRatio { get { return _cooldownRatio; } }

    public ConsumableState(ConsumableData data, PlayerStats stats)
    {
        _data = data;
        _value = stats.GetConsumableValue(data);
        _cooldownDuration = stats.GetConsumableCooldown(data);
        _charges = stats.GetConsumableCharges(data);
    }

    public bool CanUse(float now)
    {
        return 0 < _charges && _cooldown.IsReady(now);
    }

    // CanUse일 때만 부른다.
    public void Use(float now)
    {
        _charges--;
        _cooldown.Start(now, _cooldownDuration);
        _cooldownRatio = _cooldown.GetRatio(now);

        OnUsed?.Invoke();
        OnChanged?.Invoke();
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
