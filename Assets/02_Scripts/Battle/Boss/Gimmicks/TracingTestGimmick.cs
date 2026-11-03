using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 따라 그리기 시험. 보스가 플레이어 자리를 지나는 커다란 8자 연필 선을 긋고, 플레이어는 정해진 시간 안에 그 선을 따라 달리며 칠한다.
// 지나간 자리에는 플레이어 물감이 찍혀 완성 그림에도 남는다. 연필 선은 시간이 갈수록 옅어져 남은 시간을 보여 주고, 그동안 보스는 조준탄을 쏜다.
// 선의 정해진 비율 이상을 칠하면 그로기로 이어진다. 못 채워도 큰 피해는 없다.
[CreateAssetMenu(fileName = "TracingTestGimmick", menuName = "Scriptable Objects/Boss/Gimmick/TracingTest")]
public class TracingTestGimmick : BossGimmick
{
    private const float FULL_TURN = Mathf.PI * 2f;

    [Header("밑그림")]
    // 월드 좌표를 쓰는 LineRenderer 프리팹(03_Prefabs/Effects/InkPath).
    [SerializeField] private LineRenderer _pathPrefab;
    [SerializeField] private Color _pathColor = new Color(0.35f, 0.35f, 0.38f, 0.7f);
    [SerializeField, Min(0.05f)] private float _pathWidth = 1f;
    // 8자의 가로 절반, 세로 전체 크기. 방향은 무작위로 돌린다.
    [SerializeField] private Vector2 _size = new Vector2(7f, 8f);
    [SerializeField, Min(8)] private int _pointCount = 96;
    [SerializeField] private GameObject _penPrefab;
    [SerializeField] private Color _penColor = new Color(0.35f, 0.35f, 0.38f, 1f);
    [SerializeField, Min(0.1f)] private float _penDiameter = 0.8f;
    [SerializeField, Min(0.01f)] private float _sketchDuration = 1.2f;

    [Header("칠하기")]
    [SerializeField, Min(0.1f)] private float _duration = 8f;
    // 몸이 선에서 이만큼 안에 지나가면 그 자리를 칠한 것으로 본다.
    [SerializeField, Min(0.1f)] private float _brushRadius = 1.2f;
    // 선을 이 간격으로 나눠 칸마다 칠했는지 센다.
    [SerializeField, Min(0.1f)] private float _checkpointSpacing = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _requiredCoverage = 0.7f;
    [SerializeField] private SplatStyle _paintStyle;
    [SerializeField, Min(0f)] private float _paintScale = 0.8f;
    // 다 옅어졌을 때 남는 선의 투명도 비율. 0이면 끝날 때 보이지 않는다.
    [SerializeField, Range(0f, 1f)] private float _endAlphaRatio = 0.2f;

    [Header("조준탄")]
    [SerializeField, Min(0.05f)] private float _shotInterval = 1.4f;
    [SerializeField, Min(1)] private int _shotCount = 3;
    [SerializeField, Range(0f, 180f)] private float _shotSpread = 20f;
    [SerializeField, Min(1f)] private float _range = 25f;
    [SerializeField] private ShotSettings _shot = new();

    [Header("결과")]
    [SerializeField] private ImpactSettings _successImpact = new();
    [SerializeField, Min(0f)] private float _fadeDuration = 0.4f;

    public override void Prewarm()
    {
        if (null != _pathPrefab)
        {
            GameManager.Pool.Prewarm(_pathPrefab.gameObject, 1);
        }

        if (null != _penPrefab)
        {
            GameManager.Pool.Prewarm(_penPrefab, 1);
        }
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        if (null == _pathPrefab)
        {
            Debug.LogError($"따라 그리기 시험 : {name}에 밑그림 프리팹이 지정되지 않았습니다.");
            return true;
        }

        Polyline path = BuildPath(context.Target);
        List<Vector3> checkpoints = ListPool<Vector3>.Get();
        LineRenderer line = PathLine.Spawn(_pathPrefab, _pathWidth, _pathColor);

        try
        {
            BuildCheckpoints(path, checkpoints);

            await SketchAsync(path, line, token);

            int coveredCount = await TraceAsync(context, line, checkpoints, token);
            bool isPassed = coveredCount >= Mathf.CeilToInt(checkpoints.Count * _requiredCoverage);

            if (isPassed)
            {
                ImpactPlayer.Play(_successImpact, context.Target.GetAimPoint(Vector3.zero));
            }

            Color current = line.startColor;

            await DOVirtual.Float(current.a, 0f, _fadeDuration, alpha => PathLine.SetColor(line, new Color(current.r, current.g, current.b, alpha)))
                .SetTarget(line)
                .ToUniTaskKillOnCancel(token);

            return isPassed;
        }
        finally
        {
            PathLine.Despawn(line);
            ListPool<Vector3>.Release(checkpoints);
        }
    }

