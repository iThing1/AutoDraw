using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PaintSplat : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private bool _isInk;
    private TweenCallback _returnCallback;

    public bool IsInk { get { return _isInk; } }
    public SpriteRenderer SpriteRenderer { get { return _spriteRenderer; } }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 흐려질 때마다 람다를 새로 만들지 않도록 반환 콜백을 한 번만 만들어 둔다.
        _returnCallback = ReturnToPool;
    }

    public void Show(Sprite sprite, Color color, bool isInk, int sortingOrder)
    {
        _isInk = isInk;

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = color;
        _spriteRenderer.sortingOrder = sortingOrder;
    }

    public void Despawn()
    {
        transform.DOKill();
        _spriteRenderer.DOKill();
        ReturnToPool();
    }

    // 그림만 흐려지며 사라진 뒤 풀로 돌아간다. 스테이지와 함께 파괴되면 트윈도 함께 정리된다.
    public void FadeOutAndDespawn(float duration)
    {
        if (0f >= duration)
        {
            Despawn();
            return;
        }

        transform.DOKill();
        _spriteRenderer.DOKill();
        _spriteRenderer.DOFade(0f, duration)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(_returnCallback);
    }

    private void ReturnToPool()
    {
        GameManager.Pool.Despawn(gameObject);
    }
}
