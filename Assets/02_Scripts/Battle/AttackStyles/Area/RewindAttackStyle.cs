using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 최근에 쓴 패턴 몇 개를 그 자리 그대로 거꾸로 다시 재생한다. 처음엔 하나씩 보던 패턴이 한꺼번에 겹쳐 온다.
// 되감을 수 있는 패턴(휘갈기기, 데칼코마니, 액자 가두기)은 쏠 때 기록을 남기고, 어떻게 거꾸로 재생할지는 각 패턴이 정한다.
// 되감기 전에 보스 둘레로 원이 빨려 들고 시곗바늘이 거꾸로 돌아 되감기가 시작됨을 알리고, 되감은 패턴은 같은 색을 덧입혀 원래 패턴과 구분한다.
// 되감은 기록은 지워, 같은 패턴을 두 번 되감지 않는다.
[CreateAssetMenu(fileName = "RewindAttackStyle", menuName = "Scriptable Objects/AttackStyle/Rewind")]
public class RewindAttackStyle : AttackStyle
{
    // 시곗바늘이 처음 가리키는 12시 방향(도).
    private const float TWELVE_O_CLOCK = 90f;
    private const float FULL_TURN_DEGREES = 360f;

    [Header("되감기")]
    // 한 번에 되감는 기록 수. 기록이 이만큼 쌓여야 쏜다.
    [SerializeField, Min(1)] private int _replayCount = 2;

    [Header("색")]
    // 시작 연출의 색이자, 되감은 패턴의 예고·먹에 덧입히는 색.
    [SerializeField] private Color _tintColor = new Color(0.25f, 0.55f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float _tintStrength = 0.5f;

    [Header("시작 연출")]
    // 연출이 끝나야 되감은 패턴의 예고가 시작된다.
    [SerializeField, Min(0f)] private float _introDuration = 0.7f;
    [SerializeField] private ImpactSettings _introImpact = new();
    // 보스 둘레에서 안쪽으로 빨려 드는 원. 동그라미 그림을 쓴다.
    [SerializeField] private GameObject _ripplePrefab;
    [SerializeField, Min(1)] private int _rippleCount = 3;
    [SerializeField, Min(0.1f)] private float _rippleDiameter = 12f;
    [SerializeField, Range(0f, 1f)] private float _rippleAlpha = 0.3f;
    [SerializeField, Min(0.01f)] private float _rippleDuration = 0.45f;
    [SerializeField, Min(0f)] private float _rippleInterval = 0.12f;
    // 보스 자리에서 시계 반대 방향으로 도는 바늘. 1x1 네모 그림을 길이·굵기에 맞춰 늘린다.
    [SerializeField] private GameObject _handPrefab;
    [SerializeField, Min(0.1f)] private float _handLength = 2.5f;
    [SerializeField, Min(0.01f)] private float _handWidth = 0.2f;
    [SerializeField, Min(0f)] private float _handTurns = 2f;

    public override void Prewarm()
    {
        if (null != _ripplePrefab)
        {
            GameManager.Pool.Prewarm(_ripplePrefab, _rippleCount);
        }

        if (null != _handPrefab)
        {
            GameManager.Pool.Prewarm(_handPrefab, 1);
        }
    }

    public override bool CanFire(AttackContext context)
    {
        return context.RewindLog.Count >= _replayCount;
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        List<RewindRecord> records = new List<RewindRecord>(_replayCount);
        context.RewindLog.TakeLatest(_replayCount, records);

        await PlayIntroAsync(context.Owner.Body.position, token);

        InkTint tint = new InkTint(_tintColor, _tintStrength);
        List<UniTask> replays = new List<UniTask>(records.Count);

        for (int i = 0; i < records.Count; i++)
        {
            replays.Add(records[i].ReplayAsync(context, tint, gaugeGain, token));
        }

        await UniTask.WhenAll(replays);
    }

    private async UniTask PlayIntroAsync(Vector3 center, CancellationToken token)
    {
        ImpactPlayer.Play(_introImpact, center, _tintColor);

        List<UniTask> effects = new List<UniTask>(_rippleCount + 1);

        for (int i = 0; i < _rippleCount; i++)
        {
            effects.Add(PlayRippleAsync(center, _rippleInterval * i, token));
        }

        effects.Add(SpinHandAsync(center, token));

        await UniTask.WhenAll(effects);
    }

    // 큰 원이 옅게 나타나며 보스 자리로 오그라든다. 퍼져 나가는 충격파를 거꾸로 돌린 모양이다.
    private async UniTask PlayRippleAsync(Vector3 center, float delay, CancellationToken token)
    {
        if (null == _ripplePrefab)
        {
            return;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);

        Color color = new Color(_tintColor.r, _tintColor.g, _tintColor.b, 0f);
        SpriteRenderer ripple = PooledSprite.Spawn(_ripplePrefab, center, 0f, new Vector2(_rippleDiameter, _rippleDiameter), color);

        try
        {
            UniTask fade = ripple.DOFade(_rippleAlpha, _rippleDuration * 0.5f)
                .ToUniTaskKillOnCancel(token);

            UniTask shrink = ripple.transform.DOScale(Vector3.zero, _rippleDuration)
                .SetEase(Ease.InQuad)
                .ToUniTaskKillOnCancel(token);

            await UniTask.WhenAll(fade, shrink);
        }
        finally
        {
            PooledSprite.Despawn(ripple);
        }
    }

    // 12시에서 시작해 시계 반대 방향으로 돈다. 그림 가운데가 회전축이라, 한쪽 끝이 보스 자리에 오도록 길이의 절반만큼 밀어 놓는다.
    private async UniTask SpinHandAsync(Vector3 center, CancellationToken token)
    {
        if (null == _handPrefab)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_introDuration), cancellationToken: token);
            return;
        }

        SpriteRenderer hand = PooledSprite.Spawn(_handPrefab, center, TWELVE_O_CLOCK, new Vector2(_handLength, _handWidth), _tintColor);
        float sweep = _handTurns * FULL_TURN_DEGREES;
        float elapsed = 0f;

        try
        {
            while (true)
            {
                float angle = TWELVE_O_CLOCK + sweep * DOVirtual.EasedValue(0f, 1f, 0f < _introDuration ? elapsed / _introDuration : 1f, Ease.InOutQuad);

                hand.transform.SetPositionAndRotation(center + Geometry2D.ToDirection(angle) * (_handLength * 0.5f), Quaternion.Euler(0f, 0f, angle));

                if (elapsed >= _introDuration)
                {
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed = Mathf.Min(elapsed + Time.deltaTime, _introDuration);
            }
        }
        finally
        {
            PooledSprite.Despawn(hand);
        }
    }
}