    // 8자(제로노의 렘니스케이트)를 플레이어 자리가 한가운데 교차점에 오도록 놓는다. 플레이어는 선 위에서 시작한다.
    // 판 가장자리에서는 점을 플레이어 이동 범위 안으로 당겨, 닿을 수 없는 칸이 생기지 않게 한다.
    private Polyline BuildPath(AttackTarget target)
    {
        Vector3 center = target.FollowTransform.position;
        UnitMover mover = target.Unit.Mover;
        float angle = UnityEngine.Random.Range(0f, 180f) * Mathf.Deg2Rad;
        float flip = 0.5f > UnityEngine.Random.value ? -1f : 1f;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        List<Vector3> points = new List<Vector3>(_pointCount + 1);

        for (int i = 0; i <= _pointCount; i++)
        {
            float t = i / (float)_pointCount * FULL_TURN;
            float x = Mathf.Sin(t) * _size.x;
            float y = Mathf.Sin(t) * Mathf.Cos(t) * _size.y * flip;
            Vector3 point = center + new Vector3(x * cos - y * sin, x * sin + y * cos, 0f);

            points.Add(null != mover ? mover.ClampPosition(point) : point);
        }

        return new Polyline(points);
    }

    private void BuildCheckpoints(Polyline path, List<Vector3> checkpoints)
    {
        int count = Mathf.Max(1, Mathf.FloorToInt(path.Length / _checkpointSpacing));
        int segment = 0;

        for (int i = 0; i < count; i++)
        {
            checkpoints.Add(path.GetPointAt(path.Length * (i + 0.5f) / count, ref segment));
        }
    }

    // 펜 끝이 선을 처음부터 끝까지 그어 보인다.
    private async UniTask SketchAsync(Polyline path, LineRenderer line, CancellationToken token)
    {
        SpriteRenderer pen = null != _penPrefab ? PooledSprite.Spawn(_penPrefab, path.Points[0], 0f, new Vector2(_penDiameter, _penDiameter), _penColor) : null;
        float elapsed = 0f;
        int segment = 0;

        try
        {
            while (true)
            {
                Vector3 tip = path.GetPointAt(path.Length * (elapsed / _sketchDuration), ref segment);

                PathLine.Reveal(line, path, segment, tip);

                if (null != pen)
                {
                    pen.transform.position = tip;
                }

                if (elapsed >= _sketchDuration)
                {
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed = Mathf.Min(elapsed + Time.deltaTime, _sketchDuration);
            }
        }
        finally
        {
            PooledSprite.Despawn(pen);
        }
    }

    // 몸이 지나간 칸을 칠하고 칠한 칸 수를 돌려준다. 선은 시간이 갈수록 옅어지고, 보스는 정해진 간격으로 조준탄을 쏜다.
    private async UniTask<int> TraceAsync(AttackContext context, LineRenderer line, List<Vector3> checkpoints, CancellationToken token)
    {
        AttackTarget target = context.Target;
        PaintCanvas canvas = context.PaintCanvas;
        PaintPalette palette = target.Unit.Data.Palette;
        bool[] isCovered = new bool[checkpoints.Count];
        float elapsed = 0f;
        float nextShot = _shotInterval;
        int coveredCount = 0;

        while (elapsed < _duration && false == target.IsDefeated)
        {
            Vector3 body = target.FollowTransform.position;

            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (isCovered[i] || Vector2.Distance(body, checkpoints[i]) > _brushRadius)
                {
                    continue;
                }

                isCovered[i] = true;
                coveredCount++;

                if (null != canvas && null != _paintStyle)
                {
                    Color color = null != palette ? palette.Pick(coveredCount) : Color.white;
                    canvas.LeaveSplat(checkpoints[i], _paintStyle, color, false, _paintScale);
                }
            }

            if (elapsed >= nextShot)
            {
                nextShot += _shotInterval;
                FireShots(context, token);
            }

            float alpha = _pathColor.a * Mathf.Lerp(1f, _endAlphaRatio, elapsed / _duration);
            PathLine.SetColor(line, new Color(_pathColor.r, _pathColor.g, _pathColor.b, alpha));

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }

        return coveredCount;
    }

    private void FireShots(AttackContext context, CancellationToken token)
    {
        Vector3 origin = context.Owner.Body.position;
        float centerAngle = Geometry2D.ToAngle(context.Target.GetAimPoint(Vector3.zero) - origin);
        float step = 1 < _shotCount ? _shotSpread / (_shotCount - 1) : 0f;
        float startAngle = 1 < _shotCount ? centerAngle - _shotSpread * 0.5f : centerAngle;

        for (int i = 0; i < _shotCount; i++)
        {
            context.Launcher.FireShotToward(_shot, origin, Geometry2D.ToDirection(startAngle + step * i), _range, 0f, token);
        }
    }
}
