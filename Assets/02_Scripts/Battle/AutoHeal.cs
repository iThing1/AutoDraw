using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 시연에서 플레이어가 쓰러지지 않도록, 체력이 기준 아래로 떨어지면 목표 비율까지 서서히 회복한다.
[RequireComponent(typeof(BattleUnit), typeof(Health))]
public class AutoHeal : MonoBehaviour
{
    private BattleUnit _owner;
    private Health _health;
    private bool _isHealing;

    // 같은 오브젝트의 Awake 순서는 보장되지 않으므로 BattleUnit.Health 대신 직접 가져온다.
    private void Awake()
    {
        _owner = GetComponent<BattleUnit>();
        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        _health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        _health.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(DamageInfo damage)
    {
        BattleUnitData data = _owner.Data;

        if (false == data.UseAutoHeal || _isHealing || _health.IsDead)
        {
            return;
        }

        if (_health.Ratio > data.AutoHealThresholdRatio)
        {
            return;
        }

        HealAsync(destroyCancellationToken).Forget();
    }

    private async UniTaskVoid HealAsync(CancellationToken token)
    {
        _isHealing = true;

        BattleUnitData data = _owner.Data;

        int startHp = _health.CurrentHp;
        int targetHp = Mathf.RoundToInt(_health.MaxHp * data.AutoHealTargetRatio);
        float elapsed = 0f;

        try
        {
            while (elapsed < data.AutoHealDuration && false == _health.IsDead)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);

                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(elapsed / data.AutoHealDuration);
                int desiredHp = Mathf.RoundToInt(Mathf.Lerp(startHp, targetHp, progress));

                _health.Heal(desiredHp - _health.CurrentHp);
            }

            _health.Heal(targetHp - _health.CurrentHp);
        }
        catch (OperationCanceledException)
        {
            // 유닛이 사라져 회복이 중간에 끊긴 경우라 따로 처리하지 않는다.
        }
        finally
        {
            _isHealing = false;
        }
    }
}
