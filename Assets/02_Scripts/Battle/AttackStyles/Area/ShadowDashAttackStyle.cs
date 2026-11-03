using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 종이를 가로지르는 길 몇 개를 한꺼번에 예고하고, 먹 그림자가 차례로 그 길을 따라 돌진한다.
[CreateAssetMenu(fileName = "ShadowDashAttackStyle", menuName = "Scriptable Objects/AttackStyle/ShadowDash")]
public class ShadowDashAttackStyle : AttackStyle
{
    [Header("길")]
    [SerializeField, Min(1)] private int _dashCount = 3;
    [SerializeField, Min(1f)] private float _length = 90f;
    // 길이 목표 자리에서 옆으로 비켜 지나는 거리. 0이면 늘 목표를 정확히 지난다.
    [SerializeField, Min(0f)] private float _offsetRange = 3f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.3f);
    // 첫 그림자가 출발할 때까지. 뒤 그림자는 돌진 간격만큼 길의 예고가 더 길다.
    [SerializeField, Min(0f)] private float _warnDuration = 0.8f;

    [Header("돌진")]
    [SerializeField] private ShadowRunSettings _shadow = new();
    [SerializeField, Min(0f)] private float _dashInterval = 0.35f;

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, _dashCount * 2);
        }

        if (null != _shadow.Prefab)
        {
            GameManager.Pool.Prewarm(_shadow.Prefab, _dashCount);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        List<UniTask> dashes = ListPool<UniTask>.Get();
        Vector3 targetPoint = context.Target.GetAimPoint(Vector3.zero);

        try
        {
            for (int i = 0; i < _dashCount; i++)
            {
                LinePlacement line = LinePlacement.PickNear(targetPoint, _offsetRange);

                dashes.Add(DashAsync(context, line, _warnDuration + _dashInterval * i, gaugeGain, token));
            }

            await UniTask.WhenAll(dashes);
        }
        finally
        {
            ListPool<UniTask>.Release(dashes);
        }
    }

    private async UniTask DashAsync(AttackContext context, LinePlacement line, float warnDuration, float gaugeGain, CancellationToken token)
    {
        await Telegraph.PlayLineAsync(_telegraphPrefab, line.Center, line.Angle, _length, _shadow.Diameter, _telegraphColor, warnDuration, token);

        Polyline path = line.ToPath(_length, 0.5f > Random.value);

        await ShadowRun.RunAsync(context, _shadow, path, gaugeGain, token);
    }
}
