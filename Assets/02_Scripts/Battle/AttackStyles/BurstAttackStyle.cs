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
    [SerializeField, Min(1)] private int _burstCount = 3;
    [SerializeField, Min(0f)] private float _burstInterval = 0.08f;

    public override int MaxHitDamage { get { return _shot.MaxHitDamage; } }

    public override async UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token)
    {
        for (int i = 0; i < _burstCount; i++)
        {
            attacker.FireShot(_shot, attacker.Owner.Body.position, Vector3.zero, gaugeGain, token);

            await UniTask.Delay(TimeSpan.FromSeconds(_burstInterval), cancellationToken: token);
        }
    }
}
