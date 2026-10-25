using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 목표가 지금 쓰는 무기를 흉내 낸다. 기본 무기면 가벼운 연사, 화력 무기면 크고 느린 한 발이고, 실제로 쏘는 방식은 따로 둔 공격 스타일에 맡긴다.
// 간격·대기·멈춤·손동작은 이 에셋의 값을 쓰고, 흉내용 스타일에 적힌 값은 쓰지 않는다.
[CreateAssetMenu(fileName = "MimicShotAttackStyle", menuName = "Scriptable Objects/AttackStyle/MimicShot")]
public class MimicShotAttackStyle : AttackStyle
{
    [Header("흉내")]
    // 무기를 골라 쓰지 않는 목표에게는 기본 무기 흉내를 쓴다.
    [SerializeField] private AttackStyle _basicMimic;
    [SerializeField] private AttackStyle _heavyMimic;

    public override void Prewarm()
    {
        if (null != _basicMimic)
        {
            _basicMimic.Prewarm();
        }

        if (null != _heavyMimic)
        {
            _heavyMimic.Prewarm();
        }
    }

    public override UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        AttackStyle mimic = PickMimic(context.Observer);

        if (null == mimic)
        {
            Debug.LogError($"흉내 사격 : {name}에 흉내 낼 공격 스타일이 지정되지 않았습니다.");
            return UniTask.CompletedTask;
        }

        return mimic.FireAsync(context, gaugeGain, token);
    }

    private AttackStyle PickMimic(TargetObserver observer)
    {
        if (observer.TryGetWeaponType(out WeaponType type) && WeaponType.Heavy == type)
        {
            return _heavyMimic;
        }

        return _basicMimic;
    }
}
