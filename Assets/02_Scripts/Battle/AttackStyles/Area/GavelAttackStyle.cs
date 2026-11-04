using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 경매 망치를 여러 번 내리친다. 내리칠 때마다 보스 둘레에 더 넓은 원이 퍼지고, 마지막 "낙찰"이 가장 넓고 세다.
// 원은 보스를 가운데에 두므로, 내리칠 때마다 한 걸음 더 물러나야 한다.
// 낙찰은 빨간 예고로 알리고 막을 수 없어, 막기만으로는 넘길 수 없다. 대시로 피하는 것은 그대로 통한다.
[CreateAssetMenu(fileName = "GavelAttackStyle", menuName = "Scriptable Objects/AttackStyle/Gavel")]
public class GavelAttackStyle : AttackStyle
{
    [Header("내리치기")]
    // 내리칠 때마다의 원 반지름. 개수만큼 내리친다.
    [SerializeField] private float[] _radii = { 3f, 5f, 8f };
    [SerializeField, Min(0f)] private float _warnDuration = 0.7f;
    // 앞 원이 터지고 다음 예고를 시작하기까지.
    [SerializeField, Min(0f)] private float _strikeInterval = 0.25f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.45f, 0.3f, 0.12f, 0.4f);
    // 막을 수 없는 낙찰의 예고 색. 다른 막을 수 없는 공격과 같은 빨강으로 둔다.
    [SerializeField] private Color _finalTelegraphColor = new Color(0.85f, 0.1f, 0.1f, 0.45f);

    [Header("터짐")]
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();
    // 마지막 한 번(낙찰)에만 쓰는 판정과 연출.
    [SerializeField] private HitSettings _finalHit = new();
    [SerializeField] private ImpactSettings _finalImpact = new();

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, 2);
        }
    }

    // 원의 가운데는 내리칠 때마다 보스의 지금 자리로 다시 잡는다.
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
                StrikeUnblockable(context, center, radius);
                break;
            }

            await Telegraph.PlayCircleAsync(GameManager.Pool, _telegraphPrefab, center, radius, _telegraphColor, _warnDuration, token);

            GameManager.Feedback.PlayImpact(_impact, center);
            context.Damage.Blast(_hit, center, radius, gaugeGain);

            await UniTask.Delay(TimeSpan.FromSeconds(_strikeInterval), cancellationToken: token);
        }
    }

    // 막기는 보지 않고 대시 회피만 본다.
    private void StrikeUnblockable(AttackContext context, Vector3 center, float radius)
    {
        AttackTarget target = context.Target;

        if (target.IsInBlast(center, radius) && false == target.TryDodge())
        {
            context.Damage.StrikeUnblockable(_finalHit, target.GetAimPoint(Vector3.zero));
        }
    }
}
