using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 몸 둘레를 크게 휩쓸고, 휩쓴 자리에서 속이 빈 고리가 종이 끝까지 퍼져 나간다.
[CreateAssetMenu(fileName = "BrushSweepAttackStyle", menuName = "Scriptable Objects/AttackStyle/BrushSweep")]
public class BrushSweepAttackStyle : AttackStyle
{
    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.35f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.4f;

    [Header("휩쓸기")]
    [SerializeField, Min(0f)] private float _radius = 3.4f;
    [SerializeField, Range(0f, 720f)] private float _sweepAngle = 360f;
    [SerializeField, Min(0.01f)] private float _sweepDuration = 0.35f;
    [SerializeField] private Ease _sweepEase = Ease.OutCubic;
    [SerializeField, Range(0f, 1f)] private float _hitTiming = 0.5f;
    [SerializeField] private GameObject _trailPrefab;
    [SerializeField, Min(0f)] private float _trailLingerTime = 0.25f;

    [SerializeField] private HitSettings _hit = new();

    [Header("퍼지는 고리")]
    // 프리팹에 반지름 1짜리 원 점이 로컬 좌표로 들어 있어, 크기를 반지름만큼 키워 퍼뜨린다.
    [SerializeField] private LineRenderer _ringPrefab;
    [SerializeField] private Color _ringColor = new Color(0.08f, 0.08f, 0.1f, 0.9f);
    [SerializeField, Min(0.05f)] private float _ringWidth = 0.6f;
    [SerializeField, Min(0f)] private float _ringSpeed = 12f;
    [SerializeField, Min(0f)] private float _ringMaxRadius = 60f;
    [SerializeField, Range(0f, 1f)] private float _ringFadeStart = 0.7f;
    // 고리를 여러 겹 연달아 퍼뜨릴 때의 겹 수와 간격. 막기를 다시 쓸 수 있을 만큼 띄운다.
    [SerializeField, Min(1)] private int _ringCount = 1;
    [SerializeField, Min(0f)] private float _ringInterval = 0.45f;
    [SerializeField] private HitSettings _ringHit = new();

    public override void Prewarm()
    {
        if (null != _trailPrefab)
        {
            GameManager.Pool.Prewarm(_trailPrefab, 1);
        }

        if (null != _ringPrefab)
        {
            GameManager.Pool.Prewarm(_ringPrefab.gameObject, _ringCount * 2);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        Vector3 center = context.Owner.Body.position;

        await Telegraph.PlayCircleAsync(_telegraphPrefab, center, _radius, _telegraphColor, _warnDuration, token);

        Vector3 toTarget = context.Target.GetAimPoint(Vector3.zero) - center;
        float startAngle = Geometry2D.ToAngle(toTarget) + 180f;

        GameObject trail = null != _trailPrefab ? GameManager.Pool.Spawn(_trailPrefab, GetPointOnCircle(center, startAngle), Quaternion.identity) : null;
        Transform hand = null != context.Hands ? context.Hands.RightHand : null;

        if (null != trail && trail.TryGetComponent(out TrailRenderer trailRenderer))
        {
            // 풀에서 다시 꺼낸 꼬리에 이전 궤적이 남아 있으면 이전 자리에서부터 선이 그어진다.
            trailRenderer.Clear();
        }

        Tween sweep = null;

        try
        {
            sweep = DOVirtual.Float(startAngle, startAngle - _sweepAngle, _sweepDuration, angle =>
                {
                    Vector3 point = GetPointOnCircle(center, angle);

                    if (null != trail)
                    {
                        trail.transform.position = point;
                    }

                    if (null != hand)
                    {
                        hand.position = point;
                    }
                })
                .SetEase(_sweepEase);

            await UniTask.Delay(TimeSpan.FromSeconds(_sweepDuration * _hitTiming), cancellationToken: token);

            // 범위 밖으로 물러났으면 맞힐 자리가 목표에 닿지 않아 빗나간다.
            Vector3 hitPoint = Vector3.MoveTowards(center, context.Target.GetAimPoint(Vector3.zero), _radius);
            context.Damage.Strike(_hit, hitPoint, gaugeGain);

            await sweep.ToUniTaskKillOnCancel(token);

            // 고리는 스스로 끝까지 퍼지므로 기다리지 않고, 보스는 다음 공격을 이어 간다.
            ExpandRingsAsync(context, center, gaugeGain, token).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(_trailLingerTime), cancellationToken: token);
        }
        finally
        {
            // 맞히기 전에 끊기면 손이 계속 돌지 않게 함께 멈춘다.
            sweep?.Kill();

            if (null != trail)
            {
                GameManager.Pool.Despawn(trail);
            }
        }
    }

    private async UniTaskVoid ExpandRingsAsync(AttackContext context, Vector3 center, float gaugeGain, CancellationToken token)
    {
        for (int i = 0; i < _ringCount; i++)
        {
            ExpandRingAsync(context, center, gaugeGain, token).Forget();

            if (i < _ringCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_ringInterval), cancellationToken: token);
            }
        }
    }

    private async UniTask ExpandRingAsync(AttackContext context, Vector3 center, float gaugeGain, CancellationToken token)
    {
        if (null == _ringPrefab)
        {
            return;
        }

        LineRenderer ring = GameManager.Pool.Spawn(_ringPrefab.gameObject, center, Quaternion.identity).GetComponent<LineRenderer>();
        ring.widthMultiplier = _ringWidth;

        AttackTarget target = context.Target;
        float radius = _radius;
        bool hasStruck = false;

        try
        {
            while (radius < _ringMaxRadius)
            {
                float previousRadius = radius;
                radius += _ringSpeed * Time.deltaTime;

                DrawRing(ring, radius);

                if (false == hasStruck && IsRingPassing(target, center, previousRadius, radius))
                {
                    hasStruck = true;
                    context.Damage.Strike(_ringHit, target.GetAimPoint(Vector3.zero), gaugeGain);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            GameManager.Pool.Despawn(ring.gameObject);
        }
    }

    private void DrawRing(LineRenderer ring, float radius)
    {
        ring.transform.localScale = new Vector3(radius, radius, 1f);

        float fade = Mathf.InverseLerp(_ringMaxRadius, _ringMaxRadius * _ringFadeStart, radius);
        Color color = new Color(_ringColor.r, _ringColor.g, _ringColor.b, _ringColor.a * fade);
        ring.startColor = color;
        ring.endColor = color;
    }

    // 고리 선이 몸에 걸쳤거나, 한 프레임 사이에 몸을 넘어 지나갔으면 맞는다. 프레임이 끊겨도 건너뛰지 않게 한다.
    private bool IsRingPassing(AttackTarget target, Vector3 center, float previousRadius, float radius)
    {
        Vector3 targetPoint = target.FollowTransform.position;
        float distance = Vector2.Distance(center, targetPoint);

        if (previousRadius <= distance && radius >= distance)
        {
            return true;
        }

        Vector3 offset = targetPoint - center;
        offset.z = 0f;
        Vector3 nearest = center + (0f < offset.sqrMagnitude ? offset.normalized : Vector3.right) * radius;

        return target.IsTouching(nearest, _ringWidth * 0.5f);
    }

    private Vector3 GetPointOnCircle(Vector3 center, float angle)
    {
        return center + Geometry2D.ToDirection(angle) * _radius;
    }
}
