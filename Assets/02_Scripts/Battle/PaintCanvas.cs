using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 맞은 자리 주변에 물감 자국을 찍어 전장을 점점 칠한다. 자국은 풀에서 꺼내 쓰고, 상한을 넘으면 오래된 것부터 재사용한다.
public class PaintCanvas : MonoBehaviour
{
    [SerializeField] private GameObject _splatPrefab;

    [Header("자국")]
    [SerializeField, Min(1)] private int _maxSplatCount = 300;
    [SerializeField, Min(0f)] private float _appearDuration = 0.12f;
    [SerializeField] private Ease _appearEase = Ease.OutBack;

    private readonly Queue<PaintSplat> _splats = new();
    private readonly List<PaintSplat> _inkSplats = new();

    private int _sortingOrder;

    public IReadOnlyList<PaintSplat> InkSplats { get { return _inkSplats; } }

    public bool HasRequiredReferences()
    {
        if (null == _splatPrefab)
        {
            Debug.LogError("물감 캔버스 : 자국 프리팹이 지정되지 않았습니다.");
            return false;
        }

        return true;
    }

    public void LeaveSplats(Vector3 center, Color color, bool isInk, HitFeedbackPreset preset)
    {
        SplatStyle style = preset.SplatStyle;

        if (null == style || false == style.HasSprites)
        {
            return;
        }

        int count = preset.SplatCount.PickRandom();

        for (int i = 0; i < count; i++)
        {
            // insideUnitCircle은 Vector2라서, 위치에 더하려고 Vector3로 바꾼다.
            Vector3 position = center + (Vector3)(Random.insideUnitCircle * preset.SplatSpread);
            float scale = preset.SplatScale.PickRandom();

            SpawnSplat(position, style, color, isInk, scale);
        }
    }

    // 자국마다 시작을 조금씩 늦춰 한꺼번에 꺼지지 않게 한다.
    public async UniTask FadeOutInkAsync(float duration, FloatRange startDelay, Ease ease, CancellationToken token)
    {
        List<UniTask> fades = new List<UniTask>(_inkSplats.Count);

        for (int i = 0; i < _inkSplats.Count; i++)
        {
            Tween fade = _inkSplats[i].SpriteRenderer.DOFade(0f, duration)
                .SetDelay(startDelay.PickRandom())
                .SetEase(ease)
                .SetUpdate(true);

            fades.Add(fade.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token));
        }

        await UniTask.WhenAll(fades);

        RemoveAllInk();
    }

    // 큐 중간의 항목은 바로 뺄 수 없어서, 한 바퀴 돌며 잉크만 버리고 나머지는 순서대로 다시 넣는다.
    private void RemoveAllInk()
    {
        int count = _splats.Count;

        for (int i = 0; i < count; i++)
        {
            PaintSplat splat = _splats.Dequeue();

            if (splat.IsInk)
            {
                DespawnSplat(splat);
            }
            else
            {
                _splats.Enqueue(splat);
            }
        }

        _inkSplats.Clear();
    }

    private void SpawnSplat(Vector3 position, SplatStyle style, Color color, bool isInk, float scale)
    {
        if (_splats.Count >= _maxSplatCount)
        {
            RemoveSplat(_splats.Dequeue());
        }

        GameObject instance = GameManager.Pool.Spawn(_splatPrefab, transform);
        instance.transform.SetPositionAndRotation(position, style.PickRotation());

        PaintSplat splat = instance.GetOrAddComponent<PaintSplat>();

        splat.Show(style.PickSprite(), color, isInk, NextSortingOrder());

        Transform splatTransform = instance.transform;
        splatTransform.localScale = Vector3.zero;
        splatTransform.DOScale(scale, _appearDuration).SetEase(_appearEase).SetUpdate(true);

        _splats.Enqueue(splat);

        if (isInk)
        {
            _inkSplats.Add(splat);
        }
    }

    // 나중에 찍힌 자국이 위에 오도록 정렬 순서를 올린다. 정렬 순서는 short 범위라, 상한에 닿으면
    // 남아 있는 자국을 오래된 순서대로 다시 매겨 새 자국이 계속 맨 위에 오게 한다.
    private int NextSortingOrder()
    {
        if (short.MaxValue > _sortingOrder + 1)
        {
            return ++_sortingOrder;
        }

        _sortingOrder = 0;

        foreach (PaintSplat splat in _splats)
        {
            splat.SpriteRenderer.sortingOrder = ++_sortingOrder;
        }

        return ++_sortingOrder;
    }

    private void RemoveSplat(PaintSplat splat)
    {
        if (splat.IsInk)
        {
            _inkSplats.Remove(splat);
        }

        DespawnSplat(splat);
    }

    private void DespawnSplat(PaintSplat splat)
    {
        splat.transform.DOKill();
        splat.SpriteRenderer.DOKill();
        GameManager.Pool.Despawn(splat.gameObject);
    }
}
