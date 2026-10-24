using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 카운터. 원이 다 조여 드는 순간 보스가 돌진하고, 정해진 횟수만큼 연달아 받아치면 그로기에 빠진다. 실패로 끝나는 일은 없다.
[CreateAssetMenu(fileName = "CounterGimmick", menuName = "Scriptable Objects/Boss/Gimmick/Counter")]
public class CounterGimmick : BossGimmick
{
    [Header("횟수")]
    [SerializeField, Min(1)] private int _requiredStreak = 2;
    [SerializeField, Min(0f)] private float _attemptInterval = 0.8f;

    [Header("예고")]
    // 흰 종이 위에서 보이도록 붉게 한다. 오른손 예고 빛과 같은 색 계열이다.
    [SerializeField] private GameObject _warnPrefab;
    [SerializeField] private Color _warnColor = new Color(0.85f, 0.1f, 0.15f, 0.55f);
    [SerializeField, Min(0f)] private float _warnDuration = 1f;
    [SerializeField, Min(0f)] private float _warnStartDiameter = 9f;

    [Header("돌진")]
    [SerializeField, Min(0.01f)] private float _dashDuration = 0.22f;
    [SerializeField] private Ease _dashEase = Ease.InQuad;
    // 플레이어 몸 중심에서 이 거리에 멈춘다. 몸이 겹치도록 짧게 둔다.
    [SerializeField, Min(0f)] private float _stopDistance = 0.4f;
    [SerializeField] private HitSettings _dashHit = new();
    [SerializeField, Min(0f)] private float _returnDuration = 0.4f;

    [Header("받아침")]
    [SerializeField] private ImpactSettings _counterImpact = new();
    [SerializeField, Min(0f)] private float _counterHitStopDuration = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _counterHitStopTimeScale = 0.05f;
    [SerializeField, Min(0f)] private float _knockbackDistance = 3f;
    [SerializeField, Min(0f)] private float _knockbackDuration = 0.2f;

    public override void Prewarm()
    {
        if (null != _warnPrefab)
        {
            GameManager.Pool.Prewarm(_warnPrefab, 1);
        }
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        Transform bossTransform = context.Owner.transform;
        Vector3 origin = bossTransform.position;
        int streak = 0;

        // 플레이어가 쓰러지면 전투 토큰이 끊겨 빠져나온다.
        try
        {
            while (streak < _requiredStreak)
            {
                streak = await AttemptAsync(context, token) ? streak + 1 : 0;

                await bossTransform.DOMove(origin, _returnDuration)
                    .SetEase(Ease.OutQuad)
                    .ToUniTaskKillOnCancel(token);

                if (streak < _requiredStreak)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_attemptInterval), cancellationToken: token);
                }
            }
        }
        finally
        {
            bossTransform.DOKill();

            if (null != context.Hands)
            {
                context.Hands.ReturnToRest();
            }
        }

        return true;
    }

    // 받아쳤으면 true. 비켜서서 부딪히지 않았으면 피해 없이 다음 기회로 넘어간다.
    private async UniTask<bool> AttemptAsync(AttackContext context, CancellationToken token)
    {
        AttackTarget target = context.Target;

        if (null != context.Hands)
        {
            context.Hands.PoseAsync(HandGesture.RightWindUp, target.GetAimPoint(Vector3.zero), _warnDuration, token).Forget();
        }

        await PlayWarnAsync(context, token);

        float bodyRadius = context.Owner.GetBodyRadius();
        Transform bossTransform = context.Owner.transform;
        Vector3 start = bossTransform.position;
        bool isCountered = false;
        bool isDodged = false;
        bool hasCollided = false;

        // 플레이어가 움직여도 따라붙도록 도착 지점을 매 프레임 다시 구하고, 처음 닿는 순간 막았는지로 결과를 정한다.
        Tween dash = DOVirtual.Float(0f, 1f, _dashDuration, progress =>
            {
                if (hasCollided)
                {
                    return;
                }

                bossTransform.position = Vector3.LerpUnclamped(start, target.GetApproachPoint(start, _stopDistance), progress);

                Vector3 body = context.Owner.Body.position;

                if (target.IsBlockedAt(body, bodyRadius))
                {
                    hasCollided = true;
                    isCountered = true;
                }
                else if (target.IsDodgedAt(body, bodyRadius))
                {
                    hasCollided = true;
                    isCountered = true;
                    isDodged = true;
                }
                else if (target.IsTouching(body, bodyRadius))
                {
                    hasCollided = true;
                }
            })
            .SetEase(_dashEase);

        await dash.ToUniTaskKillOnCancel(token);

        if (isCountered)
        {
            await bossTransform.DOMove(PlayCounter(context, isDodged), _knockbackDuration)
                .SetEase(Ease.OutQuad)
                .ToUniTaskKillOnCancel(token);
        }
        else if (hasCollided)
        {
            context.Damage.Strike(_dashHit, target.GetAimPoint(Vector3.zero), 0f);
        }

        return isCountered;
    }

    private async UniTask PlayWarnAsync(AttackContext context, CancellationToken token)
    {
        if (null == _warnPrefab)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_warnDuration), cancellationToken: token);
            return;
        }

        float endDiameter = context.Owner.GetBodyRadius() * 2f;
        SpriteRenderer warn = PooledSprite.Spawn(_warnPrefab, context.Owner.Body.position, 0f, new Vector2(_warnStartDiameter, _warnStartDiameter), _warnColor);

        try
        {
            await warn.transform.DOScale(new Vector3(endDiameter, endDiameter, 1f), _warnDuration)
                .SetEase(Ease.InQuad)
                .ToUniTaskKillOnCancel(token);
        }
        finally
        {
            PooledSprite.Despawn(warn);
        }
    }

    // 받아친 순간 크게 멈칫하고, 보스가 플레이어 반대쪽으로 튕겨 나갈 자리를 돌려준다.
    private Vector3 PlayCounter(AttackContext context, bool isDodged)
    {
        AttackTarget target = context.Target;
        Vector3 body = context.Owner.Body.position;

        if (isDodged)
        {
            target.NotifyDodged(body);
        }
        else
        {
            target.NotifyBlocked(body);
        }

        GameManager.TimeScale.PlayHitStop(_counterHitStopDuration, _counterHitStopTimeScale);
        ImpactPlayer.Play(_counterImpact, body);

        Vector3 away = body - target.FollowTransform.position;
        away.z = 0f;

        return context.Owner.transform.position + (0f < away.sqrMagnitude ? away.normalized : Vector3.right) * _knockbackDistance;
    }
}
