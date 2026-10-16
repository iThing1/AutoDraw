using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 공격이 떨어질 자리를 미리 보여 주는 표시. 옅은 테두리 영역 안을 진한 면이 채워 가고, 다 차는 순간 공격이 온다.
// 원은 가운데에서 바깥으로, 선은 가는 줄에서 굵기 방향으로 채워진다. 그림(원·네모)은 프리팹으로 정한다.
public static class Telegraph
{
    // 테두리 영역은 채움 색의 이 비율만큼만 진하게 깐다.
    private const float AREA_ALPHA_RATIO = 0.45f;
    private const float AREA_FADE_DURATION = 0.15f;

    // 게임 시간으로 채워 히트 스톱 동안에는 함께 멈춘다. 끝나면 표시를 바로 돌려준다.
    public static async UniTask PlayAsync(GameObject prefab, Vector3 position, float angle, Vector2 size, Vector2 startFillSize, Color color,
        float duration, CancellationToken token)
    {
        if (null == prefab)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
            return;
        }

        Color areaColor = new Color(color.r, color.g, color.b, color.a * AREA_ALPHA_RATIO);
        SpriteRenderer area = Spawn(prefab, position, angle, size, areaColor);
        SpriteRenderer fill = Spawn(prefab, position, angle, startFillSize, color);

        try
        {
            FadeInArea(area, areaColor.a, duration);

            await fill.transform.DOScale(new Vector3(size.x, size.y, 1f), duration)
                .SetEase(Ease.Linear)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
        }
        finally
        {
            Despawn(area);
            Despawn(fill);
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

    public static SpriteRenderer Spawn(GameObject prefab, Vector3 position, float angle, Vector2 size, Color color)
    {
        GameObject instance = GameManager.Pool.Spawn(prefab, position, Quaternion.Euler(0f, 0f, angle));
        instance.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer mark = instance.GetComponent<SpriteRenderer>();
        mark.color = color;

        return mark;
    }

    public static void Despawn(SpriteRenderer mark)
    {
        if (null == mark)
        {
            return;
        }

        mark.transform.DOKill();
        mark.DOKill();
        GameManager.Pool.Despawn(mark.gameObject);
    }
}
