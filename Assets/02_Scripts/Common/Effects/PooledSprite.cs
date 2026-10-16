using DG.Tweening;
using UnityEngine;

// 풀에서 꺼내 쓰는 스프라이트 하나. 예고 표시, 먹물 범람, 빛기둥, 그림자처럼 자리·각도·크기·색만 정해 잠깐 보였다 돌려주는 그림에 쓴다.
// 돌려줄 때 걸려 있던 트윈을 함께 끊어, 다음에 꺼낸 그림이 이전 트윈에 움직이지 않게 한다.
public static class PooledSprite
{
    public static SpriteRenderer Spawn(GameObject prefab, Vector3 position, float angle, Vector2 size, Color color)
    {
        GameObject instance = GameManager.Pool.Spawn(prefab, position, Quaternion.Euler(0f, 0f, angle));
        instance.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sprite = instance.GetComponent<SpriteRenderer>();
        sprite.color = color;

        return sprite;
    }

    public static void Despawn(SpriteRenderer sprite)
    {
        if (null == sprite)
        {
            return;
        }

        sprite.transform.DOKill();
        sprite.DOKill();
        GameManager.Pool.Despawn(sprite.gameObject);
    }
}
