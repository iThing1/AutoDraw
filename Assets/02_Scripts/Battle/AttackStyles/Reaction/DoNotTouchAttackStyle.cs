using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 플레이어가 막기를 자주 쓰면 "손대지 마시오". 유리 진열장 여러 개를 플레이어가 갈 자리에 연달아 떨어뜨린다.
// 진열장은 막을 수 없어 빨간 예고를 보고 움직여 피해야 한다. 대시로 피하는 것은 그대로 통한다.
[CreateAssetMenu(fileName = "DoNotTouchAttackStyle", menuName = "Scriptable Objects/AttackStyle/DoNotTouch")]
public class DoNotTouchAttackStyle : UtilityReactionAttackStyle
{
    [Header("진열장")]
    [SerializeField, Min(1)] private int _caseCount = 3;
    [SerializeField] private Vector2 _caseSize = new Vector2(2.4f, 2.4f);
    // 떨어지기까지의 시간 중 이 비율만큼 앞질러 노린다. 0이면 지금 자리다.
    [SerializeField, Range(0f, 1.5f)] private float _leadRatio = 0.6f;
    [SerializeField, Min(0f)] private float _dropInterval = 0.45f;

    [Header("예고")]
    // 예고와 진열장 그림. 1x1 네모 그림을 크기에 맞춰 늘린다.
    [SerializeField] private GameObject _telegraphPrefab;
    // 막을 수 없다는 것을 알리도록 다른 예고와 달리 빨갛게 둔다.
    [SerializeField] private Color _telegraphColor = new Color(0.85f, 0.1f, 0.1f, 0.45f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.8f;

    [Header("낙하")]
    [SerializeField] private GameObject _casePrefab;
    [SerializeField] private Color _caseColor = new Color(0.75f, 0.9f, 0.95f, 0.55f);
    [SerializeField, Min(0f)] private float _lingerDuration = 0.3f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, _caseCount * 2);
        }

        if (null != _casePrefab)
        {
            GameManager.Pool.Prewarm(_casePrefab, _caseCount);
        }
    }

    protected override async UniTask ReactAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        List<UniTask> drops = ListPool<UniTask>.Get();

        try
        {
            for (int i = 0; i < _caseCount; i++)
            {
                drops.Add(DropAsync(context, context.Target.GetPredictedPoint(_warnDuration * _leadRatio), token));

                if (i < _caseCount - 1)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_dropInterval), cancellationToken: token);
                }
            }

            await UniTask.WhenAll(drops);
        }
        finally
        {
            ListPool<UniTask>.Release(drops);
        }
    }

    private async UniTask DropAsync(AttackContext context, Vector3 point, CancellationToken token)
    {
        await Telegraph.PlayAsync(GameManager.Pool, _telegraphPrefab, point, 0f, _caseSize, Vector2.zero, _telegraphColor, _warnDuration, token);

        GameManager.Feedback.PlayImpact(_impact, point);

        AttackTarget target = context.Target;

        if (IsInside(target.GetAimPoint(Vector3.zero), point) && false == target.TryDodge())
        {
            context.Damage.StrikeUnblockable(_hit, target.GetAimPoint(Vector3.zero));
        }

        await ShowCaseAsync(point, token);
    }

    private bool IsInside(Vector3 position, Vector3 center)
    {
        return Mathf.Abs(position.x - center.x) <= _caseSize.x * 0.5f && Mathf.Abs(position.y - center.y) <= _caseSize.y * 0.5f;
    }

    // 떨어진 진열장은 판정과 상관없이 잠깐 남았다가 옅어지며 사라진다.
    private async UniTask ShowCaseAsync(Vector3 point, CancellationToken token)
    {
        if (null == _casePrefab)
        {
            return;
        }

        SpriteRenderer glass = GameManager.Pool.SpawnSprite(_casePrefab, point, 0f, _caseSize, _caseColor);

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_lingerDuration), cancellationToken: token);

            await glass.DOFade(0f, _fadeDuration)
                .ToUniTaskKillOnCancel(token);
        }
        finally
        {
            GameManager.Pool.DespawnSprite(glass);
        }
    }
}
