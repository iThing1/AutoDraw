using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(fileName = "SingleAttackStyle", menuName = "Scriptable Objects/AttackStyle/Single")]
public class SingleAttackStyle : AttackStyle
{
    [Header("발사체")]
    [SerializeField] private ShotSettings _shot = new();

    public override UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token)
    {
        attacker.FireShot(_shot, attacker.Owner.Body.position, Vector3.zero, gaugeGain, token);

        return UniTask.CompletedTask;
    }
}
