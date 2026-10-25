using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 목표 둘레에 네모 액자를 그리고, 테두리가 먹으로 차오르며 안쪽으로 조여 든다. 보스 반대쪽 변의 틈으로 빠져나가거나 대시로 테두리를 뚫는다.
// 다 조인 뒤 남은 안쪽이 터져, 가운데에 버티면 맞는다.
[CreateAssetMenu(fileName = "FrameTrapAttackStyle", menuName = "Scriptable Objects/AttackStyle/FrameTrap")]
public class FrameTrapAttackStyle : AttackStyle
{
    // 오른쪽, 위, 왼쪽, 아래 순서의 바깥 방향.
    private static readonly Vector3[] SIDE_NORMALS = { Vector3.right, Vector3.up, Vector3.left, Vector3.down };
    // 틈이 있는 변은 둘로 나뉘어 테두리 조각은 최대 다섯 개다.
    private const int MAX_EDGE_COUNT = 5;

    // 테두리 한 조각. 양 끝 점으로 그리고 판정한다.
    private readonly struct Edge
    {
        public readonly Vector3 From;
        public readonly Vector3 To;

        public Edge(Vector3 from, Vector3 to)
        {
            From = from;
            To = to;
        }

        public Vector3 Center { get { return (From + To) * 0.5f; } }
        public float Length { get { return Vector2.Distance(From, To); } }
        public float Angle { get { return Geometry2D.ToAngle(To - From); } }
    }

    // 되감기용 기록. 액자 가운데와 틈을 낸 변을 쥐고 있다.
    private sealed class Record : RewindRecord
    {
        private readonly FrameTrapAttackStyle _style;
        private readonly Vector3 _center;
        private readonly int _gapSide;

        public Record(FrameTrapAttackStyle style, Vector3 center, int gapSide)
        {
            _style = style;
            _center = center;
            _gapSide = gapSide;
        }

        public override UniTask ReplayAsync(AttackContext context, InkTint tint, float gaugeGain, CancellationToken token)
        {
            return _style.PlayAsync(context, _center, _gapSide, true, tint, gaugeGain, token);
        }
    }

    [Header("액자")]
    [SerializeField, Min(0.1f)] private float _startSize = 7f;
    [SerializeField, Min(0f)] private float _endSize = 3f;
    [SerializeField, Min(0f)] private float _gapWidth = 2.2f;
    [SerializeField, Min(0.01f)] private float _edgeWidth = 0.5f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.35f, 0.35f, 0.38f, 0.4f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.9f;

    [Header("조이기")]
    // 테두리 그림. 길이·굵기에 맞춰 늘리므로 1x1 네모 그림을 쓴다.
    [SerializeField] private GameObject _edgePrefab;
    [SerializeField] private Color _edgeColor = new Color(0.02f, 0.02f, 0.03f, 1f);
    [SerializeField, Min(0.01f)] private float _shrinkDuration = 1.6f;
    [SerializeField] private Ease _shrinkEase = Ease.InQuad;
    [SerializeField] private HitSettings _edgeHit = new();

    [Header("터짐")]
    [SerializeField] private HitSettings _burstHit = new();
    [SerializeField] private ImpactSettings _burstImpact = new();
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;

