using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 그림자 돌진. 종이를 가로지르는 길 몇 개를 한꺼번에 예고하고, 먹 그림자가 차례로 그 길을 따라 빠르게 돌진한다.
// 그림자는 공격 대상이 아닌 피해야 할 장애물이라, 길이 차오르는 순서를 읽고 비켜서거나 지나가는 순간 막는다.
[CreateAssetMenu(fileName = "ShadowDashAttackStyle", menuName = "Scriptable Objects/AttackStyle/ShadowDash")]
public class ShadowDashAttackStyle : AttackStyle
{
    [Header("길")]
    [SerializeField, Min(1)] private int _dashCount = 3;
    // 종이 끝에서 끝까지 닿도록 넉넉히 잡는다.
    [SerializeField, Min(1f)] private float _length = 90f;
    // 길이 목표 자리에서 옆으로 비켜 지나는 거리. 0이면 늘 목표를 정확히 지난다.
    [SerializeField, Min(0f)] private float _offsetRange = 3f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.3f);
    // 첫 그림자가 출발할 때까지. 뒤 그림자는 돌진 간격만큼 길의 예고가 더 길다.
    [SerializeField, Min(0f)] private float _warnDuration = 0.8f;

    [Header("돌진")]
    [SerializeField] private GameObject _shadowPrefab;
    [SerializeField] private Color _shadowColor = new Color(0.02f, 0.02f, 0.03f, 1f);
    [SerializeField, Min(0.1f)] private float _shadowDiameter = 2.2f;
    [SerializeField, Min(0.1f)] private float _dashSpeed = 45f;
    [SerializeField, Min(0f)] private float _dashInterval = 0.35f;
    [SerializeField] private HitSettings _hit = new();

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, _dashCount * 2);
        }

        if (null != _shadowPrefab)
        {
            GameManager.Pool.Prewarm(_shadowPrefab, _dashCount);
        }
    }

    // 길은 시작할 때 한꺼번에 정해 모두 보여 주고, 길마다 차오르는 시간을 달리해 그림자가 나갈 순서를 알린다.
    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        List<UniTask> dashes = new List<UniTask>(_dashCount);
        Vector3 targetPoint = context.Target.GetAimPoint(Vector3.zero);

        for (int i = 0; i < _dashCount; i++)
        {
            LinePlacement line = LinePlacement.PickNear(targetPoint, _offsetRange);

            dashes.Add(DashAsync(context, line, _warnDuration + _dashInterval * i, gaugeGain, token));
        }

        await UniTask.WhenAll(dashes);
    }

    private async UniTask DashAsync(AttackContext context, LinePlacement line, float warnDuration, float gaugeGain, CancellationToken token)
    {
        await Telegraph.PlayLineAsync(_telegraphPrefab, line.Center, line.Angle, _length, _shadowDiameter, _telegraphColor, warnDuration, token);

        if (null == _shadowPrefab)
        {
            return;
        }

        // 양 끝 중 무작위로 한쪽에서 출발한다.
        float direction = 0.5f > UnityEngine.Random.value ? -1f : 1f;
        Vector3 start = line.Center - line.Along * (_length * 0.5f * direction);
        Vector3 velocity = line.Along * (_dashSpeed * direction);
        float radius = _shadowDiameter * 0.5f;

        SpriteRenderer shadow = PooledSprite.Spawn(_shadowPrefab, start, 0f, new Vector2(_shadowDiameter, _shadowDiameter), _shadowColor);
        AttackTarget target = context.Target;
        float travelled = 0f;
        bool hasStruck = false;

        try
        {
            // 게임 시간으로 달려 히트 스톱 동안에는 함께 멈춘다. 빠르게 지나가도 건너뛰지 않게 이전 자리와 잇는 선분으로 본다.
            while (travelled < _length)
            {
                Vector3 previous = shadow.transform.position;
                Vector3 step = velocity * Time.deltaTime;

                shadow.transform.position = previous + step;
                travelled += step.magnitude;

                if (false == hasStruck && IsTouchingPath(target, previous, shadow.transform.position, radius))
                {
                    hasStruck = true;
                    context.Damage.Strike(_hit, target.GetAimPoint(Vector3.zero), gaugeGain);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            PooledSprite.Despawn(shadow);
        }
    }

    private bool IsTouchingPath(AttackTarget target, Vector3 from, Vector3 to, float radius)
    {
        return target.IsTouching(Geometry2D.GetClosestPointOnSegment(from, to, target.FollowTransform.position), radius);
    }
}
