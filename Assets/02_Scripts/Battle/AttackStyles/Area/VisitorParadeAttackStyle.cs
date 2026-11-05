using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 그림자 관람객 행렬. 판 끝에서 끝까지 관람 동선 몇 줄을 깔고, 그 위로 관람객 무리가 쉬지 않고 오간다.
// 동선 사이에 머물거나 무리 사이 빈틈을 골라 건너야 한다. 동선이 깔린 동안 보스가 다른 패턴을 이어 가도록, 끝날 때까지 기다리지 않는 패턴으로 쓴다.
[CreateAssetMenu(fileName = "VisitorParadeAttackStyle", menuName = "Scriptable Objects/AttackStyle/VisitorParade")]
public class VisitorParadeAttackStyle : AttackStyle
{
    // 시작 예상 개수. 판 길이를 알기 전에 풀을 채우는 데만 쓴다.
    private const int PREWARM_VISITORS_PER_LANE = 24;

    private struct Lane
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float Length;
        public SpriteRenderer Guide;
    }

    private struct Visitor
    {
        public SpriteRenderer Body;
        public SpriteRenderer Head;
        public int Lane;
        // 처음 순간 동선 시작점에서 떨어진 거리. 걸은 거리를 더해 동선 길이로 되감는다.
        public float Start;
    }

    [Header("동선")]
    [SerializeField, Min(1)] private int _laneCount = 3;
    [SerializeField, Min(1f)] private float _laneSpacing = 5f;
    [SerializeField, Min(0.1f)] private float _walkSpeed = 4f;
    // 동선이 먼저 깔리고 관람객이 서서히 드러나는 동안은 맞지 않는다.
    [SerializeField, Min(0f)] private float _guideDuration = 1f;
    [SerializeField, Min(0.1f)] private float _duration = 8f;
    [SerializeField, Min(0.01f)] private float _fadeDuration = 0.4f;
    [SerializeField] private GameObject _guidePrefab;
    [SerializeField, Min(0.1f)] private float _guideWidth = 1.6f;
    [SerializeField] private Color _guideColor = new Color(0.62f, 0.48f, 0.2f, 0.18f);

    [Header("무리")]
    [SerializeField, Min(1)] private int _minGroupSize = 2;
    [SerializeField, Min(1)] private int _maxGroupSize = 4;
    [SerializeField, Min(0.3f)] private float _visitorSpacing = 1.1f;
    // 무리 사이 빈틈. 플레이어가 동선을 건너는 동안 무리가 걸어오는 거리보다 넉넉해야 지나갈 수 있다.
    [SerializeField] private FloatRange _groupGap = new FloatRange(4.5f, 7f);
    // 동선 끝에서 되감길 때 순간이동이 보이지 않도록 끝 가까이에서 흐려진다.
    [SerializeField, Min(0.01f)] private float _edgeFade = 1.5f;

    [Header("그림")]
    [SerializeField] private GameObject _bodyPrefab;
    [SerializeField] private GameObject _headPrefab;
    [SerializeField] private Vector2 _bodySize = new Vector2(0.6f, 1.1f);
    [SerializeField, Min(0.05f)] private float _headSize = 0.45f;
    [SerializeField] private Color _visitorColor = new Color(0.08f, 0.08f, 0.11f, 0.85f);
    [SerializeField, Min(0f)] private float _bobHeight = 0.06f;
    [SerializeField, Min(0f)] private float _bobSpeed = 10f;

    [Header("부딪힘")]
    [SerializeField, Min(0.05f)] private float _visitorRadius = 0.4f;
    [SerializeField, Min(0.05f)] private float _touchInterval = 0.6f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    public override void Prewarm()
    {
        int count = _laneCount * PREWARM_VISITORS_PER_LANE;

        if (null != _bodyPrefab)
        {
            GameManager.Pool.Prewarm(_bodyPrefab, count);
        }

        if (null != _headPrefab)
        {
            GameManager.Pool.Prewarm(_headPrefab, count);
        }

        if (null != _guidePrefab)
        {
            GameManager.Pool.Prewarm(_guidePrefab, _laneCount);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null == _bodyPrefab || null == _headPrefab || null == _guidePrefab || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            return;
        }

        AttackTarget target = context.Target;
        List<Lane> lanes = ListPool<Lane>.Get();
        List<Visitor> visitors = ListPool<Visitor>.Get();

        try
        {
            BuildLanes(area, target.FollowTransform.position, lanes);
            SpawnVisitors(lanes, visitors);

            float streamEnd = _guideDuration + _duration;
            float total = streamEnd + _fadeDuration;
            float elapsed = 0f;
            float nextHitTime = 0f;

            while (elapsed < total && false == target.IsDefeated)
            {
                float alpha = GetPhaseAlpha(elapsed, streamEnd);

                SetGuideAlpha(lanes, alpha);
                MoveVisitors(lanes, visitors, elapsed, alpha);

                bool isStreaming = elapsed >= _guideDuration && elapsed < streamEnd;

                if (isStreaming && elapsed >= nextHitTime && IsTouchingAny(target, visitors))
                {
                    nextHitTime = elapsed + _touchInterval;

                    Vector3 hitPoint = target.GetAimPoint(Vector3.zero);
                    GameManager.Feedback.PlayImpact(_impact, hitPoint);
                    context.Damage.Strike(_hit, hitPoint, gaugeGain);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }
        }
        finally
        {
            for (int i = 0; i < visitors.Count; i++)
            {
                GameManager.Pool.DespawnSprite(visitors[i].Body);
                GameManager.Pool.DespawnSprite(visitors[i].Head);
            }

            for (int i = 0; i < lanes.Count; i++)
            {
                GameManager.Pool.DespawnSprite(lanes[i].Guide);
            }

            ListPool<Lane>.Release(lanes);
            ListPool<Visitor>.Release(visitors);
        }
    }

    // 가운데 동선이 플레이어를 지나도록 깔되, 줄 전체가 판 안에 들도록 가운데를 안쪽으로 당긴다. 동선마다 걷는 쪽은 엇갈린다.
    private void BuildLanes(Rect area, Vector3 player, List<Lane> lanes)
    {
        bool isHorizontal = 0 == UnityEngine.Random.Range(0, 2);
        float sign = 0 == UnityEngine.Random.Range(0, 2) ? 1f : -1f;
        float half = _laneSpacing * (_laneCount - 1) * 0.5f;
        float crossMin = isHorizontal ? area.yMin : area.xMin;
        float crossMax = isHorizontal ? area.yMax : area.xMax;
        float crossCenter = crossMax - crossMin > half * 2f
            ? Mathf.Clamp(isHorizontal ? player.y : player.x, crossMin + half, crossMax - half)
            : (crossMin + crossMax) * 0.5f;
        float length = isHorizontal ? area.width : area.height;

        for (int i = 0; i < _laneCount; i++)
        {
            float cross = crossCenter - half + _laneSpacing * i;
            float laneSign = 0 == i % 2 ? sign : -sign;
            Vector3 direction = isHorizontal ? new Vector3(laneSign, 0f, 0f) : new Vector3(0f, laneSign, 0f);
            float along = 0f < laneSign ? (isHorizontal ? area.xMin : area.yMin) : (isHorizontal ? area.xMax : area.yMax);
            Vector3 origin = isHorizontal ? new Vector3(along, cross, 0f) : new Vector3(cross, along, 0f);
            Vector2 guideSize = isHorizontal ? new Vector2(length, _guideWidth) : new Vector2(_guideWidth, length);
            Color hidden = new Color(_guideColor.r, _guideColor.g, _guideColor.b, 0f);

            lanes.Add(new Lane
            {
                Origin = origin,
                Direction = direction,
                Length = length,
                Guide = GameManager.Pool.SpawnSprite(_guidePrefab, origin + direction * (length * 0.5f), 0f, guideSize, hidden),
            });
        }
    }

    private void SpawnVisitors(List<Lane> lanes, List<Visitor> visitors)
    {
        Color hidden = new Color(_visitorColor.r, _visitorColor.g, _visitorColor.b, 0f);
        Vector2 headSize = new Vector2(_headSize, _headSize);

        for (int lane = 0; lane < lanes.Count; lane++)
        {
            float distance = UnityEngine.Random.Range(0f, _groupGap.Max);

            while (distance < lanes[lane].Length)
            {
                int groupSize = UnityEngine.Random.Range(_minGroupSize, Mathf.Max(_minGroupSize, _maxGroupSize) + 1);

                for (int i = 0; i < groupSize && distance < lanes[lane].Length; i++)
                {
                    visitors.Add(new Visitor
                    {
                        Body = GameManager.Pool.SpawnSprite(_bodyPrefab, lanes[lane].Origin, 0f, _bodySize, hidden),
                        Head = GameManager.Pool.SpawnSprite(_headPrefab, lanes[lane].Origin, 0f, headSize, hidden),
                        Lane = lane,
                        Start = distance,
                    });

                    distance += _visitorSpacing;
                }

                distance += _groupGap.PickRandom();
            }
        }
    }

    // 동선이 깔리는 동안 서서히 드러나고, 끝나면 흐려진다.
    private float GetPhaseAlpha(float elapsed, float streamEnd)
    {
        if (elapsed < _guideDuration)
        {
            return 0f < _guideDuration ? elapsed / _guideDuration : 1f;
        }

        return elapsed < streamEnd ? 1f : 1f - Mathf.Clamp01((elapsed - streamEnd) / _fadeDuration);
    }

    private void SetGuideAlpha(List<Lane> lanes, float alpha)
    {
        Color color = new Color(_guideColor.r, _guideColor.g, _guideColor.b, _guideColor.a * alpha);

        for (int i = 0; i < lanes.Count; i++)
        {
            lanes[i].Guide.color = color;
        }
    }

    private void MoveVisitors(List<Lane> lanes, List<Visitor> visitors, float elapsed, float alpha)
    {
        Vector3 headOffset = new Vector3(0f, (_bodySize.y + _headSize) * 0.5f, 0f);
        float walked = _walkSpeed * elapsed;

        for (int i = 0; i < visitors.Count; i++)
        {
            Visitor visitor = visitors[i];
            Lane lane = lanes[visitor.Lane];
            float distance = Mathf.Repeat(visitor.Start + walked, lane.Length);
            float edge = Mathf.Clamp01(Mathf.Min(distance, lane.Length - distance) / _edgeFade);
            float bob = Mathf.Abs(Mathf.Sin(elapsed * _bobSpeed + visitor.Start)) * _bobHeight;
            Vector3 point = lane.Origin + lane.Direction * distance + new Vector3(0f, bob, 0f);
            Color color = new Color(_visitorColor.r, _visitorColor.g, _visitorColor.b, _visitorColor.a * alpha * edge);

            visitor.Body.transform.position = point;
            visitor.Head.transform.position = point + headOffset;
            visitor.Body.color = color;
            visitor.Head.color = color;
        }
    }

    private bool IsTouchingAny(AttackTarget target, List<Visitor> visitors)
    {
        for (int i = 0; i < visitors.Count; i++)
        {
            if (target.IsTouching(visitors[i].Body.transform.position, _visitorRadius))
            {
                return true;
            }
        }

        return false;
    }
}
