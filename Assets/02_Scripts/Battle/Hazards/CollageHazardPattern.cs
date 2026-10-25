using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 오려 붙인 종이 조각 벽이 화면 한쪽에서 차례로 밀려온다. 벽마다 구멍이 나 있어, 구멍 자리에 서 있거나 대시로 벽을 뚫고 지나간다.
// 예고는 벽이 플레이어를 지나갈 줄에 미리 그어, 어디가 구멍인지 보고 자리를 잡게 한다.
[CreateAssetMenu(fileName = "CollageHazardPattern", menuName = "Scriptable Objects/Hazard/Collage")]
public class CollageHazardPattern : HazardPattern
{
    // 벽이 밀려가는 방향. 오른쪽에서 왼쪽, 위에서 아래, 왼쪽에서 오른쪽, 아래에서 위 순서다.
    private static readonly Vector3[] TRAVEL_DIRECTIONS = { Vector3.left, Vector3.down, Vector3.right, Vector3.up };

    [Header("예고")]
    [SerializeField] private AudioClip _warningSound;
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.35f, 0.35f, 0.38f, 0.4f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.7f;

    [Header("벽")]
    [SerializeField, Min(1)] private int _wallCount = 4;
    [SerializeField, Min(0f)] private float _wallInterval = 1f;
    [SerializeField, Min(1f)] private float _span = 40f;
    [SerializeField, Min(0.1f)] private float _thickness = 1.2f;
    [SerializeField, Min(0.5f)] private float _holeWidth = 3f;
    [SerializeField] private IntRange _holeCount = new IntRange(1, 2);
    // 첫 구멍은 플레이어 자리에서 옆으로 이 거리 안에 낸다. 너무 멀면 벽이 오기 전에 닿지 못한다.
    [SerializeField, Min(0f)] private float _firstHoleRange = 4f;
    // 플레이어 뒤쪽 이 거리에서 출발해 이만큼 밀려간다.
    [SerializeField, Min(0f)] private float _startDistance = 16f;
    [SerializeField, Min(0f)] private float _travelDistance = 32f;
    [SerializeField, Min(0.1f)] private float _speed = 12f;

    [Header("겉모습")]
    // 1x1 네모 그림을 길이·굵기에 맞춰 늘린다.
    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private Color _wallColor = new Color(0.22f, 0.22f, 0.24f, 1f);
    // 밀려가는 마지막 이 시간 동안 옅어져, 끝에 닿은 벽이 뚝 사라지지 않게 한다.
    [SerializeField, Min(0f)] private float _fadeDuration = 0.4f;

