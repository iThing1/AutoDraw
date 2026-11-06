using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 관람 안내. 안내 조명이 판 위 동선을 따라 움직이고, 플레이어는 그 안에 머물러야 한다.
// 조명 밖에 오래 있으면 실패하고, 끝까지 따라가면 조명 밖이 모두 터진 뒤 그로기로 이어진다.
[CreateAssetMenu(fileName = "GuidedTourGimmick", menuName = "Scriptable Objects/Boss/Gimmick/GuidedTour")]
public class GuidedTourGimmick : PunishingBossGimmick
{
    private const int MAX_PICK_TRIES = 8;

    [Header("구역")]
    // 전투 카메라는 조명을 따라가지 않으므로, 플레이어 둘레 이 구역에 카메라와 이동 범위, 동선을 함께 묶어 조명이 화면을 벗어나지 않게 한다.
    [SerializeField] private Vector2 _fieldSize = new Vector2(28f, 16f);
    [SerializeField, Min(0f)] private float _cameraPadding = 1f;
    [SerializeField, Min(0f)] private float _cameraBlendDuration = 0.5f;

    [Header("안내 조명")]
    [SerializeField] private GameObject _lightPrefab;
    [SerializeField, Min(0.5f)] private float _lightRadius = 2.5f;
    [SerializeField] private Color _lightColor = new Color(1f, 0.82f, 0.3f, 0.4f);
    [SerializeField] private Color _lostColor = new Color(0.85f, 0.1f, 0.1f, 0.45f);
    // 몸 전체가 조명 안에 있어야 하므로 경계가 보여야 한다.
    [SerializeField] private LineRenderer _ringPrefab;
    [SerializeField, Min(0.01f)] private float _ringWidth = 0.18f;
    [SerializeField] private Color _ringColor = new Color(0.85f, 0.55f, 0.1f, 1f);
    [SerializeField] private Color _ringLostColor = new Color(0.85f, 0.1f, 0.1f, 1f);
    [SerializeField, Min(0f)] private float _startDelay = 1.2f;

    [Header("동선")]
    [SerializeField, Min(1)] private int _waypointCount = 6;
    [SerializeField] private FloatRange _segmentLength = new FloatRange(5f, 8f);
    [SerializeField, Min(0.1f)] private float _startSpeed = 3f;
    [SerializeField, Min(0.1f)] private float _endSpeed = 6f;
    [SerializeField] private GameObject _markerPrefab;
    [SerializeField, Min(0.1f)] private float _markerSize = 0.9f;
    [SerializeField] private Color _markerColor = new Color(0.95f, 0.5f, 0.1f, 1f);

    [Header("길 잃음")]
    // 조명 밖에 머문 시간을 쌓아 이만큼(초)이 되면 실패한다. 안에 있으면 recoverRate 배 빠르기로 줄어든다.
    [SerializeField, Min(0.1f)] private float _lostLimit = 1.5f;
    [SerializeField, Min(0f)] private float _recoverRate = 0.5f;

    [Header("방해")]
    [SerializeField, Min(0.1f)] private float _hazardInterval = 1.6f;
    // 원이 조명보다 작아야 조명 안에서 피할 수 있다.
    [SerializeField] private GroundDropSettings _hazard = new();

    [Header("마무리")]
    [SerializeField] private GameObject _overlayPrefab;
    [SerializeField] private Color _overlayColor = new Color(0.85f, 0.1f, 0.1f, 0.25f);
    [SerializeField, Min(0f)] private float _finalWarnDuration = 1f;
    [SerializeField] private ImpactSettings _finalImpact = new();
    [SerializeField] private ImpactSettings _successImpact = new();

