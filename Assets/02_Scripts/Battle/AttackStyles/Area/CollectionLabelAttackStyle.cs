using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

// 플레이어에게 소장품 라벨을 붙인다. 시간이 다 되면 그 자리에 전시대가 떨어지고, 한동안 닿으면 아픈 장애물로 남는다.
// 전시대가 남아 있는 동안 보스가 다른 패턴을 이어 가도록, 끝날 때까지 기다리지 않는 패턴으로 쓴다.
[CreateAssetMenu(fileName = "CollectionLabelAttackStyle", menuName = "Scriptable Objects/AttackStyle/CollectionLabel")]
public class CollectionLabelAttackStyle : AttackStyle
{
    [Header("라벨")]
    [SerializeField] private GameObject _labelPrefab;
    [SerializeField] private Vector2 _labelSize = new Vector2(1.1f, 1.1f);
    [SerializeField] private Vector3 _labelOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private Color _labelColor = new Color(0.95f, 0.92f, 0.82f, 1f);
    [SerializeField] private Color _urgentColor = new Color(0.85f, 0.1f, 0.1f, 1f);
    [SerializeField, Min(0.1f)] private float _countdown = 3f;
    // 발밑에 떨어질 자리를 따라 붙이고, 남은 시간만큼 안쪽을 채운다.
    [SerializeField] private GameObject _markerPrefab;
    [SerializeField] private Color _markerColor = new Color(0.62f, 0.48f, 0.2f, 0.35f);

    [Header("낙하")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.62f, 0.48f, 0.2f, 0.45f);
    [SerializeField, Min(0.1f)] private float _dropRadius = 1.8f;
    [SerializeField, Min(0f)] private float _warnDuration = 0.5f;
    [SerializeField] private HitSettings _dropHit = new();
    [SerializeField] private ImpactSettings _dropImpact = new();

    [Header("전시대")]
    [SerializeField] private GameObject _pedestalPrefab;
    [SerializeField, Min(0.1f)] private float _pedestalRadius = 1.2f;
    [SerializeField] private Color _pedestalColor = new Color(0.32f, 0.26f, 0.2f, 1f);
    [SerializeField, Min(0f)] private float _linger = 6f;
    [SerializeField, Min(0.05f)] private float _touchInterval = 0.5f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;
    [SerializeField] private HitSettings _touchHit = new();
    [SerializeField] private ImpactSettings _touchImpact = new();

    public override void Prewarm()
    {
        if (null != _labelPrefab)
        {
            GameManager.Pool.Prewarm(_labelPrefab, 1);
        }

        if (null != _markerPrefab)
        {
            GameManager.Pool.Prewarm(_markerPrefab, 2);
        }

        if (null != _pedestalPrefab)
        {
            GameManager.Pool.Prewarm(_pedestalPrefab, 2);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _labelPrefab || null == _markerPrefab || null == _pedestalPrefab)
        {
            return;
        }

        Vector3 point = await CountdownAsync(context, token);

        if (context.Target.IsDefeated)
        {
            return;
        }

        await Telegraph.PlayCircleAsync(GameManager.Pool, _telegraphPrefab, point, _dropRadius, _telegraphColor, _warnDuration, token);

        GameManager.Feedback.PlayImpact(_dropImpact, point);
        context.Damage.Blast(_dropHit, point, _dropRadius, gaugeGain);

        float diameter = _pedestalRadius * 2f;
        SpriteRenderer pedestal = GameManager.Pool.SpawnSprite(_pedestalPrefab, point, 0f, new Vector2(diameter, diameter), _pedestalColor);

        try
        {
            await StandAsync(context, point, gaugeGain, token);
            await pedestal.DOFade(0f, _fadeDuration).ToUniTaskKillOnCancel(token);
        }
        finally
        {
            GameManager.Pool.DespawnSprite(pedestal);
        }
    }

    // 라벨이 다 되는 순간 플레이어가 선 자리를 돌려준다.
    private async UniTask<Vector3> CountdownAsync(AttackContext context, CancellationToken token)
    {
        Transform player = context.Target.FollowTransform;
        float diameter = _dropRadius * 2f;
        Color areaColor = new Color(_markerColor.r, _markerColor.g, _markerColor.b, _markerColor.a * 0.5f);
        SpriteRenderer label = GameManager.Pool.SpawnSprite(_labelPrefab, player.position + _labelOffset, 0f, _labelSize, _labelColor);
        SpriteRenderer area = GameManager.Pool.SpawnSprite(_markerPrefab, player.position, 0f, new Vector2(diameter, diameter), areaColor);
        SpriteRenderer fill = GameManager.Pool.SpawnSprite(_markerPrefab, player.position, 0f, Vector2.zero, _markerColor);
        float elapsed = 0f;

        try
        {
            while (elapsed < _countdown && false == context.Target.IsDefeated)
            {
                float progress = elapsed / _countdown;
                float size = diameter * progress;

                label.transform.position = player.position + _labelOffset;
                label.color = Color.Lerp(_labelColor, _urgentColor, progress);
                area.transform.position = player.position;
                fill.transform.position = player.position;
                fill.transform.localScale = new Vector3(size, size, 1f);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }

            return player.position;
        }
        finally
        {
            GameManager.Pool.DespawnSprite(label);
            GameManager.Pool.DespawnSprite(area);
            GameManager.Pool.DespawnSprite(fill);
        }
    }

    private async UniTask StandAsync(AttackContext context, Vector3 point, float gaugeGain, CancellationToken token)
    {
        AttackTarget target = context.Target;
        float elapsed = 0f;
        float nextHitTime = 0f;

        while (elapsed < _linger && false == target.IsDefeated)
        {
            if (elapsed >= nextHitTime && target.IsTouching(point, _pedestalRadius))
            {
                nextHitTime = elapsed + _touchInterval;

                Vector3 hitPoint = target.GetAimPoint(Vector3.zero);
                GameManager.Feedback.PlayImpact(_touchImpact, hitPoint);
                context.Damage.Strike(_touchHit, hitPoint, gaugeGain);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }
}
