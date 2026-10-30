using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 공격이 떨어질 자리를 미리 보여 주는 표시. 옅은 테두리 영역 안을 진한 면이 채워 가고, 다 차는 순간 공격이 온다.
public static class Telegraph
{
    // 테두리 영역은 채움 색의 이 비율만큼만 진하게 깐다.
    private const float AREA_ALPHA_RATIO = 0.45f;
    private const float AREA_FADE_DURATION = 0.15f;

    public static async UniTask PlayAsync(GameObject prefab, Vector3 position, float angle, Vector2 size, Vector2 startFillSize, Color color,
        float duration, CancellationToken token)
    {
        if (null == prefab)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
            return;
        }

        Color areaColor = new Color(color.r, color.g, color.b, color.a * AREA_ALPHA_RATIO);
        SpriteRenderer area = PooledSprite.Spawn(prefab, position, angle, size, areaColor);
        SpriteRenderer fill = PooledSprite.Spawn(prefab, position, angle, startFillSize, color);

        try
        {
            FadeInArea(area, areaColor.a, duration);

            await fill.transform.DOScale(new Vector3(size.x, size.y, 1f), duration)
                .SetEase(Ease.Linear)
                .ToUniTaskKillOnCancel(token);
        }
        finally
        {
            PooledSprite.Despawn(area);
            PooledSprite.Despawn(fill);
        }
    }

    private static void FadeInArea(SpriteRenderer area, float alpha, float duration)
    {
        Color color = area.color;
        area.color = new Color(color.r, color.g, color.b, 0f);
        area.DOFade(alpha, Mathf.Min(AREA_FADE_DURATION, duration));
    }

    public static UniTask PlayCircleAsync(GameObject prefab, Vector3 center, float radius, Color color, float duration, CancellationToken token)
    {
        float diameter = radius * 2f;

        return PlayAsync(prefab, center, 0f, new Vector2(diameter, diameter), Vector2.zero, color, duration, token);
    }

    // 선은 길이 방향(x)은 처음부터 다 보이고, 굵기 방향(y)만 차오른다.
    public static UniTask PlayLineAsync(GameObject prefab, Vector3 center, float angle, float length, float width, Color color, float duration,
        CancellationToken token)
    {
        return PlayAsync(prefab, center, angle, new Vector2(length, width), new Vector2(length, 0f), color, duration, token);
    }

    // 꺾인 길을 따라가는 선. 연필로 긋듯 sketchDuration 동안 처음부터 끝까지 그어 나가고, 굵기는 duration 동안 차오른다.
    // 프리팹은 월드 좌표를 쓰는 LineRenderer여야 한다.
    public static async UniTask PlayPathAsync(LineRenderer prefab, Polyline path, float width, Color color, float sketchDuration, float duration,
        CancellationToken token)
    {
        if (null == prefab)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
            return;
        }

        Color areaColor = new Color(color.r, color.g, color.b, color.a * AREA_ALPHA_RATIO);
        LineRenderer area = PathLine.Spawn(prefab, width, areaColor);
        LineRenderer fill = PathLine.Spawn(prefab, 0f, color);
        // 다 긋기 전에 공격이 오지 않도록, 긋는 시간은 예고 시간을 넘지 않게 한다.
        sketchDuration = Mathf.Min(sketchDuration, duration);
        float elapsed = 0f;
        int segment = 0;

        try
        {
            while (true)
            {
                float sketch = 0f < sketchDuration ? Mathf.Clamp01(elapsed / sketchDuration) : 1f;
                Vector3 tip = path.GetPointAt(path.Length * sketch, ref segment);

                PathLine.Reveal(area, path, segment, tip);
                PathLine.Reveal(fill, path, segment, tip);
                fill.widthMultiplier = 0f < duration ? width * Mathf.Clamp01(elapsed / duration) : width;

                if (elapsed >= duration)
                {
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }
        }
        finally
        {
            PathLine.Despawn(area);
            PathLine.Despawn(fill);
        }
    }
}