    public override void Prewarm()
    {
        if (null != _lightPrefab)
        {
            GameManager.Pool.Prewarm(_lightPrefab, 1);
        }

        if (null != _markerPrefab)
        {
            GameManager.Pool.Prewarm(_markerPrefab, 1);
        }

        if (null != _ringPrefab)
        {
            GameManager.Pool.Prewarm(_ringPrefab.gameObject, 1);
        }

        GroundDrop.Prewarm(GameManager.Pool, _hazard, 2);
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null == _lightPrefab || null == _markerPrefab || null == _ringPrefab || null == _overlayPrefab || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            Debug.LogError($"관람 안내 : {name}에 그림 프리팹이 없거나 플레이어 이동 범위를 알 수 없습니다.");
            return true;
        }

        float diameter = _lightRadius * 2f;
        Vector3 start = context.Target.FollowTransform.position;
        Rect field = Geometry2D.FitRect(area, start, _fieldSize);
        // 조명 전체가 구역 안에 들도록 중심이 다닐 수 있는 범위를 반경만큼 줄인다.
        Rect route = new Rect(field.xMin + _lightRadius, field.yMin + _lightRadius, field.width - diameter, field.height - diameter);
        SpriteRenderer light = GameManager.Pool.SpawnSprite(_lightPrefab, start, 0f, new Vector2(diameter, diameter), _lightColor);
        SpriteRenderer marker = GameManager.Pool.SpawnSprite(_markerPrefab, start, 0f, new Vector2(_markerSize, _markerSize), _markerColor);
        LineRenderer ring = GameManager.Pool.Spawn(_ringPrefab, start, Quaternion.identity);
        List<Vector3> waypoints = ListPool<Vector3>.Get();

        ring.transform.localScale = new Vector3(_lightRadius, _lightRadius, 1f);
        ring.widthMultiplier = _ringWidth;
        SetRingColor(ring, _ringColor);

