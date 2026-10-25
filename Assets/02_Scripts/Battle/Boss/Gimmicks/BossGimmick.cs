using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 보스 체력 줄에서 나오는 기믹 하나. 실패 시 큰 피해가 있는 기믹은 PunishingBossGimmick을 상속한다.
public abstract class BossGimmick : ScriptableObject
{
    // true면 기믹 동안 플레이어 공격을 멈춘다. 실제로 멈추고 다시 켜는 일은 전투 스테이지가 맡는다.
    public virtual bool PausesTargetAttack { get { return false; } }

    // 첫 기믹 때 끊기지 않도록 기믹이 쓰는 프리팹을 미리 만들어 둔다.
    public virtual void Prewarm()
    {
    }

    public abstract UniTask<bool> PlayAsync(AttackContext context, CancellationToken token);
}
