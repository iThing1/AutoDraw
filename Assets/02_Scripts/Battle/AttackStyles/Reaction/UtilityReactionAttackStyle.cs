using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 플레이어가 막기나 대시를 정해진 횟수만큼 쓰면 나오는 반응 패턴의 공통 부모. 묶음에 넣어 두면 횟수가 찰 때까지는 고르지 않는다.
// 반응이 시작되면 그때부터 다시 센다.
public abstract class UtilityReactionAttackStyle : AttackStyle
{
    [Header("반응 조건")]
    [SerializeField] private UtilityType _utility;
    [SerializeField, Min(1)] private int _requiredUses = 10;

    protected UtilityType Utility { get { return _utility; } }

    public override bool CanFire(AttackContext context)
    {
        return context.Observer.GetUsesSinceMark(_utility) >= _requiredUses;
    }

    public sealed override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        context.Observer.MarkUses(_utility);

        await ReactAsync(context, gaugeGain, token);
    }

    protected abstract UniTask ReactAsync(AttackContext context, float gaugeGain, CancellationToken token);
}