        using (CancellationTokenSource hazardSource = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            try
            {
                GameManager.CameraRig.ShowArea(new Bounds(field.center, field.size), _cameraPadding, _cameraBlendDuration);
                mover.SetMoveArea(field);

                BuildRoute(route, start, waypoints);

                await UniTask.Delay(TimeSpan.FromSeconds(_startDelay), cancellationToken: token);

                bool isFollowing = await TourAsync(context, light, ring, marker, waypoints, hazardSource.Token, token);

                hazardSource.Cancel();
                GameManager.Pool.DespawnSprite(marker);
                marker = null;

                if (isFollowing)
                {
                    isFollowing = await CloseAsync(context, area, light, token);
                }

                if (isFollowing)
                {
                    GameManager.Feedback.PlayImpact(_successImpact, light.transform.position);
                }
                else
                {
                    PunishTarget(context);
                }

                return isFollowing;
            }
            finally
            {
                GameManager.CameraRig.EndShowArea(_cameraBlendDuration);
                mover.SetMoveArea(area);
                hazardSource.Cancel();
                GameManager.Pool.DespawnSprite(light);
                GameManager.Pool.DespawnSprite(marker);
                GameManager.Pool.Despawn(ring.gameObject);
                ListPool<Vector3>.Release(waypoints);
            }
        }
    }

    private void BuildRoute(Rect area, Vector3 start, List<Vector3> waypoints)
    {
        Vector3 current = start;

        for (int i = 0; i < _waypointCount; i++)
        {
            current = PickNextPoint(area, current);
            waypoints.Add(current);
        }
    }

    // 판 밖으로 나가지 않는 방향을 몇 번 골라 보고, 끝내 못 찾으면 판 안으로 당긴다.
    private Vector3 PickNextPoint(Rect area, Vector3 from)
    {
        Vector3 next = from;

        for (int i = 0; i < MAX_PICK_TRIES; i++)
        {
            next = from + Geometry2D.ToDirection(UnityEngine.Random.Range(0f, 360f)) * _segmentLength.PickRandom();

            if (area.Contains(next))
            {
                return next;
            }
        }

        return new Vector3(Mathf.Clamp(next.x, area.xMin, area.xMax), Mathf.Clamp(next.y, area.yMin, area.yMax), 0f);
    }

    // 동선 끝까지 길을 잃지 않았으면 true다.
    private async UniTask<bool> TourAsync(AttackContext context, SpriteRenderer light, LineRenderer ring, SpriteRenderer marker, List<Vector3> waypoints,
        CancellationToken hazardToken, CancellationToken token)
    {
        AttackTarget target = context.Target;
        Transform lightTransform = light.transform;
        float totalLength = GetRouteLength(lightTransform.position, waypoints);
        float traveled = 0f;
        float lost = 0f;
        float nextHazard = _hazardInterval;
        float elapsed = 0f;
        int index = 0;

        while (index < waypoints.Count && false == target.IsDefeated)
        {
            float speed = Mathf.Lerp(_startSpeed, _endSpeed, 0f < totalLength ? traveled / totalLength : 1f);
            Vector3 before = lightTransform.position;

            lightTransform.position = Vector3.MoveTowards(before, waypoints[index], speed * Time.deltaTime);
            traveled += Vector3.Distance(before, lightTransform.position);

            if (lightTransform.position == waypoints[index])
            {
                index++;
            }

            if (index < waypoints.Count)
            {
                marker.transform.position = waypoints[index];
            }

            bool isInside = target.IsInsideCircle(lightTransform.position, _lightRadius);
            lost = isInside ? Mathf.Max(0f, lost - Time.deltaTime * _recoverRate) : lost + Time.deltaTime;
            light.color = Color.Lerp(_lightColor, _lostColor, lost / _lostLimit);
            ring.transform.position = lightTransform.position;
            SetRingColor(ring, Color.Lerp(_ringColor, _ringLostColor, lost / _lostLimit));

            if (lost >= _lostLimit)
            {
                return false;
            }

            if (elapsed >= nextHazard && index < waypoints.Count)
            {
                nextHazard += _hazardInterval;

                // 터질 때 조명이 와 있을 자리 둘레에 떨어뜨린다.
                Vector3 ahead = Vector3.MoveTowards(lightTransform.position, waypoints[index], speed * _hazard.WarnDuration);
                Vector3 point = ahead + (Vector3)(UnityEngine.Random.insideUnitCircle * (_lightRadius * 0.5f));

                GroundDrop.PlayAsync(GameManager.Pool, GameManager.Feedback, context, _hazard, point, hazardToken).Forget();
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }

        return true;
    }

    // 조명 밖 판 전체를 붉게 덮었다가 터뜨린다. 조명 안에 있었으면 true다.
    private async UniTask<bool> CloseAsync(AttackContext context, Rect area, SpriteRenderer light, CancellationToken token)
    {
        SpriteRenderer overlay = GameManager.Pool.SpawnSprite(_overlayPrefab, area.center, 0f, area.size + Vector2.one * (_lightRadius * 2f), _overlayColor);
        int lightOrder = light.sortingOrder;

        // 조명이 덮개 위에 보여야 안전한 곳이 드러난다. 풀에 돌려줄 때 원래대로 되돌린다.
        light.sortingOrder = overlay.sortingOrder + 1;
        light.color = _lightColor;

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_finalWarnDuration), cancellationToken: token);

            GameManager.Feedback.PlayImpact(_finalImpact, context.Target.GetAimPoint(Vector3.zero));

            return context.Target.IsInsideCircle(light.transform.position, _lightRadius);
        }
        finally
        {
            light.sortingOrder = lightOrder;
            GameManager.Pool.DespawnSprite(overlay);
        }
    }

    private static void SetRingColor(LineRenderer ring, Color color)
    {
        ring.startColor = color;
        ring.endColor = color;
    }

    private static float GetRouteLength(Vector3 start, List<Vector3> waypoints)
    {
        float length = 0f;
        Vector3 previous = start;

        for (int i = 0; i < waypoints.Count; i++)
        {
            length += Vector3.Distance(previous, waypoints[i]);
            previous = waypoints[i];
        }

        return length;
    }
}
