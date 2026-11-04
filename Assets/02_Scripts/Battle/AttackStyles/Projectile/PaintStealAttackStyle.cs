using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 목표 둘레의 플레이어 물감을 빨아들여 보스에게 모은 뒤, 빼앗은 색 그대로 유도탄으로 되쏜다. 많이 칠한 자리에 서 있을수록 많이 날아온다.
// 빨린 물감은 캔버스에서 지워져 클리어 그림에서도 사라진다.
[CreateAssetMenu(fileName = "PaintStealAttackStyle", menuName = "Scriptable Objects/AttackStyle/PaintSteal")]
public class PaintStealAttackStyle : AttackStyle
{
    [Header("빨아들이기")]
    [SerializeField, Min(0.1f)] private float _stealRadius = 6f;
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.35f, 0.35f, 0.38f, 0.4f);
    // 예고 원이 다 차는 순간 원 안의 물감을 빨아들인다. 그전에 원 밖으로 나가면 덜 빨린다.
    [SerializeField, Min(0f)] private float _warnDuration = 0.6f;
    // 빨린 물감이 보스에게 모이는 비행. 맞히지 않는다.
    [SerializeField] private ShotSettings _gatherShot = new();
    // 다 모인 뒤 되쏘기 전에 숨을 고르는 시간.
    [SerializeField, Min(0f)] private float _chargeDuration = 0.4f;
    [SerializeField] private ImpactSettings _chargeImpact = new();

    [Header("되쏘기")]
    // 빨아들인 자국 수만큼 쏘되 이 범위로 자른다. 빨린 물감이 없어도 최소만큼은 쏜다.
    [SerializeField] private IntRange _shotCount = new IntRange(4, 12);
    [SerializeField, Min(0f)] private float _shotInterval = 0.08f;
    [SerializeField] private ShotSettings _returnShot = new();

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        Vector3 center = context.Target.GetAimPoint(Vector3.zero);

        await Telegraph.PlayCircleAsync(GameManager.Pool, _telegraphPrefab, center, _stealRadius, _telegraphColor, _warnDuration, token);

        List<PaintMark> stolen = ListPool<PaintMark>.Get();
        List<Color> colors = ListPool<Color>.Get();

        try
        {
            if (null != context.PaintCanvas)
            {
                context.PaintCanvas.RemovePaintAround(center, _stealRadius, stolen);
            }

            await GatherAsync(context, center, stolen, colors, token);

            GameManager.Feedback.PlayImpact(_chargeImpact, context.Owner.Body.position);
            await UniTask.Delay(TimeSpan.FromSeconds(_chargeDuration), cancellationToken: token);

            for (int i = 0; i < colors.Count; i++)
            {
                context.Launcher.FireShot(_returnShot, context.Owner.Body.position, Vector3.zero, colors[i], gaugeGain, token);

                if (i < colors.Count - 1)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_shotInterval), cancellationToken: token);
                }
            }
        }
        finally
        {
            ListPool<PaintMark>.Release(stolen);
            ListPool<Color>.Release(colors);
        }
    }

    // 빨린 자국마다 보스에게 날아오는 방울을 띄우고, 되쏠 색을 colors에 담는다. 자국이 쏠 수보다 많으면 고르게 건너뛰어 고른다.
    // 빨린 물감이 모자라면 원 안 아무 데서나 보스 물감 색으로 채운다.
    private async UniTask GatherAsync(AttackContext context, Vector3 center, List<PaintMark> stolen, List<Color> colors, CancellationToken token)
    {
        int count = Mathf.Clamp(stolen.Count, _shotCount.Min, _shotCount.Max);
        List<UniTask<bool>> gathers = ListPool<UniTask<bool>>.Get();
        Vector3 destination = context.Owner.Body.position;

        try
        {
            for (int i = 0; i < count; i++)
            {
                PaintMark source = PickSource(stolen, i, count);
                Vector3 start = null != source ? source.Position : center + (Vector3)(UnityEngine.Random.insideUnitCircle * _stealRadius);
                Color color = null != source ? source.Color : context.Damage.PickColor();

                colors.Add(color);
                gathers.Add(context.Launcher.FlyToPointAsync(_gatherShot, start, destination, color, token));
            }

            await UniTask.WhenAll(gathers);
        }
        finally
        {
            ListPool<UniTask<bool>>.Release(gathers);
        }
    }

    // index번째 방울이 날아올 자국. 모자라면 null이다.
    private static PaintMark PickSource(List<PaintMark> stolen, int index, int count)
    {
        if (stolen.Count >= count)
        {
            return stolen[index * stolen.Count / count];
        }

        return index < stolen.Count ? stolen[index] : null;
    }
}
