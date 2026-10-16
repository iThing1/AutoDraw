using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class Health : MonoBehaviour
{
    // 무적인 동안을 using 블록으로 묶는다. 여러 연출이 겹쳐 걸어도 모두 풀려야 다시 맞는다.
    public readonly struct InvulnerableScope : IDisposable
    {
        private readonly Health _health;

        internal InvulnerableScope(Health health)
        {
            _health = health;
        }

        public void Dispose()
        {
            if (null == _health)
            {
                return;
            }

            _health.EndInvulnerable();
        }
    }

    public event Action<DamageInfo> OnDamaged;
    public event Action<int> OnHealed;
    public event Action OnDied;
    // 최소 체력에 막 닿았을 때. 보스가 기믹 줄에서 멈추거나 쓰러졌다 일어나는 데 쓴다.
    public event Action OnFloorReached;
    // 체력을 다시 가득 채웠을 때. 체력바가 차오르는 연출을 따로 보여 준다.
    public event Action OnRefilled;

    // 죽는 순간 완료되는 작업. 전투 매니저가 이것을 await해서 쓰러질 때까지 기다린다.
    private UniTaskCompletionSource _deathSource = new();

    private int _maxHp;
    private int _currentHp;
    private int _floorHp;
    private int _invulnerableCount;
    private float _damageMultiplier = 1f;

    public int MaxHp { get { return _maxHp; } }
    public int CurrentHp { get { return _currentHp; } }
    public float Ratio { get { return 0 < _maxHp ? (float)_currentHp / _maxHp : 0f; } }
    public bool IsDead { get { return 0 >= _currentHp; } }
    public bool IsInvulnerable { get { return 0 < _invulnerableCount; } }

    public void Init(int maxHp)
    {
        _maxHp = maxHp;
        _currentHp = maxHp;
    }

    // 체력이 이 값 아래로 내려가지 않는다. 0이면 제한이 없어 쓰러질 수 있다.
    public void SetFloor(int floorHp)
    {
        _floorHp = Mathf.Clamp(floorHp, 0, _maxHp);
    }

    // 그로기처럼 잠깐 더 아프게 맞는 동안 쓴다. 피해 숫자도 늘어난 값으로 보인다.
    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = Mathf.Max(0f, multiplier);
    }

    public InvulnerableScope BeginInvulnerable()
    {
        _invulnerableCount++;

        return new InvulnerableScope(this);
    }

    public void TakeDamage(DamageInfo damage)
    {
        if (IsDead || IsInvulnerable)
        {
            return;
        }

        bool wasAboveFloor = _currentHp > _floorHp;

        damage.Amount = Mathf.RoundToInt(damage.Amount * _damageMultiplier);
        _currentHp = Mathf.Max(_floorHp, _currentHp - damage.Amount);

        OnDamaged?.Invoke(damage);

        if (IsDead)
        {
            OnDied?.Invoke();
            _deathSource.TrySetResult();
            return;
        }

        if (wasAboveFloor && _currentHp == _floorHp)
        {
            OnFloorReached?.Invoke();
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

    // 쓰러지지 않은 채로 최대 체력을 바꿔 가득 채운다. 다음 페이즈의 체력으로 다시 시작할 때 쓴다.
    public void Refill(int maxHp)
    {
        if (IsDead)
        {
            return;
        }

        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = _maxHp;
        _floorHp = 0;

        OnRefilled?.Invoke();
    }

    public UniTask WaitForDeathAsync(CancellationToken token)
    {
        // 죽기 전에 씬이 끝나면 기다림도 함께 취소되도록 토큰을 연결한다.
        return _deathSource.Task.AttachExternalCancellation(token);
    }

    private void EndInvulnerable()
    {
        _invulnerableCount = Mathf.Max(0, _invulnerableCount - 1);
    }
}
