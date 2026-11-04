using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 막기를 자주 쓰면 나오는 반응. 막을 수 없는 유리 진열장을 플레이어가 갈 자리에 연달아 떨어뜨린다.
[CreateAssetMenu(fileName = "DoNotTouchAttackStyle", menuName = "Scriptable Objects/AttackStyle/DoNotTouch")]
public class DoNotTouchAttackStyle : UtilityReactionAttackStyle
{
    [Header("진열장")]
    [SerializeField, Min(1)] private int _caseCount = 3;
    [SerializeField] private Vector2 _caseSize = new Vector2(2.4f, 2.4f);
    // 0이면 지금 자리, 1이면 떨어질 때 있을 자리를 노린다.
    [SerializeField, Range(0f, 1.5f)] private float _leadRatio = 0.6f;
    [SerializeField, Min(0f)] private float _dropInterval = 0.45f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
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

        Vector3 aimPoint = context.Target.GetAimPoint(Vector3.zero);

        if (IsInside(aimPoint, point))
        {
            context.Damage.StrikeThroughGuard(_hit, aimPoint);
        }

        await ShowCaseAsync(point, token);
    }

    private bool IsInside(Vector3 position, Vector3 center)
    {
        return Mathf.Abs(position.x - center.x) <= _caseSize.x * 0.5f && Mathf.Abs(position.y - center.y) <= _caseSize.y * 0.5f;
    }

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
