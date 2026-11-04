using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 목표 둘레에 보스 쪽만 열린 차단봉 울타리를 치고 조인다. 줄에 닿아 있는 동안 계속 맞는다.
[CreateAssetMenu(fileName = "VelvetRopeAttackStyle", menuName = "Scriptable Objects/AttackStyle/VelvetRope")]
public class VelvetRopeAttackStyle : AttackStyle
{
    private const int CORNER_COUNT = 4;

    private static readonly Vector3[] CornerDirections =
    {
        new Vector3(-1f, -1f, 0f),
        new Vector3(1f, -1f, 0f),
        new Vector3(1f, 1f, 0f),
        new Vector3(-1f, 1f, 0f),
    };

    [Header("울타리")]
    [SerializeField, Min(0.5f)] private float _startHalfSize = 4.5f;
    [SerializeField, Min(0f)] private float _endHalfSize = 0.8f;
    [SerializeField, Min(0.01f)] private float _tightenDuration = 1.6f;
    [SerializeField] private Ease _tightenEase = Ease.InQuad;
    [SerializeField, Min(0.05f)] private float _ropeWidth = 0.35f;

    [Header("기둥")]
    [SerializeField] private GameObject _postPrefab;
    [SerializeField, Min(0.1f)] private float _postSize = 0.6f;
    [SerializeField] private Color _postColor = new Color(0.62f, 0.48f, 0.2f, 1f);
    [SerializeField, Min(0f)] private float _throwDuration = 0.35f;
    [SerializeField, Min(0f)] private float _throwInterval = 0.08f;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.62f, 0.48f, 0.2f, 0.4f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.7f;

