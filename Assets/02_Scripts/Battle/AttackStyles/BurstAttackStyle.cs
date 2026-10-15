using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(fileName = "BurstAttackStyle", menuName = "Scriptable Objects/AttackStyle/Burst")]
public class BurstAttackStyle : AttackStyle
{
    [Header("발사체")]
    [SerializeField] private ShotSettings _shot = new();

    [Header("연사")]
    // 쏠 때마다 이 범위에서 발 수를 고른다. 같은 수로 고정하려면 최소와 최대를 같게 둔다.
    [SerializeField] private IntRange _shotCount = new(3, 3);
    [SerializeField, Min(0f)] private float _burstInterval = 0.08f;

    public override async UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token)
    {
        int shotCount = Mathf.Max(1, _shotCount.PickRandom());

        for (int i = 0; i < shotCount; i++)
        {
            attacker.FireShot(_shot, attacker.Owner.Body.position, Vector3.zero, gaugeGain, token);

            await UniTask.Delay(TimeSpan.FromSeconds(_burstInterval), cancellationToken: token);
        }
    }
}
