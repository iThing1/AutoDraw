using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 쓰러진 자리 주변 잉크에서 그림자 팔이 기어 나와 대상을 움켜쥐고, 가장 많이 붙잡은 잉크로 끌고 가 가라앉힌다.
// 연출 한 번에 하나씩 만들어 상태를 필드로 들고, 팔의 움직임은 InkArm이 맡는다. 슬로 모션 중에도 정해진 시간에 끝나도록 실제 시간으로 돈다.
public class InkSwallow
{
    // 대상 바로 밑 잉크는 팔이 뻗을 거리가 없어 고르지 않고, 서로 너무 가까운 잉크는 같은 방향이라 하나만 고른다.
    private const float MIN_SOURCE_DISTANCE = 1.5f;
    private const float MIN_SOURCE_SEPARATION = 1.5f;
    // 끌려가는 시간의 이 비율부터 작아지기 시작해, 잉크에 닿을 즈음 사라진다.
    private const float SINK_SHRINK_START = 0.6f;

    private readonly BattleUnit _target;
    private readonly InkSwallowSettings _settings;
    private readonly PaintCanvas _paintCanvas;
    private readonly List<Transform> _sources = new();
    private readonly List<InkArm> _arms = new();

    private Vector3 _center;
    private Transform _sinkSource;

    private InkSwallow(BattleUnit target, InkSwallowSettings settings, PaintCanvas paintCanvas)
    {
        _target = target;
        _settings = settings;
        _paintCanvas = paintCanvas;
    }

    public static UniTask PlayAsync(BattleUnit target, InkSwallowSettings settings, PaintCanvas paintCanvas, CancellationToken token)
    {
        InkSwallow swallow = new InkSwallow(target, settings, paintCanvas);

        return swallow.RunAsync(token);
    }

    private async UniTask RunAsync(CancellationToken token)
    {
        SpriteRenderer body = _target.Body.GetComponent<SpriteRenderer>();

        if (null == body || null == _paintCanvas || null == _settings.ArmPrefab)
        {
            Debug.LogError("먹물 삼킴 : 대상의 Body 그림, 물감 캔버스, 팔 프리팹 중 비어 있는 것이 있습니다.");
            return;
        }

        // 넉백·번쩍임이 진행 중이면 자리와 색이 어긋난 채로 끌려가므로 먼저 끝낸다.
        _target.Body.DOComplete();
        body.DOComplete();

        // 끌려가는 동안 물리가 위치를 덮어쓰지 않게 끈다. 쓰러진 뒤라 다시 켤 일이 없다.
        if (_target.TryGetComponent(out Rigidbody2D physicsBody))
        {
            physicsBody.simulated = false;
        }

        _center = _target.transform.position;
        FindSources();

        if (0 == _sources.Count)
        {
            return;
        }

        try
        {
            SpawnArms();
            TwitchSources();

            await FollowTimelineAsync(body, token);

            if (null != _settings.ResidueSplat)
            {
                _paintCanvas.LeaveSplats(_sinkSource.position, _settings.InkColor, true, _settings.ResidueSplat);
            }

            _sinkSource.DOComplete();
            await _sinkSource.DOPunchScale(_sinkSource.localScale * _settings.SinkRippleStrength, _settings.SinkRippleDuration, PunchTween.VIBRATO, PunchTween.ELASTICITY)
                .SetUpdate(true)
                .ToUniTaskKillOnCancel(token);
        }
        finally
        {
            for (int i = 0; i < _arms.Count; i++)
            {
                GameManager.Pool.Despawn(_arms[i].gameObject);
            }
        }
    }

    // 가까운 잉크부터 고르되 서로 떨어진 것만 골라 팔이 여러 방향에서 오게 하고, 모자라면 대상 둘레에 새로 찍는다.
    private void FindSources()
    {
        int wantedCount = _settings.SourceCount.PickRandom();
        List<PaintSplat> candidates = new List<PaintSplat>();
        IReadOnlyList<PaintSplat> inkSplats = _paintCanvas.InkSplats;

        for (int i = 0; i < inkSplats.Count; i++)
        {
            float distance = Vector2.Distance(inkSplats[i].transform.position, _center);

            if (MIN_SOURCE_DISTANCE <= distance && _settings.SourceSearchRadius >= distance)
            {
                candidates.Add(inkSplats[i]);
            }
        }

        while (_sources.Count < wantedCount && 0 < candidates.Count)
        {
            int nearestIndex = FindNearestIndex(candidates);
            Transform nearest = candidates[nearestIndex].transform;
            candidates.RemoveAt(nearestIndex);

            if (IsFarFromSources(nearest.position))
            {
                _sources.Add(nearest);
            }
        }

        while (_sources.Count < wantedCount)
        {
            Vector3 direction = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)) * Vector3.right;
            Vector3 position = _center + direction * _settings.FallbackDistance.PickRandom();
            PaintSplat splat = _paintCanvas.LeaveInkSplat(position, _settings.FallbackSplatStyle, _settings.InkColor, _settings.FallbackSplatScale.PickRandom());

            if (null == splat)
            {
                Debug.LogError("먹물 삼킴 : 주변 잉크가 모자란데 새로 찍을 잉크 모양(Fallback Splat Style)이 없습니다.");
                return;
            }

