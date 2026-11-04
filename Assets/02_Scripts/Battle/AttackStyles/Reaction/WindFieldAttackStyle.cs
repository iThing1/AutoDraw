using System.Threading;
using UnityEngine;

// 플레이어가 대시를 자주 쓰면, 보스 둘레에 기류를 펼쳐 공격을 흘려보내고 가까이 온 플레이어를 바깥으로 민다.
// 대시는 미는 힘을 받지 않아, 기류를 뚫고 들어가는 데 쓸 수 있다.
[CreateAssetMenu(fileName = "WindFieldAttackStyle", menuName = "Scriptable Objects/AttackStyle/WindField")]
public class WindFieldAttackStyle : GuardAuraAttackStyle
{
    [Header("기류")]
    // 이 거리 안에 들어온 플레이어를 바깥으로 미는 빠르기(초당 거리). 플레이어 이동 속도보다 낮게 둬야 버티며 다가올 수 있다.
    [SerializeField, Min(0f)] private float _pushRadius = 5f;
    [SerializeField, Min(0f)] private float _pushSpeed = 3f;
    // 흘려보낸 자리의 작은 바람 자국. 자주 나오므로 흔들림은 0으로 둔다.
    [SerializeField] private ImpactSettings _deflectImpact = new();

    protected override void OnBlocked(AttackContext context, Vector3 point, float gaugeGain, CancellationToken token)
    {
        GameManager.Feedback.PlayImpact(_deflectImpact, point);
    }

    protected override void OnTick(AttackContext context)
    {
        Vector2 offset = context.Target.FollowTransform.position - context.Owner.Body.position;

        context.Target.SetPull(offset.magnitude < _pushRadius ? offset.normalized * _pushSpeed : Vector2.zero);
    }

    protected override void OnEnd(AttackContext context)
    {
        context.Target.SetPull(Vector2.zero);
    }
}
