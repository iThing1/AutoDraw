using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PaintSplat : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private bool _isInk;

    public bool IsInk { get { return _isInk; } }
    public SpriteRenderer SpriteRenderer { get { return _spriteRenderer; } }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Show(Sprite sprite, Color color, bool isInk, int sortingOrder)
    {
        _isInk = isInk;

        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = color;
        _spriteRenderer.sortingOrder = sortingOrder;
    }
}
