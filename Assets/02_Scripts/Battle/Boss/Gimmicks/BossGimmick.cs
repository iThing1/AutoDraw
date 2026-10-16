using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 보스 체력 줄에서 나오는 기믹 하나. 보스는 기믹 동안 그 줄에 멈춰 맞지 않고, 해내면 그로기로 이어진다.
// 실패 시 큰 피해가 있는 기믹은 PunishingBossGimmick을 상속한다. 새 기믹은 기존 코드를 고치지 않고 하위 클래스 하나만 추가하면 된다.
public abstract class BossGimmick : ScriptableObject
{
    // 첫 기믹 때 끊기지 않도록 기믹이 쓰는 프리팹을 미리 만들어 둔다.
    public virtual void Prewarm()
    {
    }

    // 플레이어가 기믹을 해내면 true를 돌려준다.
    public abstract UniTask<bool> PlayAsync(BattleUnit boss, CancellationToken token);
}
