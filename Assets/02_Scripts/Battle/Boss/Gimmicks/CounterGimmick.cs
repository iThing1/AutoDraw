using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 카운터. 보스 오른손이 붉게 빛나고 몸 둘레의 붉은 원이 조여 들며, 원이 다 조여 드는 순간 플레이어에게 돌진한다.
// 부딪히는 순간 막기로 받아치면 보스가 튕겨 나간다. 정해진 횟수만큼 연달아 받아칠 때까지 돌진을 되풀이하고, 다 채우면 그로기에 빠진다.
// 받아치지 못하면 연속 횟수가 처음부터 다시 세어지고, 부딪혔다면 그때마다 피해를 입는다. 실패로 끝나는 일은 없다.
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
    [SerializeField] private ShotSettings _dashHit = new();
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

    public override async UniTask<bool> PlayAsync(BattleUnit boss, CancellationToken token)
    {
        Transform bossTransform = boss.transform;
        Vector3 origin = bossTransform.position;
        int streak = 0;

        // 플레이어가 쓰러지면 전투 토큰이 끊겨 빠져나온다.
        try
        {
            while (streak < _requiredStreak)
            {
                streak = await AttemptAsync(boss, token) ? streak + 1 : 0;

                await bossTransform.DOMove(origin, _returnDuration)
                    .SetEase(Ease.OutQuad)
                    .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

                if (streak < _requiredStreak)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_attemptInterval), cancellationToken: token);
                }
            }
        }
        finally
        {
            bossTransform.DOKill();

            if (null != boss.Attacker.Hands)
            {
                boss.Attacker.Hands.ReturnToRest();
            }
        }

        return true;
    }

    // 받아쳤으면 true. 비켜서서 부딪히지 않았으면 피해 없이 다음 기회로 넘어간다.
    private async UniTask<bool> AttemptAsync(BattleUnit boss, CancellationToken token)
    {
        AutoAttacker attacker = boss.Attacker;
        AttackTarget target = attacker.Target;

        if (null != attacker.Hands)
        {
            attacker.Hands.PoseAsync(HandGesture.RightWindUp, target.GetAimPoint(Vector3.zero), _warnDuration, token).Forget();
        }

        await PlayWarnAsync(boss, token);

        float bodyRadius = GetBodyRadius(boss);
        Transform bossTransform = boss.transform;
        Vector3 start = bossTransform.position;
        bool isCountered = false;
        bool hasCollided = false;

        // 플레이어가 움직여도 따라붙도록 도착 지점을 매 프레임 다시 구하고, 처음 닿는 순간 막았는지로 결과를 정한다.
        Tween dash = DOVirtual.Float(0f, 1f, _dashDuration, progress =>
            {
                if (hasCollided)
                {
                    return;
                }

                bossTransform.position = Vector3.LerpUnclamped(start, target.GetApproachPoint(start, _stopDistance), progress);

                Vector3 body = boss.Body.position;

                if (target.IsBlockedAt(body, bodyRadius))
                {
                    hasCollided = true;
                    isCountered = true;
                }
                else if (target.IsTouching(body, bodyRadius))
                {
                    hasCollided = true;
                }
            })
            .SetEase(_dashEase);

        await dash.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

        if (isCountered)
        {
            await bossTransform.DOMove(PlayCounter(boss), _knockbackDuration)
                .SetEase(Ease.OutQuad)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
        }
        else if (hasCollided)
        {
            attacker.Strike(_dashHit, target.GetAimPoint(Vector3.zero), 0f);
        }

        return isCountered;
    }

    // 몸 둘레의 붉은 원이 몸 크기까지 조여 든다. 다 조여 드는 순간 돌진하므로, 원을 보고 막기 타이밍을 잡는다.
    private async UniTask PlayWarnAsync(BattleUnit boss, CancellationToken token)
    {
        if (null == _warnPrefab)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_warnDuration), cancellationToken: token);
            return;
        }

        float endDiameter = GetBodyRadius(boss) * 2f;
        SpriteRenderer warn = PooledSprite.Spawn(_warnPrefab, boss.Body.position, 0f, new Vector2(_warnStartDiameter, _warnStartDiameter), _warnColor);

        try
        {
            await warn.transform.DOScale(new Vector3(endDiameter, endDiameter, 1f), _warnDuration)
                .SetEase(Ease.InQuad)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
        }
        finally
        {
            PooledSprite.Despawn(warn);
        }
    }

    // 받아친 순간 크게 멈칫하고, 보스가 플레이어 반대쪽으로 튕겨 나갈 자리를 돌려준다.
    private Vector3 PlayCounter(BattleUnit boss)
    {
        AttackTarget target = boss.Attacker.Target;
        Vector3 body = boss.Body.position;

        target.NotifyBlocked(body);
        GameManager.TimeScale.PlayHitStop(_counterHitStopDuration, _counterHitStopTimeScale);
        ImpactPlayer.Play(_counterImpact, body);

        Vector3 away = body - target.FollowTransform.position;
        away.z = 0f;

        return boss.transform.position + (0f < away.sqrMagnitude ? away.normalized : Vector3.right) * _knockbackDistance;
    }

    private float GetBodyRadius(BattleUnit boss)
    {
        SpriteRenderer body = boss.Body.GetComponent<SpriteRenderer>();

        return null != body ? body.bounds.extents.x : 1f;
    }
}
