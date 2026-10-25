using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 위작에 서명하듯 목표 근처를 가로지르는 구불구불한 획을 연필로 먼저 그어 보이고, 펜 끝이 그 획을 따라 먹 선을 남긴다.
// 남은 먹 선은 잠시 동안 닿으면 맞아 판을 갈라 놓는다. 펜 끝과 남은 선을 합쳐 한 번만 맞는다.
[CreateAssetMenu(fileName = "ScribbleAttackStyle", menuName = "Scriptable Objects/AttackStyle/Scribble")]
public class ScribbleAttackStyle : AttackStyle
{
    private const float FULL_TURN = Mathf.PI * 2f;

    [Header("획")]
    [SerializeField, Min(1f)] private float _length = 26f;
    // 획의 가운데 줄이 목표 자리에서 옆으로 비켜 지나는 거리.
    [SerializeField, Min(0f)] private float _offsetRange = 2f;
    // 가운데 줄에서 좌우로 구불거리는 폭과, 획 전체에 들어가는 굽이 수.
    [SerializeField, Min(0f)] private float _amplitude = 3f;
    [SerializeField, Min(0.1f)] private float _waveCount = 2.5f;
    [SerializeField, Min(2)] private int _pointCount = 48;

    [Header("예고")]
    // 월드 좌표를 쓰는 LineRenderer 프리팹(03_Prefabs/Effects/InkPath).
    [SerializeField] private LineRenderer _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.35f, 0.35f, 0.38f, 0.4f);
    [SerializeField, Min(0f)] private float _sketchDuration = 0.45f;
    [SerializeField, Min(0f)] private float _warnDuration = 0.6f;

    [Header("긋기")]
    [SerializeField] private GameObject _penPrefab;
    [SerializeField] private Color _penColor = new Color(0.02f, 0.02f, 0.03f, 1f);
    [SerializeField, Min(0.1f)] private float _penDiameter = 1.2f;
    [SerializeField] private LineRenderer _strokePrefab;
    [SerializeField] private Color _strokeColor = new Color(0.02f, 0.02f, 0.03f, 1f);
    [SerializeField, Min(0.05f)] private float _strokeWidth = 0.8f;
    [SerializeField, Min(0.01f)] private float _drawDuration = 0.7f;
    [SerializeField] private Ease _drawEase = Ease.InQuad;
    [SerializeField] private HitSettings _hit = new();

    [Header("남는 선")]
    [SerializeField, Min(0f)] private float _lingerDuration = 1.5f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab.gameObject, 2);
        }

        if (null != _strokePrefab)
        {
            GameManager.Pool.Prewarm(_strokePrefab.gameObject, 1);
        }

        if (null != _penPrefab)
        {
            GameManager.Pool.Prewarm(_penPrefab, 1);
        }
    }

    // 획은 쏘는 순간에 정한다. 예고를 본 뒤 획에서 벗어나면 피한다.
    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        Polyline path = BuildPath(context.Target.GetAimPoint(Vector3.zero));

        await Telegraph.PlayPathAsync(_telegraphPrefab, path, _strokeWidth, _telegraphColor, _sketchDuration, _warnDuration, token);

        if (null == _strokePrefab)
        {
            return;
        }

        LineRenderer stroke = PathLine.Spawn(_strokePrefab, _strokeWidth, _strokeColor);

        try
        {
            bool hasStruck = await DrawAsync(context, path, stroke, gaugeGain, token);

            await LingerAsync(context, path, hasStruck, gaugeGain, token);

            await DOVirtual.Float(_strokeColor.a, 0f, _fadeDuration, alpha => PathLine.SetColor(stroke, new Color(_strokeColor.r, _strokeColor.g, _strokeColor.b, alpha)))
                .SetTarget(stroke)
                .ToUniTaskKillOnCancel(token);
        }
        finally
        {
            PathLine.Despawn(stroke);
        }
    }

    // 가운데 줄을 따라 사인 곡선으로 구불거리는 꺾은선. 양 끝 중 어느 쪽에서 긋기 시작할지는 무작위다.
    private Polyline BuildPath(Vector3 target)
    {
        LinePlacement line = LinePlacement.PickNear(target, _offsetRange);
        Vector3 side = new Vector3(-line.Along.y, line.Along.x, 0f);
        float phase = Random.Range(0f, FULL_TURN);
        float direction = 0.5f > Random.value ? -1f : 1f;
        List<Vector3> points = new List<Vector3>(_pointCount);

        for (int i = 0; i < _pointCount; i++)
        {
            float t = i / (float)(_pointCount - 1);
            float along = (t - 0.5f) * _length * direction;
            float lateral = Mathf.Sin(t * _waveCount * FULL_TURN + phase) * _amplitude;

            points.Add(line.Center + line.Along * along + side * lateral);
        }

        return new Polyline(points);
    }

    // 펜 끝이 처음엔 천천히, 뒤로 갈수록 빠르게 획을 따라가며 먹 선을 그어 나간다. 펜 끝에 닿았으면 true를 돌려준다.
    private async UniTask<bool> DrawAsync(AttackContext context, Polyline path, LineRenderer stroke, float gaugeGain, CancellationToken token)
    {
        AttackTarget target = context.Target;
        SpriteRenderer pen = null != _penPrefab ? PooledSprite.Spawn(_penPrefab, path.Points[0], 0f, new Vector2(_penDiameter, _penDiameter), _penColor) : null;
        float radius = Mathf.Max(_penDiameter, _strokeWidth) * 0.5f;
        Vector3 previous = path.Points[0];
        float elapsed = 0f;
        int segment = 0;
        bool hasStruck = false;

        try
        {
            while (elapsed < _drawDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed = Mathf.Min(elapsed + Time.deltaTime, _drawDuration);

                float distance = path.Length * DOVirtual.EasedValue(0f, 1f, elapsed / _drawDuration, _drawEase);
                Vector3 tip = path.GetPointAt(distance, ref segment);

                PathLine.Reveal(stroke, path, segment, tip);

                if (null != pen)
                {
                    pen.transform.position = tip;
                }

                if (false == hasStruck && target.IsTouchingSegment(previous, tip, radius))
                {
                    hasStruck = true;
                    context.Damage.Strike(_hit, target.GetAimPoint(Vector3.zero), gaugeGain);
                }

                previous = tip;
            }
        }
        finally
        {
            PooledSprite.Despawn(pen);
        }

        return hasStruck;
    }

    // 다 그은 선은 남아 있는 동안 닿으면 맞는다. 펜 끝에 이미 맞았으면 다시 맞지 않는다.
    private async UniTask LingerAsync(AttackContext context, Polyline path, bool hasStruck, float gaugeGain, CancellationToken token)
    {
        AttackTarget target = context.Target;
        float elapsed = 0f;

        while (elapsed < _lingerDuration)
        {
            if (false == hasStruck && IsTouchingPath(target, path))
            {
                hasStruck = true;
                context.Damage.Strike(_hit, target.GetAimPoint(Vector3.zero), gaugeGain);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    private bool IsTouchingPath(AttackTarget target, Polyline path)
    {
        float radius = _strokeWidth * 0.5f;

        for (int i = 0; i < path.Points.Count - 1; i++)
        {
            if (target.IsTouchingSegment(path.Points[i], path.Points[i + 1], radius))
            {
                return true;
            }
        }

        return false;
    }
}
