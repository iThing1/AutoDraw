using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 플레이어 둘레를 어슬렁거리는 감시 카메라를 띄운다. 시야에 잠시 머물면 그 카메라가 멈춰 서서 플레이어 자리를 친다.
// 카메라를 띄워 둔 채 보스가 다른 패턴을 이어 가도록, 끝날 때까지 기다리지 않는 패턴으로 쓴다.
[CreateAssetMenu(fileName = "SecurityCameraAttackStyle", menuName = "Scriptable Objects/AttackStyle/SecurityCamera")]
public class SecurityCameraAttackStyle : AttackStyle
{
    private const int MAX_PICK_TRIES = 6;

    private sealed class Watcher
    {
        public SpriteRenderer Body;
        public SpriteRenderer Sight;
        public Vector3 Destination;
        public float Detect;
        public float Rest;
        public bool IsFiring;
    }

    [Header("카메라")]
    [SerializeField, Min(1)] private int _cameraCount = 3;
    [SerializeField, Min(0.1f)] private float _duration = 7f;
    [SerializeField] private GameObject _bodyPrefab;
    [SerializeField] private Vector2 _bodySize = new Vector2(0.7f, 0.5f);
    [SerializeField] private Color _bodyColor = new Color(0.2f, 0.22f, 0.26f, 1f);
    [SerializeField, Min(0f)] private float _throwDuration = 0.4f;
    [SerializeField, Min(0f)] private float _deployDistance = 5f;
    // 다음 목적지는 플레이어 둘레 이 거리 안에서 고른다. 플레이어보다 느려 떨쳐 낼 수 있다.
    [SerializeField, Min(0f)] private float _wanderSpread = 5f;
    [SerializeField, Min(0.1f)] private float _wanderSpeed = 2.5f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;

    [Header("시야")]
    [SerializeField] private GameObject _sightPrefab;
    [SerializeField, Min(0.1f)] private float _sightRadius = 2.4f;
    [SerializeField] private Color _sightColor = new Color(0.6f, 0.85f, 1f, 0.22f);
    [SerializeField] private Color _alertColor = new Color(0.85f, 0.1f, 0.1f, 0.35f);
    [SerializeField] private Color _restColor = new Color(0.6f, 0.85f, 1f, 0.08f);
    // 시야 안에 이만큼 머물면 들킨다. 밖에 나가면 같은 빠르기로 줄어든다.
    [SerializeField, Min(0f)] private float _detectTime = 0.4f;
    [SerializeField, Min(0f)] private float _rearmTime = 1.2f;

    [Header("사격")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.85f, 0.1f, 0.1f, 0.4f);
    [SerializeField, Min(0.1f)] private float _shotRadius = 1.6f;
    [SerializeField, Min(0f)] private float _warnDuration = 0.45f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    public override void Prewarm()
    {
        if (null != _bodyPrefab)
        {
            GameManager.Pool.Prewarm(_bodyPrefab, _cameraCount);
        }

        if (null != _sightPrefab)
        {
            GameManager.Pool.Prewarm(_sightPrefab, _cameraCount);
        }

        if (null != _telegraphPrefab)
        {
            GameManager.Pool.Prewarm(_telegraphPrefab, _cameraCount * 2);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null == _bodyPrefab || null == _sightPrefab || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            return;
        }

        List<Watcher> watchers = ListPool<Watcher>.Get();
        List<UniTask> shots = ListPool<UniTask>.Get();

        try
        {
            await DeployAsync(context, area, watchers, token);
            await WatchAsync(context, area, watchers, shots, gaugeGain, token);
            await UniTask.WhenAll(shots);
            await FadeOutAsync(watchers, token);
        }
        finally
        {
            for (int i = 0; i < watchers.Count; i++)
            {
                GameManager.Pool.DespawnSprite(watchers[i].Body);
                GameManager.Pool.DespawnSprite(watchers[i].Sight);
            }

            ListPool<Watcher>.Release(watchers);
            ListPool<UniTask>.Release(shots);
        }
    }

    // 보스에게서 플레이어 둘레로 고르게 흩어 던진다.
    private async UniTask DeployAsync(AttackContext context, Rect area, List<Watcher> watchers, CancellationToken token)
    {
        Vector3 from = context.Owner.Body.position;
        Vector3 player = context.Target.FollowTransform.position;
        float diameter = _sightRadius * 2f;
        float startAngle = Random.Range(0f, 360f);
        Sequence throws = DOTween.Sequence();

        for (int i = 0; i < _cameraCount; i++)
        {
            Vector3 point = Clamp(area, player + Geometry2D.ToDirection(startAngle + 360f * i / _cameraCount) * _deployDistance);
            Watcher watcher = new Watcher
            {
                Body = GameManager.Pool.SpawnSprite(_bodyPrefab, from, 0f, _bodySize, _bodyColor),
                Sight = GameManager.Pool.SpawnSprite(_sightPrefab, from, 0f, new Vector2(diameter, diameter), _sightColor),
            };

            watcher.Destination = PickDestination(area, player);
            watchers.Add(watcher);

            JoinThrow(throws, watcher, point);
        }

        await throws.ToUniTaskKillOnCancel(token);
    }

