using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

// 맞은 자리 주변에 물감 자국을 찍어 전장을 점점 칠한다. 자국은 풀에서 꺼내 쓰고, 상한을 넘으면 오래된 것부터 재사용한다.
public class PaintCanvas : MonoBehaviour
{
    [SerializeField] private GameObject _splatPrefab;
    [SerializeField] private Sprite[] _splatSprites;

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
        if (null == _splatPrefab || null == _splatSprites || 0 == _splatSprites.Length)
        {
            Debug.LogError("물감 캔버스 : 자국 프리팹 또는 자국 스프라이트가 지정되지 않았습니다.");
            return false;
        }

        return true;
    }

    // 맞은 자리 주변 반경 안에 크기와 회전이 제각각인 물감 자국을 여러 개 찍어 흔적을 남긴다.
    public void LeaveSplats(Vector3 center, Color color, bool isInk, HitFeedbackPreset preset)
    {
        int count = preset.SplatCount.Pick();

        for (int i = 0; i < count; i++)
        {
            // insideUnitCircle은 Vector2라서, 위치에 더하려고 Vector3로 바꾼다.
            Vector3 position = center + (Vector3)(Random.insideUnitCircle * preset.SplatSpread);
            float scale = preset.SplatScale.Pick();

            SpawnSplat(position, color, isInk, scale);
        }
    }

    private void SpawnSplat(Vector3 position, Color color, bool isInk, float scale)
    {
        if (_splats.Count >= _maxSplatCount)
        {
            RemoveSplat(_splats.Dequeue());
        }

        GameObject instance = GameManager.Pool.Spawn(_splatPrefab, transform);
        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

        PaintSplat splat = instance.GetOrAddComponent<PaintSplat>();

        splat.Show(_splatSprites[Random.Range(0, _splatSprites.Length)], color, isInk, NextSortingOrder());

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

        splat.transform.DOKill();
        GameManager.Pool.Despawn(splat.gameObject);
    }
}
