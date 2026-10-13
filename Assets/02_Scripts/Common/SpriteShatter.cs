using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class SpriteShatter
{
    // 조각은 원본 텍스처의 일부 영역만 쓰는 스프라이트라서 원래 그림이 그대로 쪼개진다.
    public static async UniTask PlayAsync(SpriteRenderer source, ShatterSettings settings, CancellationToken token)
    {
        Sprite sprite = source.sprite;

        if (null == sprite)
        {
            return;
        }

        // 넉백·플래시가 진행 중이면 위치와 색이 어긋난 채로 쪼개지므로 먼저 끝낸다.
        source.transform.DOComplete();
        source.DOComplete();

        Transform sourceTransform = source.transform;
        Vector3 center = sourceTransform.position;

        GameObject root = new GameObject(nameof(SpriteShatter));
        List<Sprite> pieceSprites = new List<Sprite>();
        List<UniTask> pieceTasks = new List<UniTask>();

        Rect spriteRect = sprite.rect;
        float cellWidth = spriteRect.width / settings.Columns;
        float cellHeight = spriteRect.height / settings.Rows;

        for (int row = 0; row < settings.Rows; row++)
        {
            for (int column = 0; column < settings.Columns; column++)
            {
                Rect cellRect = new Rect(spriteRect.x + column * cellWidth, spriteRect.y + row * cellHeight, cellWidth, cellHeight);
                Sprite pieceSprite = Sprite.Create(sprite.texture, cellRect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
                pieceSprites.Add(pieceSprite);

                // 칸 중심을 원본 피벗 기준 위치로 바꿔, 조각이 원본과 같은 자리에서 출발하게 한다.
                Vector2 cellCenter = new Vector2((column + 0.5f) * cellWidth, (row + 0.5f) * cellHeight) - sprite.pivot;
                Vector3 position = sourceTransform.TransformPoint(cellCenter / sprite.pixelsPerUnit);

                // 원본 렌더러를 복제해 재질·색·정렬 설정을 그대로 물려받고, 스프라이트만 조각으로 바꾼다.
                SpriteRenderer piece = Object.Instantiate(source, position, sourceTransform.rotation, root.transform);
                piece.transform.localScale = sourceTransform.lossyScale;
                piece.sprite = pieceSprite;

                pieceTasks.Add(FlyPieceAsync(piece, center, settings, token));
            }
        }

        source.enabled = false;

        ImpactPlayer.Play(settings.Impact, center, source.color);

        try
        {
            await UniTask.WhenAll(pieceTasks);
        }
        finally
        {
            Object.Destroy(root);

            // Sprite.Create로 만든 스프라이트는 오브젝트를 지워도 남으므로 따로 지운다.
            for (int i = 0; i < pieceSprites.Count; i++)
            {
                Object.Destroy(pieceSprites[i]);
            }
        }
    }

    // 조각마다 흐려지기 시작하는 때를 달리해 한꺼번에 꺼지지 않게 한다.
    private static UniTask FlyPieceAsync(SpriteRenderer piece, Vector3 center, ShatterSettings settings, CancellationToken token)
    {
        Transform pieceTransform = piece.transform;

        Vector3 direction = pieceTransform.position - center;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Random.insideUnitCircle;
        }

        Vector3 destination = pieceTransform.position + direction.normalized * settings.SpreadDistance.Pick();
        float rotation = Random.Range(-settings.MaxRotation, settings.MaxRotation);
        float fadeDelay = settings.FadeDelay.Pick();

        Sequence sequence = DOTween.Sequence()
            .Join(pieceTransform.DOMove(destination, settings.FlyDuration).SetEase(settings.FlyEase))
            .Join(pieceTransform.DORotate(new Vector3(0f, 0f, rotation), settings.FlyDuration, RotateMode.FastBeyond360).SetEase(settings.FlyEase))
            .Insert(fadeDelay, piece.DOFade(0f, settings.FadeDuration))
            .SetUpdate(true);

        return sequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }
}
