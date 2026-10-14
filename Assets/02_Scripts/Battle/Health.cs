using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class Health : MonoBehaviour
{
    public event Action<DamageInfo> OnDamaged;
    public event Action<int> OnHealed;
    public event Action OnDied;

    // 죽는 순간 완료되는 작업. 전투 매니저가 이것을 await해서 쓰러질 때까지 기다린다.
    private UniTaskCompletionSource _deathSource = new();

    private int _maxHp;
    private int _currentHp;

    public int MaxHp { get { return _maxHp; } }
    public int CurrentHp { get { return _currentHp; } }
    public float Ratio { get { return 0 < _maxHp ? (float)_currentHp / _maxHp : 0f; } }
    public bool IsDead { get { return 0 >= _currentHp; } }

    public void Init(int maxHp)
    {
        _maxHp = maxHp;
        _currentHp = maxHp;
    }

    public void TakeDamage(DamageInfo damage)
    {
        if (IsDead)
        {
            return;
        }

        _currentHp = Mathf.Max(0, _currentHp - damage.Amount);

        OnDamaged?.Invoke(damage);

        if (IsDead)
        {
            OnDied?.Invoke();
            _deathSource.TrySetResult();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || 0 >= amount)
        {
            return;
        }

        int healed = Mathf.Min(amount, _maxHp - _currentHp);

        if (0 >= healed)
        {
            return;
        }

        _currentHp += healed;

        OnHealed?.Invoke(healed);
    }

    public UniTask WaitForDeathAsync(CancellationToken token)
    {
        // 죽기 전에 씬이 끝나면 기다림도 함께 취소되도록 토큰을 연결한다.
        return _deathSource.Task.AttachExternalCancellation(token);
    }
}
