using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(fileName = "SingleAttackStyle", menuName = "Scriptable Objects/AttackStyle/Single")]
public class SingleAttackStyle : AttackStyle
{
    [Header("발사체")]
    [SerializeField] private ShotSettings _shot = new();

    public override UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        context.Launcher.FireShot(_shot, context.Owner.Body.position, Vector3.zero, gaugeGain, token);

        return UniTask.CompletedTask;
    }
}
