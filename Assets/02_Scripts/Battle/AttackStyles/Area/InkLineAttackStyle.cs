using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 화면을 가로지르는 예고선을 긋고, 다 차면 굵은 붓 획이 그어진다. 몇 줄씩 한꺼번에 묶어 차례로 그어 공간을 가른다.
// 한 묶음은 같은 순간에 그어져 막기 한 번으로 막을 수 있고, 묶음 사이는 막기를 다시 쓸 수 있을 만큼 띄운다.
// 줄은 목표 근처를 지나도록 긋고, 방향은 무작위라 선과 선 사이 빈자리를 찾아 움직이게 한다.
[CreateAssetMenu(fileName = "InkLineAttackStyle", menuName = "Scriptable Objects/AttackStyle/InkLine")]
public class InkLineAttackStyle : AttackStyle
{
    [Header("줄")]
    [SerializeField, Min(1)] private int _volleyCount = 3;
    [SerializeField] private IntRange _linesPerVolley = new IntRange(2, 3);
    [SerializeField, Min(0f)] private float _volleyInterval = 0.75f;
    // 종이 끝까지 닿도록 넉넉히 잡는다.
    [SerializeField, Min(1f)] private float _length = 120f;
    [SerializeField, Min(0.1f)] private float _width = 1.6f;
    // 줄이 목표 자리에서 옆으로 비켜 지나는 거리. 0이면 늘 목표를 정확히 지난다.
    [SerializeField, Min(0f)] private float _offsetRange = 2.5f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.35f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.9f;

    [Header("획")]
    [SerializeField] private Color _strokeColor = new Color(0.05f, 0.05f, 0.06f, 1f);
    [SerializeField, Min(0f)] private float _strokeDuration = 0.12f;
    [SerializeField, Min(0f)] private float _lingerDuration = 0.35f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, _linesPerVolley.Max * 3);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        List<UniTask> lines = new List<UniTask>(_volleyCount * _linesPerVolley.Max);

        for (int volley = 0; volley < _volleyCount; volley++)
        {
            int lineCount = _linesPerVolley.PickRandom();

            for (int i = 0; i < lineCount; i++)
            {
                lines.Add(DrawLineAsync(context, gaugeGain, token));
            }

            if (volley < _volleyCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_volleyInterval), cancellationToken: token);
            }
        }

        await UniTask.WhenAll(lines);
    }

    // 예고를 시작하는 순간의 목표 자리를 기준으로 줄을 정해, 예고를 본 뒤 비켜서면 피한다.
    private async UniTask DrawLineAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        LinePlacement line = LinePlacement.PickNear(context.Target.GetAimPoint(Vector3.zero), _offsetRange);

        await Telegraph.PlayLineAsync(_telegraphPrefab, line.Center, line.Angle, _length, _width, _telegraphColor, _warnDuration, token);

        SpriteRenderer stroke = PooledSprite.Spawn(_telegraphPrefab, line.Center, line.Angle, new Vector2(_length, 0f), _strokeColor);

        try
        {
            await stroke.transform.DOScaleY(_width, _strokeDuration)
                .SetEase(Ease.OutQuad)
                .ToUniTaskKillOnCancel(token);

            ImpactPlayer.Play(_impact, GetClosestPoint(line.Center, line.Along, context.Target.GetAimPoint(Vector3.zero)));
            StrikeIfTouching(context, line.Center, line.Along, gaugeGain);

            await UniTask.Delay(TimeSpan.FromSeconds(_lingerDuration), cancellationToken: token);

            await stroke.DOFade(0f, _fadeDuration)
                .ToUniTaskKillOnCancel(token);
        }
        finally
        {
            PooledSprite.Despawn(stroke);
        }
    }

    // 획의 굵기 안에 몸이 걸쳐 있으면 맞는다. 방패를 들고 있으면 막힌다.
    private void StrikeIfTouching(AttackContext context, Vector3 center, Vector3 along, float gaugeGain)
    {
        Vector3 targetPoint = context.Target.GetAimPoint(Vector3.zero);

        if (context.Target.IsTouching(GetClosestPoint(center, along, targetPoint), _width * 0.5f))
        {
            context.Damage.Strike(_hit, targetPoint, gaugeGain);
        }
    }

    private Vector3 GetClosestPoint(Vector3 center, Vector3 along, Vector3 point)
    {
        Vector3 half = along * (_length * 0.5f);

        return Geometry2D.GetClosestPointOnSegment(center - half, center + half, point);
    }
}
