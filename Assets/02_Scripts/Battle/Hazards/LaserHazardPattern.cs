using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 보안 레이저. 판 모서리 발사기에서 레이저가 판 전체를 부채꼴로 쓸고 지나간다.
// 레이저가 닿는 순간 방패로 막거나 대시로 뚫고 지나간다.
[CreateAssetMenu(fileName = "LaserHazardPattern", menuName = "Scriptable Objects/Hazard/Laser")]
public class LaserHazardPattern : HazardPattern
{
    // 판 모서리마다 판 안쪽을 덮는 90도의 시작 각도. 왼쪽 아래, 오른쪽 아래, 오른쪽 위, 왼쪽 위 순서다.
    private static readonly float[] CORNER_ANGLES = { 0f, 90f, 180f, 270f };
    private const float SWEEP_ANGLE = 90f;

    [Header("예고")]
    [SerializeField] private AudioClip _warningSound;
    [SerializeField] private GameObject _telegraphPrefab;
    // 빨간 예고는 막을 수 없다는 뜻이므로 쓰지 않는다.
    [SerializeField] private Color _telegraphColor = new Color(0.25f, 0.75f, 1f, 0.4f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.8f;

    [Header("레이저")]
    [SerializeField, Min(1)] private int _sweepCount = 2;
    [SerializeField, Min(0f)] private float _sweepGap = 0.6f;
    [SerializeField, Min(0.1f)] private float _sweepDuration = 2.4f;
    [SerializeField, Min(0.05f)] private float _thickness = 0.35f;
    [SerializeField] private GameObject _beamPrefab;
    [SerializeField] private Color _beamColor = new Color(0.35f, 0.85f, 1f, 0.9f);
    [SerializeField] private GameObject _emitterPrefab;
    [SerializeField, Min(0.1f)] private float _emitterSize = 0.8f;
    [SerializeField] private Color _emitterColor = new Color(0.2f, 0.22f, 0.26f, 1f);

    [Header("피해")]
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    public override void Prewarm(GameObject defaultProjectile)
    {
        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, 2);
        }

        if (null != _beamPrefab)
        {
            GameManager.Pool.Prewarm(_beamPrefab, 1);
        }

        if (null != _emitterPrefab)
        {
            GameManager.Pool.Prewarm(_emitterPrefab, 1);
        }
    }

    public override async UniTask PlayAsync(AttackContext context, CancellationToken token)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null == _beamPrefab || null == _emitterPrefab || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            Debug.LogError($"보안 레이저 : {name}에 레이저·발사기 프리팹이 없거나 플레이어 이동 범위를 알 수 없습니다.");
            return;
        }

        int corner = UnityEngine.Random.Range(0, CORNER_ANGLES.Length);

        for (int i = 0; i < _sweepCount; i++)
        {
            await SweepAsync(context, area, corner, token);

            // 다음 발사기는 바로 옆 모서리로 옮겨, 같은 자리에서 같은 쪽으로만 쓸지 않게 한다.
            corner = (corner + 1 + UnityEngine.Random.Range(0, 2) * 2) % CORNER_ANGLES.Length;

            if (i < _sweepCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_sweepGap), cancellationToken: token);
            }
        }
    }

    private async UniTask SweepAsync(AttackContext context, Rect area, int corner, CancellationToken token)
    {
        Vector3 origin = GetCorner(area, corner);
        float length = area.size.magnitude;
        bool isReversed = 0 == UnityEngine.Random.Range(0, 2);
        float from = CORNER_ANGLES[corner] + (isReversed ? SWEEP_ANGLE : 0f);
        float to = CORNER_ANGLES[corner] + (isReversed ? 0f : SWEEP_ANGLE);
        SpriteRenderer emitter = GameManager.Pool.SpawnSprite(_emitterPrefab, origin, 0f, new Vector2(_emitterSize, _emitterSize), _emitterColor);
        SpriteRenderer beam = null;

        try
        {
            GameManager.Sound.PlaySfx(_warningSound);

            await Telegraph.PlayLineAsync(GameManager.Pool, _telegraphPrefab, origin + Geometry2D.ToDirection(from) * (length * 0.5f), from, length, _thickness,
                _telegraphColor, _warnDuration, token);

            beam = GameManager.Pool.SpawnSprite(_beamPrefab, origin, from, new Vector2(length, _thickness), _beamColor);

            await MoveBeamAsync(context, origin, length, from, to, beam, token);
        }
        finally
        {
            GameManager.Pool.DespawnSprite(beam);
            GameManager.Pool.DespawnSprite(emitter);
        }
    }

    // 한 번 쓸고 지나가는 동안 한 번만 맞는다. 막으면 그 쓸기는 끝난 셈이고, 대시 중이면 피하되 다시 닿으면 그때 맞는다.
    private async UniTask MoveBeamAsync(AttackContext context, Vector3 origin, float length, float from, float to, SpriteRenderer beam,
        CancellationToken token)
    {
        AttackTarget target = context.Target;
        float elapsed = 0f;
        bool hasStruck = false;

        while (elapsed < _sweepDuration && false == target.IsDefeated)
        {
            float angle = Mathf.Lerp(from, to, elapsed / _sweepDuration);
            Vector3 end = origin + Geometry2D.ToDirection(angle) * length;

            beam.transform.SetPositionAndRotation((origin + end) * 0.5f, Quaternion.Euler(0f, 0f, angle));

            if (false == hasStruck && target.IsTouchingSegment(origin, end, _thickness * 0.5f) && false == target.TryDodge())
            {
                hasStruck = true;

                Vector3 hitPoint = target.GetAimPoint(Vector3.zero);
                GameManager.Feedback.PlayImpact(_impact, hitPoint);
                context.Damage.Strike(_hit, hitPoint, 0f);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    private static Vector3 GetCorner(Rect area, int corner)
    {
        switch (corner)
        {
            case 0:
                return new Vector3(area.xMin, area.yMin, 0f);
            case 1:
                return new Vector3(area.xMax, area.yMin, 0f);
            case 2:
                return new Vector3(area.xMax, area.yMax, 0f);
            default:
                return new Vector3(area.xMin, area.yMax, 0f);
        }
    }
}
