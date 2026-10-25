using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 보스 자신을 소실점으로 삼아 목표 쪽으로 좁은 쐐기 모양의 원근선 두 줄을 연필로 긋고, 소실점에서 먹 덩어리가 그 사이를 따라 튀어나온다.
// 먹 덩어리는 원근처럼 멀어질수록(목표에 다가올수록) 커지며 빨라져, 쐐기 밖으로 비켜서야 피한다. 여러 번 연달아 다시 겨눈다.
[CreateAssetMenu(fileName = "VanishingPointAttackStyle", menuName = "Scriptable Objects/AttackStyle/VanishingPoint")]
public class VanishingPointAttackStyle : AttackStyle
{
    [Header("쐐기")]
    [SerializeField, Min(1)] private int _rushCount = 3;
    // 쐐기의 반각(도). 먹 덩어리 크기도 이 각도로 정해져, 소실점에서 멀수록 커진다.
    [SerializeField, Range(1f, 45f)] private float _halfAngle = 9f;
    [SerializeField, Min(1f)] private float _length = 30f;
    [SerializeField, Min(0.01f)] private float _guideWidth = 0.15f;

    [Header("예고")]
    // 원근선과 먹 덩어리 그림. 1x1 네모 그림을 길이·굵기에 맞춰 늘린다.
    [SerializeField] private GameObject _guidePrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.35f, 0.35f, 0.38f, 0.4f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.7f;

    [Header("돌진")]
    [SerializeField] private GameObject _blockPrefab;
    [SerializeField] private Color _inkColor = new Color(0.02f, 0.02f, 0.03f, 1f);
    // 소실점에서 쐐기 끝까지 가는 시간. 처음엔 천천히, 다가올수록 빠르게 온다.
    [SerializeField, Min(0.01f)] private float _rushDuration = 0.55f;
    [SerializeField] private Ease _rushEase = Ease.InQuad;
    // 소실점 바로 앞에서도 보이도록 먹 덩어리의 가장 작은 크기.
    [SerializeField, Min(0f)] private float _minBlockSize = 0.3f;
    // 앞 돌진이 튀어나오고 다음 쐐기를 긋기 시작하기까지. 앞 덩어리가 날아가는 동안 다음 쐐기를 긋는다.
    [SerializeField, Min(0f)] private float _rushInterval = 0.15f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    public override void Prewarm()
    {
        if (null != _guidePrefab)
        {
            GameManager.Pool.Prewarm(_guidePrefab, 4);
        }

        if (null != _blockPrefab)
        {
            GameManager.Pool.Prewarm(_blockPrefab, 2);
        }
    }

    // 쐐기는 그을 때마다 목표의 지금 자리로 다시 겨눈다.
    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        List<UniTask> rushes = new List<UniTask>(_rushCount);

        for (int i = 0; i < _rushCount; i++)
        {
            Vector3 point = context.Owner.Body.position;
            Vector3 toTarget = context.Target.GetAimPoint(Vector3.zero) - point;
            float angle = Geometry2D.ToAngle(toTarget);

            await PlayGuidesAsync(point, angle, token);

            rushes.Add(RushAsync(context, point, angle, gaugeGain, token));

            if (i < _rushCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_rushInterval), cancellationToken: token);
            }
        }

        await UniTask.WhenAll(rushes);
    }

    // 소실점에서 쐐기 양쪽 가장자리를 따라 원근선 두 줄을 긋는다.
    private UniTask PlayGuidesAsync(Vector3 point, float angle, CancellationToken token)
    {
        UniTask left = PlayGuideAsync(point, angle + _halfAngle, token);
        UniTask right = PlayGuideAsync(point, angle - _halfAngle, token);

        return UniTask.WhenAll(left, right);
    }

    private UniTask PlayGuideAsync(Vector3 point, float angle, CancellationToken token)
    {
        Vector3 center = point + Geometry2D.ToDirection(angle) * (_length * 0.5f);

        return Telegraph.PlayLineAsync(_guidePrefab, center, angle, _length, _guideWidth, _telegraphColor, _warnDuration, token);
    }

    // 먹 덩어리가 쐐기를 따라 나아가며 소실점에서 떨어진 거리에 맞춰 커진다. 빠르게 지나쳐도 놓치지 않도록, 지난 자리부터 지금 자리까지 쓸고 지나간 길로 판정한다.
    private async UniTask RushAsync(AttackContext context, Vector3 point, float angle, float gaugeGain, CancellationToken token)
    {
        if (null == _blockPrefab)
        {
            return;
        }

        AttackTarget target = context.Target;
        Vector3 along = Geometry2D.ToDirection(angle);
        float spread = Mathf.Tan(_halfAngle * Mathf.Deg2Rad) * 2f;
        SpriteRenderer block = PooledSprite.Spawn(_blockPrefab, point, angle, new Vector2(_minBlockSize, _minBlockSize), _inkColor);
        Vector3 previous = point;
        float elapsed = 0f;
        bool hasStruck = false;

        ImpactPlayer.Play(_impact, point);

        try
        {
            while (elapsed < _rushDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed = Mathf.Min(elapsed + Time.deltaTime, _rushDuration);

                float distance = _length * DOVirtual.EasedValue(0f, 1f, elapsed / _rushDuration, _rushEase);
                float size = Mathf.Max(_minBlockSize, distance * spread);
                Vector3 center = point + along * distance;

                block.transform.position = center;
                block.transform.localScale = new Vector3(size, size, 1f);

                if (false == hasStruck && target.IsTouchingSegment(previous, center, size * 0.5f))
                {
                    hasStruck = true;
                    context.Damage.Strike(_hit, target.GetAimPoint(Vector3.zero), gaugeGain);
                }

                previous = center;
            }
        }
        finally
        {
            PooledSprite.Despawn(block);
        }
    }
}
