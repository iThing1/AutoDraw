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
    [SerializeField, Min(1)] private int _maxSplatCount = 600;
    [SerializeField, Min(0f)] private float _appearDuration = 0.12f;
    [SerializeField] private Ease _appearEase = Ease.OutBack;
    // 상한을 넘어 지우는 자국이 갑자기 사라지지 않고 이 시간 동안 흐려지며 사라진다.
    [SerializeField, Min(0f)] private float _removeFadeDuration = 0.4f;

    // 오래된 순서대로 둔다. 상한을 넘을 때 가운데(가장 오래된 잉크)를 빼야 해서 큐 대신 리스트를 쓴다.
    private readonly List<PaintSplat> _splats = new();
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

            fades.Add(fade.ToUniTaskKillOnCancel(token));
        }

        await UniTask.WhenAll(fades);

        RemoveAllInk();
    }

    // 뒤에서부터 지워야 지운 자리 뒤의 순서가 밀려도 건너뛰는 자국이 없다.
    private void RemoveAllInk()
    {
        for (int i = _splats.Count - 1; i >= 0; i--)
        {
            if (_splats[i].IsInk)
            {
                _splats[i].Despawn();
                _splats.RemoveAt(i);
            }
        }

        _inkSplats.Clear();
    }

    // 연출이 새로 찍은 잉크 자국 자리를 써야 할 때(주변에 잉크가 없어 새로 찍는 경우 등) 찍은 자국을 돌려준다.
    public PaintSplat LeaveInkSplat(Vector3 position, SplatStyle style, Color color, float scale)
    {
        return LeaveSplat(position, style, color, true, scale);
    }

    // 맞은 자리가 아닌 곳에 자국을 하나 찍는다. 기믹이 플레이어 쪽 물감(안전한 자리)을 미리 깔아 둘 때도 쓴다.
    public PaintSplat LeaveSplat(Vector3 position, SplatStyle style, Color color, bool isInk, float scale)
    {
        if (null == style || false == style.HasSprites)
        {
            return null;
        }

        return SpawnSplat(position, style, color, isInk, scale);
    }

    // 플레이어 쪽 물감(잉크가 아닌 자국)이 이 자리에 칠해져 있는지. 자국 그림 크기의 원으로 넉넉히 본다.
    // 기믹 때 한 번씩만 불려, 자국 수(최대 수백 개)만큼 도는 비용은 문제가 되지 않는다.
    public bool IsPaintedAt(Vector3 position, float radius)
    {
        for (int i = 0; i < _splats.Count; i++)
        {
            PaintSplat splat = _splats[i];

            if (splat.IsInk)
            {
                continue;
            }

            float splatRadius = splat.SpriteRenderer.bounds.extents.x;

            if (Vector2.Distance(position, splat.transform.position) <= splatRadius + radius)
            {
                return true;
            }
        }

        return false;
    }

    // 아직 캔버스에 남은 잉크이고, 그 뒤에 찍힌 플레이어 물감에 가운데가 덮이지 않았는지.
    public bool IsInkExposed(PaintSplat ink)
    {
        int index = _splats.IndexOf(ink);

        if (0 > index || false == ink.IsInk)
        {
            return false;
        }

        Vector3 center = ink.transform.position;

        for (int i = index + 1; i < _splats.Count; i++)
        {
            PaintSplat splat = _splats[i];

            if (false == splat.IsInk && Vector2.Distance(center, splat.transform.position) <= splat.SpriteRenderer.bounds.extents.x)
            {
                return false;
            }
        }

        return true;
    }

    private PaintSplat SpawnSplat(Vector3 position, SplatStyle style, Color color, bool isInk, float scale)
    {
        if (_splats.Count >= _maxSplatCount)
        {
            RemoveOldest();
        }

        GameObject instance = GameManager.Pool.Spawn(_splatPrefab, transform);
        instance.transform.SetPositionAndRotation(position, style.PickRotation());

        PaintSplat splat = instance.GetOrAddComponent<PaintSplat>();

        splat.Show(style.PickSprite(), color, isInk, NextSortingOrder());

        Transform splatTransform = instance.transform;
        splatTransform.localScale = Vector3.zero;
        splatTransform.DOScale(scale, _appearDuration).SetEase(_appearEase).SetUpdate(true);

        _splats.Add(splat);

        if (isInk)
        {
            _inkSplats.Add(splat);
        }

        return splat;
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

        for (int i = 0; i < _splats.Count; i++)
        {
            _splats[i].SpriteRenderer.sortingOrder = ++_sortingOrder;
        }

        return ++_sortingOrder;
    }

    // 상한을 넘으면 보스 잉크 중 가장 오래된 것부터 지운다. 플레이어 물감은 클리어 그림에 남아야 해서 잉크가 하나도 없을 때만 지운다.
    // 목록에서는 바로 빼 상한을 비우고, 그림만 흐려지며 사라진다.
    private void RemoveOldest()
    {
        PaintSplat oldest = 0 < _inkSplats.Count ? _inkSplats[0] : _splats[0];

        _splats.Remove(oldest);

        if (oldest.IsInk)
        {
            _inkSplats.RemoveAt(0);
        }

        oldest.FadeOutAndDespawn(_removeFadeDuration);
    }
}
