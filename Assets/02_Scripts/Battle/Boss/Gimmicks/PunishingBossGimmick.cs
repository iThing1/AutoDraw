using UnityEngine;

// 실패하면 플레이어에게 막을 수 없는 큰 피해를 주는 기믹의 공통 부모. 실패가 없는 기믹(버티기형)은 BossGimmick을 바로 상속해 이 값이 보이지 않는다.
public abstract class PunishingBossGimmick : BossGimmick
{
    [Header("실패")]
    [SerializeField] private HitSettings _failHit = new();

    protected void PunishTarget(AttackContext context)
    {
        AttackTarget target = context.Target;

        if (null == target)
        {
            return;
        }

        context.Damage.StrikeUnblockable(_failHit, target.GetAimPoint(Vector3.zero));
    }
}