    [Header("피해")]
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    // 벽 사이 간격보다 한 벽이 지나가는 시간이 길어, 모든 벽이 한꺼번에 떠 있을 수 있다.
    public override void Prewarm(GameObject defaultProjectile)
    {
        int pieceCount = _wallCount * (_holeCount.Max + 1);

        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, pieceCount * 2);
        }

        if (null != _wallPrefab)
        {
            GameManager.Pool.Prewarm(_wallPrefab, pieceCount);
        }
    }

    public override async UniTask PlayAsync(AttackContext context, CancellationToken token)
    {
        if (null == _wallPrefab)
        {
            Debug.LogError($"콜라주 : {name}에 벽 프리팹이 지정되지 않았습니다.");
            return;
        }

        GameManager.Sound.PlaySfx(_warningSound);

        // 한 번 발동하는 동안은 같은 쪽에서만 밀려와, 방향이 엇갈린 벽이 겹쳐 피할 수 없게 되지 않는다.
        Vector3 travel = TRAVEL_DIRECTIONS[UnityEngine.Random.Range(0, TRAVEL_DIRECTIONS.Length)];
        List<UniTask> walls = new List<UniTask>(_wallCount);

        for (int i = 0; i < _wallCount; i++)
        {
            walls.Add(PlayWallAsync(context, travel, _wallInterval * i, token));
        }

        await UniTask.WhenAll(walls);
    }

    // 벽은 예고를 시작하는 순간의 플레이어 자리를 지나는 줄을 기준으로 놓는다.
    private async UniTask PlayWallAsync(AttackContext context, Vector3 travel, float delay, CancellationToken token)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);

        Vector3 crossing = context.Target.FollowTransform.position;
        Vector3 tangent = new Vector3(-travel.y, travel.x, 0f);
        float angle = Geometry2D.ToAngle(tangent);
        List<Vector2> pieces = BuildPieces();

        await PlayTelegraphAsync(crossing, tangent, angle, pieces, token);
        await MoveAsync(context, crossing, travel, tangent, angle, pieces, token);
    }

    private async UniTask PlayTelegraphAsync(Vector3 crossing, Vector3 tangent, float angle, List<Vector2> pieces, CancellationToken token)
    {
        List<UniTask> telegraphs = new List<UniTask>(pieces.Count);

        for (int i = 0; i < pieces.Count; i++)
        {
            Vector3 center = crossing + tangent * ((pieces[i].x + pieces[i].y) * 0.5f);

            telegraphs.Add(Telegraph.PlayLineAsync(_telegraphPrefab, center, angle, pieces[i].y - pieces[i].x, _thickness, _telegraphColor, _warnDuration,
                token));
        }

        await UniTask.WhenAll(telegraphs);
    }

    private async UniTask MoveAsync(AttackContext context, Vector3 crossing, Vector3 travel, Vector3 tangent, float angle, List<Vector2> pieces,
        CancellationToken token)
    {
        AttackTarget target = context.Target;
        List<SpriteRenderer> sprites = new List<SpriteRenderer>(pieces.Count);
        float travelled = 0f;
        bool hasStruck = false;

        try
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                sprites.Add(PooledSprite.Spawn(_wallPrefab, crossing, angle, new Vector2(pieces[i].y - pieces[i].x, _thickness), _wallColor));
            }

            float fadeStart = _travelDistance - _speed * _fadeDuration;

            while (travelled < _travelDistance)
            {
                Vector3 line = crossing + travel * (travelled - _startDistance);
                bool isFading = travelled > fadeStart;
                Color color = isFading ? GetFadedColor(travelled, fadeStart) : _wallColor;

                for (int i = 0; i < pieces.Count; i++)
                {
                    Vector3 from = line + tangent * pieces[i].x;
                    Vector3 to = line + tangent * pieces[i].y;
                    sprites[i].transform.position = (from + to) * 0.5f;

                    if (isFading)
                    {
                        sprites[i].color = color;
                    }

                    // 환경 탄막은 보스의 공격이 아니라서 게이지를 쌓지 않는다. 방패를 들고 있으면 막힌다.
                    if (false == hasStruck && target.IsTouchingSegment(from, to, _thickness * 0.5f))
                    {
                        hasStruck = true;
                        ImpactPlayer.Play(_impact, target.GetAimPoint(Vector3.zero));
                        context.Damage.Strike(_hit, target.GetAimPoint(Vector3.zero), 0f);
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                travelled += _speed * Time.deltaTime;
            }
        }
        finally
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                PooledSprite.Despawn(sprites[i]);
            }
        }
    }

    private Color GetFadedColor(float travelled, float fadeStart)
    {
        float remaining = Mathf.InverseLerp(_travelDistance, fadeStart, travelled);

        return new Color(_wallColor.r, _wallColor.g, _wallColor.b, _wallColor.a * remaining);
    }

    // 벽을 구멍으로 나눈 조각들. 조각은 벽 줄을 따라 잰 (시작, 끝) 거리이고, 0이 플레이어를 지나는 자리다.
    // 겹칠 자리에 뽑힌 구멍은 버려, 구멍 수가 정한 수보다 적을 수 있다.
    private List<Vector2> BuildPieces()
    {
        float half = _span * 0.5f;
        float holeHalf = _holeWidth * 0.5f;
        int holeCount = _holeCount.PickRandom();
        List<float> holes = new List<float>(holeCount) { UnityEngine.Random.Range(-_firstHoleRange, _firstHoleRange) };

        for (int i = 1; i < holeCount; i++)
        {
            float hole = UnityEngine.Random.Range(-half + holeHalf, half - holeHalf);

            if (false == IsOverlapping(holes, hole))
            {
                holes.Add(hole);
            }
        }

        holes.Sort();

        List<Vector2> pieces = new List<Vector2>(holes.Count + 1);
        float cursor = -half;

        for (int i = 0; i < holes.Count; i++)
        {
            float pieceEnd = holes[i] - holeHalf;

            if (pieceEnd > cursor)
            {
                pieces.Add(new Vector2(cursor, pieceEnd));
            }

            cursor = holes[i] + holeHalf;
        }

        if (half > cursor)
        {
            pieces.Add(new Vector2(cursor, half));
        }

        return pieces;
    }

    // 구멍 둘이 붙어 하나로 보이지 않도록 구멍 폭의 절반만큼 더 띄운다.
    private bool IsOverlapping(List<float> holes, float hole)
    {
        for (int i = 0; i < holes.Count; i++)
        {
            if (Mathf.Abs(holes[i] - hole) < _holeWidth * 1.5f)
            {
                return true;
            }
        }

        return false;
    }
}
