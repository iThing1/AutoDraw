using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 보스 둘레를 차례로 내리친다. 원은 밖으로, 안쪽을 비운 고리는 보스 곁으로 들어가야 피한다.
// 마지막 낙찰은 빨간 예고로 알리고 막을 수 없다.
[CreateAssetMenu(fileName = "GavelAttackStyle", menuName = "Scriptable Objects/AttackStyle/Gavel")]
public class GavelAttackStyle : AttackStyle
{
    [Serializable]
    private struct Strike
    {
        // 0이면 가운데까지 차는 원, 0보다 크면 이 안쪽이 안전한 고리다.
        public float InnerRadius;
        public float OuterRadius;
    }

    [Header("내리치기")]
    [SerializeField] private Strike[] _strikes =
    {
        new Strike { InnerRadius = 0f, OuterRadius = 4f },
        new Strike { InnerRadius = 3f, OuterRadius = 40f },
        new Strike { InnerRadius = 0f, OuterRadius = 8f },
    };
    [SerializeField, Min(0f)] private float _warnDuration = 0.7f;
    [SerializeField, Min(0f)] private float _strikeInterval = 0.25f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.45f, 0.3f, 0.12f, 0.4f);
    [SerializeField] private Color _finalTelegraphColor = new Color(0.85f, 0.1f, 0.1f, 0.45f);
    // 고리 안쪽 안전한 자리를 예고 위에 덮어 보여 준다.
    [SerializeField] private Color _safeColor = new Color(0.95f, 0.92f, 0.82f, 0.85f);

    [Header("터짐")]
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();
    [SerializeField] private HitSettings _finalHit = new();
    [SerializeField] private ImpactSettings _finalImpact = new();

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, 3);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _strikes)
        {
            return;
        }

        for (int i = 0; i < _strikes.Length; i++)
        {
            bool isFinal = i == _strikes.Length - 1;
            Strike strike = _strikes[i];
            Vector3 center = context.Owner.Body.position;

            await WarnAsync(center, strike, isFinal ? _finalTelegraphColor : _telegraphColor, token);

            GameManager.Feedback.PlayImpact(isFinal ? _finalImpact : _impact, center);

            if (IsHit(context.Target, center, strike))
            {
                Vector3 hitPoint = context.Target.GetAimPoint(Vector3.zero);

                if (isFinal)
                {
                    context.Damage.StrikeThroughGuard(_finalHit, hitPoint);
                }
                else
                {
                    context.Damage.Strike(_hit, hitPoint, gaugeGain);
                }
            }

            if (false == isFinal)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_strikeInterval), cancellationToken: token);
            }
        }
    }

    // 안전한 자리는 예고가 깔린 뒤 그 위에 그려지도록 정렬 순서를 한 칸 올리고, 풀에 돌려줄 때 되돌린다.
    private async UniTask WarnAsync(Vector3 center, Strike strike, Color color, CancellationToken token)
    {
        SpriteRenderer safe = null;
        int safeOrder = 0;

        if (0f < strike.InnerRadius && null != _telegraphPrefab)
        {
            float diameter = strike.InnerRadius * 2f;
            safe = GameManager.Pool.SpawnSprite(_telegraphPrefab, center, 0f, new Vector2(diameter, diameter), _safeColor);
            safeOrder = safe.sortingOrder;
            safe.sortingOrder = safeOrder + 1;
        }

        try
        {
            await Telegraph.PlayCircleAsync(GameManager.Pool, _telegraphPrefab, center, strike.OuterRadius, color, _warnDuration, token);
        }
        finally
        {
            if (null != safe)
            {
                safe.sortingOrder = safeOrder;
                GameManager.Pool.DespawnSprite(safe);
            }
        }
    }

    private static bool IsHit(AttackTarget target, Vector3 center, Strike strike)
    {
        return target.IsInBlast(center, strike.OuterRadius) && (0f >= strike.InnerRadius || false == target.IsInBlast(center, strike.InnerRadius));
    }
}