    public override void Prewarm()
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, MAX_EDGE_COUNT * 2);
        }

        if (null != _edgePrefab)
        {
            GameManager.Pool.Prewarm(_edgePrefab, MAX_EDGE_COUNT);
        }
    }

    public override UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        Vector3 center = context.Target.GetAimPoint(Vector3.zero);
        int gapSide = PickGapSide(center - context.Owner.Body.position);

        context.RewindLog.Add(new Record(this, center, gapSide));

        return PlayAsync(context, center, gapSide, false, default, gaugeGain, token);
    }

    // 되감으면(isReversed) 다 조인 크기로 예고한 뒤 안쪽이 먼저 터지고, 테두리가 조이던 길을 거꾸로 밟아 바깥으로 풀려 나간다.
    // 풀려 나가는 테두리는 틈 앞에 서 있거나 대시로 뚫어 피한다.
    private async UniTask PlayAsync(AttackContext context, Vector3 center, int gapSide, bool isReversed, InkTint tint, float gaugeGain,
        CancellationToken token)
    {
        List<Edge> edges = new List<Edge>(MAX_EDGE_COUNT);

        BuildEdges(center, isReversed ? _endSize : _startSize, gapSide, edges);
        await PlayTelegraphAsync(edges, tint.Apply(_telegraphColor), token);

        List<SpriteRenderer> sprites = new List<SpriteRenderer>(MAX_EDGE_COUNT);

        try
        {
            // 작은 액자는 틈 변이 사라져 조각이 적을 수 있어, 그림은 가장 큰 액자의 조각 수만큼 꺼내 둔다.
            BuildEdges(center, Mathf.Max(_startSize, _endSize), gapSide, edges);

            for (int i = 0; i < edges.Count; i++)
            {
                sprites.Add(PooledSprite.Spawn(_edgePrefab, edges[i].Center, edges[i].Angle, new Vector2(edges[i].Length, _edgeWidth), tint.Apply(_edgeColor)));
            }

            if (isReversed)
            {
                Burst(context, center, tint, gaugeGain);
            }

            await ShrinkAsync(context, center, gapSide, isReversed, edges, sprites, gaugeGain, token);

            if (false == isReversed)
            {
                Burst(context, center, tint, gaugeGain);
            }

            await FadeEdgesAsync(sprites, token);
        }
        finally
        {
            // 같은 네모 그림을 다른 공격의 예고도 꺼내 쓰므로, 숨긴 채로 돌려주지 않는다.
            for (int i = 0; i < sprites.Count; i++)
            {
                sprites[i].enabled = true;
                PooledSprite.Despawn(sprites[i]);
            }
        }
    }

    // 터진 뒤 남은 테두리는 바로 없애지 않고 옅어지게 한다. 숨긴 조각도 함께 옅어져 다시 보일 일은 없다.
    private async UniTask FadeEdgesAsync(List<SpriteRenderer> sprites, CancellationToken token)
    {
        List<UniTask> fades = new List<UniTask>(sprites.Count);

        for (int i = 0; i < sprites.Count; i++)
        {
            fades.Add(sprites[i].DOFade(0f, _fadeDuration).ToUniTaskKillOnCancel(token));
        }

        await UniTask.WhenAll(fades);
    }

    private async UniTask PlayTelegraphAsync(List<Edge> edges, Color telegraphColor, CancellationToken token)
    {
        List<UniTask> telegraphs = new List<UniTask>(edges.Count);

        for (int i = 0; i < edges.Count; i++)
        {
            telegraphs.Add(Telegraph.PlayLineAsync(_telegraphPrefab, edges[i].Center, edges[i].Angle, edges[i].Length, _edgeWidth, telegraphColor,
                _warnDuration, token));
        }

        await UniTask.WhenAll(telegraphs);
    }

    private void Burst(AttackContext context, Vector3 center, InkTint tint, float gaugeGain)
    {
        ImpactPlayer.Play(_burstImpact, center, tint.Apply(_burstImpact.EffectColor));
        context.Damage.Blast(_burstHit, center, _endSize * 0.5f, gaugeGain);
    }

    // 틈이 있는 변이 조여 들며 틈이 사라질 수 있어, 조각 수가 줄면 남는 그림은 숨긴다.
    // 되감으면 같은 크기 변화를 시간만 거꾸로 밟는다. 꺼내 둔 그림이 한 프레임도 다른 크기로 보이지 않게, 기다리기 전에 먼저 놓는다.
    private async UniTask ShrinkAsync(AttackContext context, Vector3 center, int gapSide, bool isReversed, List<Edge> edges,
        List<SpriteRenderer> sprites, float gaugeGain, CancellationToken token)
    {
        AttackTarget target = context.Target;
        float elapsed = 0f;
        bool hasStruck = false;

        while (true)
        {
            float progress = elapsed / _shrinkDuration;
            float size = DOVirtual.EasedValue(_startSize, _endSize, isReversed ? 1f - progress : progress, _shrinkEase);
            BuildEdges(center, size, gapSide, edges);

            for (int i = 0; i < sprites.Count; i++)
            {
                bool isVisible = i < edges.Count;
                sprites[i].enabled = isVisible;

                if (isVisible)
                {
                    PlaceEdge(sprites[i].transform, edges[i]);
                }
            }

            if (false == hasStruck && IsTouchingAny(target, edges))
            {
                hasStruck = true;
                context.Damage.Strike(_edgeHit, target.GetAimPoint(Vector3.zero), gaugeGain);
            }

            if (elapsed >= _shrinkDuration)
            {
                break;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed = Mathf.Min(elapsed + Time.deltaTime, _shrinkDuration);
        }
    }

    private void PlaceEdge(Transform edge, Edge placement)
    {
        edge.SetPositionAndRotation(placement.Center, Quaternion.Euler(0f, 0f, placement.Angle));
        edge.localScale = new Vector3(placement.Length, _edgeWidth, 1f);
    }

    private bool IsTouchingAny(AttackTarget target, List<Edge> edges)
    {
        float radius = _edgeWidth * 0.5f;

        for (int i = 0; i < edges.Count; i++)
        {
            if (target.IsTouchingSegment(edges[i].From, edges[i].To, radius))
            {
                return true;
            }
        }

        return false;
    }

    // 변 길이를 테두리 굵기만큼 늘려 모서리가 비지 않게 한다. 틈 변은 틈 양쪽 두 조각이고, 틈보다 좁아지면 없어진다.
    private void BuildEdges(Vector3 center, float size, int gapSide, List<Edge> edges)
    {
        edges.Clear();

        float half = size * 0.5f;
        float reach = half + _edgeWidth * 0.5f;
        float gapHalf = _gapWidth * 0.5f;

        for (int side = 0; side < SIDE_NORMALS.Length; side++)
        {
            Vector3 normal = SIDE_NORMALS[side];
            Vector3 tangent = new Vector3(-normal.y, normal.x, 0f);
            Vector3 sideCenter = center + normal * half;

            if (side != gapSide)
            {
                edges.Add(new Edge(sideCenter - tangent * reach, sideCenter + tangent * reach));
                continue;
            }

            if (reach > gapHalf)
            {
                edges.Add(new Edge(sideCenter - tangent * reach, sideCenter - tangent * gapHalf));
                edges.Add(new Edge(sideCenter + tangent * gapHalf, sideCenter + tangent * reach));
            }
        }
    }

    // 보스에게서 멀어지는 쪽 변에 틈을 내, 빠져나가면 보스와 거리가 벌어지게 한다.
    private static int PickGapSide(Vector3 awayFromOwner)
    {
        int bestSide = 0;
        float bestDot = float.NegativeInfinity;

        for (int side = 0; side < SIDE_NORMALS.Length; side++)
        {
            float dot = Vector2.Dot(SIDE_NORMALS[side], awayFromOwner);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestSide = side;
            }
        }

        return bestSide;
    }
}
