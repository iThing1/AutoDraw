using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 먹물 방울을 연달아 떨어뜨려, 플레이어가 움직일 자리를 앞질러 노린다. 떨어진 자리에는 밟으면 느려지는 웅덩이가 남는다.
[CreateAssetMenu(fileName = "InkDropAttackStyle", menuName = "Scriptable Objects/AttackStyle/InkDrop")]
public class InkDropAttackStyle : AttackStyle
{
    [Header("자리")]
    [SerializeField, Min(1)] private int _dropCount = 7;
    // 떨어지기까지의 시간 중 이 비율만큼 앞질러 노린다. 0이면 지금 자리, 1이면 지금 속도 그대로 갔을 자리다.
    [SerializeField, Range(0f, 1.5f)] private float _leadRatio = 1f;

    [Header("빨라짐")]
    [SerializeField, Min(0f)] private float _firstWarnDuration = 1f;
    [SerializeField, Min(0f)] private float _lastWarnDuration = 0.35f;
    [SerializeField, Min(0f)] private float _firstInterval = 0.5f;
    [SerializeField, Min(0f)] private float _lastInterval = 0.12f;
    [SerializeField] private Ease _speedUpEase = Ease.InQuad;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.35f);

    [Header("낙하")]
    [SerializeField, Min(0f)] private float _dropRadius = 1.4f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();
    // 떨어진 자리에 남는 먹물 자국. 비워 두면 남기지 않는다.
    [SerializeField] private HitFeedbackPreset _groundSplat;

    [Header("웅덩이")]
    [SerializeField] private SlowZone _puddlePrefab;
    [SerializeField] private SlowZoneSettings _puddle = new();

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, _dropCount * 2);
        }

        if (null != _puddlePrefab)
        {
            GameManager.Pool.Prewarm(_puddlePrefab.gameObject, _dropCount);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        List<UniTask> drops = new List<UniTask>(_dropCount);

        for (int i = 0; i < _dropCount; i++)
        {
            float progress = DOVirtual.EasedValue(0f, 1f, 1 < _dropCount ? i / (float)(_dropCount - 1) : 1f, _speedUpEase);
            float warnDuration = Mathf.Lerp(_firstWarnDuration, _lastWarnDuration, progress);
            Vector3 point = context.Target.GetPredictedPoint(warnDuration * _leadRatio);

            drops.Add(DropAsync(context, point, warnDuration, gaugeGain, token));

            if (i < _dropCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Lerp(_firstInterval, _lastInterval, progress)), cancellationToken: token);
            }
        }

        await UniTask.WhenAll(drops);
    }

    private async UniTask DropAsync(AttackContext context, Vector3 point, float warnDuration, float gaugeGain, CancellationToken token)
    {
        await Telegraph.PlayCircleAsync(_telegraphPrefab, point, _dropRadius, _telegraphColor, warnDuration, token);

        ImpactPlayer.Play(_impact, point);
        context.Damage.Blast(_hit, point, _dropRadius, gaugeGain);

        if (null != _groundSplat && null != context.PaintCanvas)
        {
            context.PaintCanvas.LeaveSplats(point, context.Damage.PickColor(), true, _groundSplat);
        }

        if (null != _puddlePrefab)
        {
            SlowZone puddle = GameManager.Pool.Spawn(_puddlePrefab.gameObject, point, Quaternion.identity).GetComponent<SlowZone>();
            puddle.PlayAsync(_puddle, token).Forget();
        }
    }
}
