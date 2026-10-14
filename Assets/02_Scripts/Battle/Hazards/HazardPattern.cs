using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 보스 공격과 따로 도는 환경 탄막 하나. 보스 패턴 묶음에 들어가지 않고, 스테이지가 정해진 주기로 꺼내 쓴다.
// 새 환경 탄막은 기존 코드를 고치지 않고 하위 클래스 하나만 추가하면 된다.
public abstract class HazardPattern : ScriptableObject
{
    // 한 번 발동할 때 동시에 날아갈 수 있는 가장 많은 탄 수. 첫 발동 때 끊기지 않도록 풀에 미리 만들어 둘 때 쓴다.
    public abstract int MaxProjectileCount { get; }

    // 탄은 쏘는 쪽(보스)의 투사체와 물감 색을 빌려 쓰고, 맞았을 때 처리도 쏘는 쪽에 맡긴다.
    public abstract UniTask PlayAsync(AutoAttacker shooter, CancellationToken token);
}
