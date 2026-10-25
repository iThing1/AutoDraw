using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 보스 공격과 따로 도는 환경 탄막 하나. 스테이지가 정해진 주기로 꺼내 쓴다.
public abstract class HazardPattern : ScriptableObject
{
    // 첫 발동 때 끊기지 않도록 이 패턴이 쓰는 프리팹을 풀에 미리 만들어 둔다. 탄을 쏘는 패턴은 따로 정하지 않았으면 보스 탄을 쓴다.
    public virtual void Prewarm(GameObject defaultProjectile)
    {
    }

    public abstract UniTask PlayAsync(AttackContext context, CancellationToken token);
}
