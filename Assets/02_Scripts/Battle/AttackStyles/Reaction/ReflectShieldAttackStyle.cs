using System.Threading;
using UnityEngine;

// 플레이어가 막기를 자주 쓰면, 보스도 막을 두르고 막은 공격을 그 자리에서 플레이어에게 되쏜다.
[CreateAssetMenu(fileName = "ReflectShieldAttackStyle", menuName = "Scriptable Objects/AttackStyle/ReflectShield")]
public class ReflectShieldAttackStyle : GuardAuraAttackStyle
{
    [Header("되쏘기")]
    [SerializeField] private ShotSettings _reflectShot = new();
    [SerializeField] private Color _reflectColor = new Color(0.4f, 0.6f, 0.95f, 1f);

    protected override void OnBlocked(AttackContext context, Vector3 point, float gaugeGain, CancellationToken token)
    {
        context.Launcher.FireShot(_reflectShot, point, Vector3.zero, _reflectColor, gaugeGain, token);
    }
}
