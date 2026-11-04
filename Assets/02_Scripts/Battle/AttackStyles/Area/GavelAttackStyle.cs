using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 보스 둘레로 점점 넓어지는 원을 내리친다. 마지막 낙찰은 빨간 예고로 알리고 막을 수 없다.
[CreateAssetMenu(fileName = "GavelAttackStyle", menuName = "Scriptable Objects/AttackStyle/Gavel")]
public class GavelAttackStyle : AttackStyle
{
    [Header("내리치기")]
    [SerializeField] private float[] _radii = { 3f, 5f, 8f };
    [SerializeField, Min(0f)] private float _warnDuration = 0.7f;
    [SerializeField, Min(0f)] private float _strikeInterval = 0.25f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.45f, 0.3f, 0.12f, 0.4f);
    [SerializeField] private Color _finalTelegraphColor = new Color(0.85f, 0.1f, 0.1f, 0.45f);

    [Header("터짐")]
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();
    [SerializeField] private HitSettings _finalHit = new();
    [SerializeField] private ImpactSettings _finalImpact = new();

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, 2);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _radii)
        {
            return;
        }

        for (int i = 0; i < _radii.Length; i++)
        {
            bool isFinal = i == _radii.Length - 1;
            float radius = _radii[i];
            Vector3 center = context.Owner.Body.position;

            if (isFinal)
            {
                await Telegraph.PlayCircleAsync(GameManager.Pool, _telegraphPrefab, center, radius, _finalTelegraphColor, _warnDuration, token);

                GameManager.Feedback.PlayImpact(_finalImpact, center);

                if (context.Target.IsInBlast(center, radius))
                {
                    context.Damage.StrikeThroughGuard(_finalHit, context.Target.GetAimPoint(Vector3.zero));
                }

                break;
            }

            await Telegraph.PlayCircleAsync(GameManager.Pool, _telegraphPrefab, center, radius, _telegraphColor, _warnDuration, token);

            GameManager.Feedback.PlayImpact(_impact, center);
            context.Damage.Blast(_hit, center, radius, gaugeGain);

            await UniTask.Delay(TimeSpan.FromSeconds(_strikeInterval), cancellationToken: token);
        }
    }
}
