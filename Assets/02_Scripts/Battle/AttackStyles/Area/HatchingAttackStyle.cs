using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 명암을 넣듯 목표 둘레에 평행한 빗금 여러 줄을 한꺼번에 긋는다. 묶음마다 빗금 방향이 돌아가 좁은 빈틈 자리도 바뀐다.
// 한 묶음은 같은 순간에 그어져, 줄이 겹쳐도 한 번만 맞고 막기 한 번으로 막는다.
[CreateAssetMenu(fileName = "HatchingAttackStyle", menuName = "Scriptable Objects/AttackStyle/Hatching")]
public class HatchingAttackStyle : AttackStyle
{
    [Header("빗금")]
    [SerializeField, Min(1)] private int _roundCount = 3;
    [SerializeField, Min(1)] private int _lineCount = 6;
    [SerializeField, Min(0.1f)] private float _lineWidth = 1.2f;
    // 줄 사이 빈틈. 몸이 겨우 들어갈 만큼 좁게 둔다.
    [SerializeField, Min(0.1f)] private float _gapWidth = 1.6f;
    [SerializeField, Min(1f)] private float _length = 30f;
    // 다음 묶음은 빗금 방향을 이 범위만큼 한쪽으로 돌린다(도).
    [SerializeField] private FloatRange _angleStep = new FloatRange(40f, 70f);

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.35f, 0.35f, 0.38f, 0.4f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.55f;

    [Header("긋기")]
    // 다 그은 빗금 그림. 1x1 네모 그림을 길이·굵기에 맞춰 늘린다.
    [SerializeField] private GameObject _strokePrefab;
    [SerializeField] private Color _strokeColor = new Color(0.02f, 0.02f, 0.03f, 1f);
    [SerializeField, Min(0f)] private float _lingerDuration = 0.25f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;
    // 앞 묶음을 긋고 다음 묶음의 예고를 시작하기까지.
    [SerializeField, Min(0f)] private float _roundInterval = 0.2f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, _lineCount * 2);
        }

        if (null != _strokePrefab)
        {
            GameManager.Pool.Prewarm(_strokePrefab, _lineCount * 2);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        float angle = UnityEngine.Random.Range(0f, 180f);
        float turn = 0.5f > UnityEngine.Random.value ? -1f : 1f;
        List<Vector3> centers = new List<Vector3>(_lineCount);

        for (int round = 0; round < _roundCount; round++)
        {
            await PlayRoundAsync(context, angle, centers, gaugeGain, token);

            angle += turn * _angleStep.PickRandom();

            if (round < _roundCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_roundInterval), cancellationToken: token);
            }
        }
    }

    // 줄 묶음은 쏘는 순간의 목표 자리를 가운데 두되, 통째로 무작위만큼 옆으로 밀어 목표가 늘 빈틈이나 줄 위에 오지 않게 한다.
    private async UniTask PlayRoundAsync(AttackContext context, float angle, List<Vector3> centers, float gaugeGain, CancellationToken token)
    {
        Vector3 target = context.Target.GetAimPoint(Vector3.zero);
        Vector3 along = Geometry2D.ToDirection(angle);
        Vector3 side = new Vector3(-along.y, along.x, 0f);
        float spacing = _lineWidth + _gapWidth;
        float firstOffset = -(spacing * (_lineCount - 1)) * 0.5f + UnityEngine.Random.Range(-0.5f, 0.5f) * spacing;

        centers.Clear();

        for (int i = 0; i < _lineCount; i++)
        {
            centers.Add(target + side * (firstOffset + spacing * i));
        }

        List<UniTask> telegraphs = new List<UniTask>(_lineCount);

        for (int i = 0; i < centers.Count; i++)
        {
            telegraphs.Add(Telegraph.PlayLineAsync(_telegraphPrefab, centers[i], angle, _length, _lineWidth, _telegraphColor, _warnDuration, token));
        }

        await UniTask.WhenAll(telegraphs);

        ImpactPlayer.Play(_impact, target);
        StrikeIfTouching(context, centers, along, gaugeGain);

        for (int i = 0; i < centers.Count; i++)
        {
            ShowStrokeAsync(centers[i], angle, token).Forget();
        }
    }

    private void StrikeIfTouching(AttackContext context, List<Vector3> centers, Vector3 along, float gaugeGain)
    {
        AttackTarget target = context.Target;
        Vector3 halfLength = along * (_length * 0.5f);
        float radius = _lineWidth * 0.5f;

        for (int i = 0; i < centers.Count; i++)
        {
            if (target.IsTouchingSegment(centers[i] - halfLength, centers[i] + halfLength, radius))
            {
                context.Damage.Strike(_hit, target.GetAimPoint(Vector3.zero), gaugeGain);
                return;
            }
        }
    }

    // 그은 빗금은 판정과 상관없이 잠깐 남았다가 옅어지며 사라진다. 다음 묶음을 기다리게 하지 않도록 따로 돌린다.
    private async UniTaskVoid ShowStrokeAsync(Vector3 center, float angle, CancellationToken token)
    {
        if (null == _strokePrefab)
        {
            return;
        }

        SpriteRenderer stroke = PooledSprite.Spawn(_strokePrefab, center, angle, new Vector2(_length, _lineWidth), _strokeColor);

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_lingerDuration), cancellationToken: token);

            await stroke.DOFade(0f, _fadeDuration)
                .ToUniTaskKillOnCancel(token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            PooledSprite.Despawn(stroke);
        }
    }
}