            _sources.Add(splat.transform);
        }
    }

    // 잉크마다 팔 개수를 정하고, 가장 많은 팔이 나온 잉크를 대상이 끌려가 가라앉을 곳으로 정한다.
    // 잉크는 가까운 순서로 골라 두었으므로, 개수가 같으면 먼저 나온(가까운) 잉크가 남는다.
    private void SpawnArms()
    {
        List<int> armCounts = new List<int>(_sources.Count);
        int maxCount = 0;

        for (int i = 0; i < _sources.Count; i++)
        {
            int count = Mathf.Max(1, _settings.ArmsPerSource.PickRandom());
            armCounts.Add(count);

            if (count > maxCount)
            {
                maxCount = count;
                _sinkSource = _sources[i];
            }
        }

        // 팔이 나오기 직전 잉크가 꿈틀하는 동안 첫 팔이 나오기 시작한다.
        float firstStartTime = _settings.SourceTwitchDuration * 0.5f;

        for (int i = 0; i < _sources.Count; i++)
        {
            for (int arm = 0; arm < armCounts[i]; arm++)
            {
                Vector3 root = _sources[i].position + (Vector3)(Random.insideUnitCircle * _settings.RootSpread);
                Vector3 grabPoint = _center + (Vector3)(Random.insideUnitCircle * _settings.GrabSpread);
                float startTime = firstStartTime + _settings.ArmStartDelay.PickRandom();

                InkArm inkArm = GameManager.Pool.Spawn(_settings.ArmPrefab.gameObject, root, Quaternion.identity).GetComponent<InkArm>();
                inkArm.Setup(root, grabPoint, startTime, _sources[i] == _sinkSource, _settings);

                _arms.Add(inkArm);
            }
        }
    }

    // 팔이 나오기 직전 잉크 자국이 꿈틀해 어디서 나올지 미리 알린다.
    private void TwitchSources()
    {
        for (int i = 0; i < _sources.Count; i++)
        {
            Transform source = _sources[i];

            source.DOComplete();
            source.DOPunchScale(source.localScale * _settings.SourceTwitchStrength, _settings.SourceTwitchDuration, PunchTween.VIBRATO, PunchTween.ELASTICITY)
                .SetUpdate(true);
        }
    }

    // 팔들이 기어 옴 → 모두 닿으면 움켜쥠 → 잠시 뒤 끌고 감 순서로, 지난 시간만으로 대상과 모든 팔의 상태를 정한다.
    private async UniTask FollowTimelineAsync(SpriteRenderer body, CancellationToken token)
    {
        Transform targetTransform = _target.transform;
        Transform bodyTransform = _target.Body;
        Vector3 startScale = targetTransform.localScale;
        Vector3 bodyStartPosition = bodyTransform.localPosition;
        Color bodyStartColor = body.color;
        Vector3 sinkPoint = _sinkSource.position;

        float grabStart = GetLastArrivalTime();
        float pullStart = grabStart + _settings.GrabDuration + _settings.PullDelay;
        float pullEnd = pullStart + _settings.PullDuration;

        SwallowFrame frame = new SwallowFrame { Center = _center };

        while (frame.Elapsed < pullEnd)
        {
            frame.Elapsed += Time.unscaledDeltaTime;
            frame.Grab = DOVirtual.EasedValue(0f, 1f, SwallowFrame.GetProgress(frame.Elapsed, grabStart, _settings.GrabDuration), Ease.OutQuad);
            frame.PullProgress = SwallowFrame.GetProgress(frame.Elapsed, pullStart, _settings.PullDuration);
            frame.Remain = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SINK_SHRINK_START, 1f, frame.PullProgress));

            // InBack 같은 이징은 0보다 작은 값이 나와, 끌려가기 전에 잠깐 버티듯 뒤로 밀린다.
            float pull = DOVirtual.EasedValue(0f, 1f, frame.PullProgress, _settings.PullEase);
            frame.TargetPosition = Vector3.LerpUnclamped(_center, sinkPoint, pull);

            float shake = _settings.ShakeStrength * frame.Grab * (1f - frame.PullProgress);
            targetTransform.position = frame.TargetPosition;
            targetTransform.localScale = startScale * frame.Remain;
            bodyTransform.localPosition = bodyStartPosition + (Vector3)(Random.insideUnitCircle * shake);
            body.color = Color.Lerp(bodyStartColor, _settings.InkColor, frame.PullProgress);

            for (int i = 0; i < _arms.Count; i++)
            {
                _arms[i].Tick(frame);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        bodyTransform.localPosition = bodyStartPosition;
    }

    private int FindNearestIndex(List<PaintSplat> splats)
    {
        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < splats.Count; i++)
        {
            float distance = Vector2.Distance(splats[i].transform.position, _center);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private bool IsFarFromSources(Vector3 position)
    {
        for (int i = 0; i < _sources.Count; i++)
        {
            if (Vector2.Distance(position, _sources[i].position) < MIN_SOURCE_SEPARATION)
            {
                return false;
            }
        }

        return true;
    }

    private float GetLastArrivalTime()
    {
        float lastArrivalTime = 0f;

        for (int i = 0; i < _arms.Count; i++)
        {
            lastArrivalTime = Mathf.Max(lastArrivalTime, _arms[i].ArrivalTime);
        }

        return lastArrivalTime;
    }
}