    [Header("줄")]
    [SerializeField] private GameObject _ropePrefab;
    [SerializeField] private Color _ropeColor = new Color(0.55f, 0.04f, 0.1f, 1f);
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;
    [SerializeField, Min(0.05f)] private float _hitInterval = 0.3f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    public override void Prewarm()
    {
        if (null != _postPrefab)
        {
            GameManager.Pool.Prewarm(_postPrefab, CORNER_COUNT);
        }

        if (null != _ropePrefab)
        {
            GameManager.Pool.Prewarm(_ropePrefab, CORNER_COUNT - 1);
        }

        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, (CORNER_COUNT - 1) * 2);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _postPrefab || null == _ropePrefab)
        {
            return;
        }

        Vector3 from = context.Owner.Body.position;
        Vector3 center = context.Target.GetAimPoint(Vector3.zero);
        int openSide = PickOpenSide(from - center);
        List<SpriteRenderer> posts = ListPool<SpriteRenderer>.Get();
        List<SpriteRenderer> ropes = ListPool<SpriteRenderer>.Get();

        try
        {
            await ThrowPostsAsync(from, center, posts, token);
            await PlayTelegraphsAsync(center, openSide, token);

            for (int side = 0; side < CORNER_COUNT; side++)
            {
                if (side != openSide)
                {
                    ropes.Add(GameManager.Pool.SpawnSprite(_ropePrefab, center, 0f, Vector2.zero, _ropeColor));
                }
            }

            await TightenAsync(context, center, openSide, posts, ropes, gaugeGain, token);
            await FadeOutAsync(posts, ropes, token);
        }
        finally
        {
            DespawnAll(posts);
            DespawnAll(ropes);
            ListPool<SpriteRenderer>.Release(posts);
            ListPool<SpriteRenderer>.Release(ropes);
        }
    }

    private async UniTask ThrowPostsAsync(Vector3 from, Vector3 center, List<SpriteRenderer> posts, CancellationToken token)
    {
        Vector2 size = new Vector2(_postSize, _postSize);
        Tween lastThrow = null;

        for (int i = 0; i < CORNER_COUNT; i++)
        {
            SpriteRenderer post = GameManager.Pool.SpawnSprite(_postPrefab, from, 0f, size, _postColor);
            posts.Add(post);

            lastThrow = post.transform.DOMove(GetCorner(center, _startHalfSize, i), _throwDuration).SetEase(Ease.OutQuad);

            if (i < CORNER_COUNT - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_throwInterval), cancellationToken: token);
            }
        }

        await lastThrow.ToUniTaskKillOnCancel(token);
    }

    private async UniTask PlayTelegraphsAsync(Vector3 center, int openSide, CancellationToken token)
    {
        List<UniTask> telegraphs = ListPool<UniTask>.Get();

        try
        {
            for (int side = 0; side < CORNER_COUNT; side++)
            {
                if (side == openSide)
                {
                    continue;
                }

                GetSide(center, _startHalfSize, side, out Vector3 from, out Vector3 to);
                Vector3 delta = to - from;

                telegraphs.Add(Telegraph.PlayLineAsync(GameManager.Pool, _telegraphPrefab, (from + to) * 0.5f, Geometry2D.ToAngle(delta), delta.magnitude,
                    _ropeWidth, _telegraphColor, _warnDuration, token));
            }

            await UniTask.WhenAll(telegraphs);
        }
        finally
        {
            ListPool<UniTask>.Release(telegraphs);
        }
    }

    private async UniTask TightenAsync(AttackContext context, Vector3 center, int openSide, List<SpriteRenderer> posts, List<SpriteRenderer> ropes,
        float gaugeGain, CancellationToken token)
    {
        AttackTarget target = context.Target;
        float radius = _ropeWidth * 0.5f;
        float elapsed = 0f;
        float nextHitTime = 0f;

        while (true)
        {
            float progress = DOVirtual.EasedValue(0f, 1f, Mathf.Clamp01(elapsed / _tightenDuration), _tightenEase);
            float halfSize = Mathf.Lerp(_startHalfSize, _endHalfSize, progress);
            int rope = 0;

            for (int i = 0; i < CORNER_COUNT; i++)
            {
                posts[i].transform.position = GetCorner(center, halfSize, i);
            }

            for (int side = 0; side < CORNER_COUNT; side++)
            {
                if (side == openSide)
                {
                    continue;
                }

                GetSide(center, halfSize, side, out Vector3 from, out Vector3 to);
                PlaceRope(ropes[rope++], from, to);

                if (elapsed >= nextHitTime && target.IsTouchingSegment(from, to, radius))
                {
                    nextHitTime = elapsed + _hitInterval;

                    Vector3 hitPoint = target.GetAimPoint(Vector3.zero);
                    GameManager.Feedback.PlayImpact(_impact, hitPoint);
                    context.Damage.Strike(_hit, hitPoint, gaugeGain);
                }
            }

            if (elapsed >= _tightenDuration)
            {
                break;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    private async UniTask FadeOutAsync(List<SpriteRenderer> posts, List<SpriteRenderer> ropes, CancellationToken token)
    {
        List<UniTask> fades = ListPool<UniTask>.Get();

        try
        {
            AddFades(posts, fades, token);
            AddFades(ropes, fades, token);

            await UniTask.WhenAll(fades);
        }
        finally
        {
            ListPool<UniTask>.Release(fades);
        }
    }

    private void AddFades(List<SpriteRenderer> sprites, List<UniTask> fades, CancellationToken token)
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            fades.Add(sprites[i].DOFade(0f, _fadeDuration).ToUniTaskKillOnCancel(token));
        }
    }

    private void PlaceRope(SpriteRenderer rope, Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;

        rope.transform.SetPositionAndRotation((from + to) * 0.5f, Quaternion.Euler(0f, 0f, Geometry2D.ToAngle(delta)));
        rope.transform.localScale = new Vector3(delta.magnitude, _ropeWidth, 1f);
    }

    // 보스는 판 안에 있으므로 보스 쪽 변을 열면 벽에 막히지 않는다.
    private static int PickOpenSide(Vector3 toOwner)
    {
        if (Mathf.Abs(toOwner.x) > Mathf.Abs(toOwner.y))
        {
            return 0f < toOwner.x ? 1 : 3;
        }

        return 0f < toOwner.y ? 2 : 0;
    }

    private static Vector3 GetCorner(Vector3 center, float halfSize, int index)
    {
        return center + CornerDirections[index] * halfSize;
    }

    private static void GetSide(Vector3 center, float halfSize, int side, out Vector3 from, out Vector3 to)
    {
        from = GetCorner(center, halfSize, side);
        to = GetCorner(center, halfSize, (side + 1) % CORNER_COUNT);
    }

    private static void DespawnAll(List<SpriteRenderer> sprites)
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            GameManager.Pool.DespawnSprite(sprites[i]);
        }
    }
}