    private void JoinThrow(Sequence throws, Watcher watcher, Vector3 point)
    {
        throws.Join(watcher.Body.transform.DOMove(point, _throwDuration).SetEase(Ease.OutQuad));
        throws.Join(watcher.Sight.transform.DOMove(point, _throwDuration).SetEase(Ease.OutQuad));
    }

    private async UniTask WatchAsync(AttackContext context, Rect area, List<Watcher> watchers, List<UniTask> shots, float gaugeGain,
        CancellationToken token)
    {
        AttackTarget target = context.Target;
        float elapsed = 0f;

        while (elapsed < _duration && false == target.IsDefeated)
        {
            float deltaTime = Time.deltaTime;

            for (int i = 0; i < watchers.Count; i++)
            {
                Watcher watcher = watchers[i];

                if (watcher.IsFiring)
                {
                    continue;
                }

                Wander(watcher, area, target.FollowTransform.position, deltaTime);

                if (0f < watcher.Rest)
                {
                    watcher.Rest -= deltaTime;
                    watcher.Sight.color = _restColor;
                    continue;
                }

                bool isSeen = target.IsInBlast(watcher.Body.transform.position, _sightRadius);

                watcher.Detect = isSeen ? watcher.Detect + deltaTime : Mathf.Max(0f, watcher.Detect - deltaTime);
                watcher.Sight.color = Color.Lerp(_sightColor, _alertColor, 0f < _detectTime ? watcher.Detect / _detectTime : 1f);

                if (isSeen && watcher.Detect >= _detectTime)
                {
                    shots.Add(ShootAsync(context, watcher, gaugeGain, token));
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    private void Wander(Watcher watcher, Rect area, Vector3 player, float deltaTime)
    {
        Vector3 next = Vector3.MoveTowards(watcher.Body.transform.position, watcher.Destination, _wanderSpeed * deltaTime);

        watcher.Body.transform.position = next;
        watcher.Sight.transform.position = next;

        if (next == watcher.Destination)
        {
            watcher.Destination = PickDestination(area, player);
        }
    }

    // 쏘는 동안 카메라는 멈춰 서고, 다 쏘면 잠시 쉬었다가 다시 본다.
    private async UniTask ShootAsync(AttackContext context, Watcher watcher, float gaugeGain, CancellationToken token)
    {
        Vector3 point = context.Target.GetAimPoint(Vector3.zero);

        watcher.IsFiring = true;
        watcher.Detect = 0f;
        watcher.Sight.color = _alertColor;

        await Telegraph.PlayCircleAsync(GameManager.Pool, _telegraphPrefab, point, _shotRadius, _telegraphColor, _warnDuration, token);

        GameManager.Feedback.PlayImpact(_impact, point);
        context.Damage.Blast(_hit, point, _shotRadius, gaugeGain);

        watcher.IsFiring = false;
        watcher.Rest = _rearmTime;
    }

    private UniTask FadeOutAsync(List<Watcher> watchers, CancellationToken token)
    {
        Sequence fades = DOTween.Sequence();

        for (int i = 0; i < watchers.Count; i++)
        {
            fades.Join(watchers[i].Body.DOFade(0f, _fadeDuration));
            fades.Join(watchers[i].Sight.DOFade(0f, _fadeDuration));
        }

        return fades.ToUniTaskKillOnCancel(token);
    }

    // 지금 자리에서 너무 가깝지 않은 곳을 몇 번 골라 보고, 끝내 못 찾으면 마지막에 고른 자리를 쓴다.
    private Vector3 PickDestination(Rect area, Vector3 player)
    {
        Vector3 point = player;

        for (int i = 0; i < MAX_PICK_TRIES; i++)
        {
            point = Clamp(area, player + (Vector3)(Random.insideUnitCircle * _wanderSpread));

            if (Vector2.Distance(point, player) > _sightRadius)
            {
                break;
            }
        }

        return point;
    }

    private static Vector3 Clamp(Rect area, Vector3 point)
    {
        return new Vector3(Mathf.Clamp(point.x, area.xMin, area.xMax), Mathf.Clamp(point.y, area.yMin, area.yMax), 0f);
    }
}
