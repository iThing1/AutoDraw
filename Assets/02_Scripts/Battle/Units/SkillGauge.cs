using System;
using UnityEngine;

public class SkillGauge : MonoBehaviour
{
    public event Action OnChanged;

    private float _current;
    private float _max = 1f;

    public float Current { get { return _current; } }
    public float Ratio { get { return _current / _max; } }
    public bool IsFull { get { return _current >= _max; } }

    public bool CanConsume(float amount)
    {
        return _current >= amount;
    }

    public bool TryConsume(float amount)
    {
        if (false == CanConsume(amount))
        {
            return false;
        }

        _current -= amount;

        OnChanged?.Invoke();
        return true;
    }

    public void Init(float max)
    {
        _max = Mathf.Max(1f, max);
        _current = 0f;

        OnChanged?.Invoke();
    }

    public void Add(float amount)
    {
        if (IsFull || 0f >= amount)
        {
            return;
        }

        _current = Mathf.Min(_max, _current + amount);

        OnChanged?.Invoke();
    }

    public bool TryConsumeAll()
    {
        if (false == IsFull)
        {
            return false;
        }

        _current = 0f;

        OnChanged?.Invoke();
        return true;
    }
}
