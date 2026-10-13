using DG.Tweening;
using UnityEngine;

// 유닛의 겉모습 연출(피격 플래시, 넉백)을 맡는다. M5에서 플래시는 셰이더로 바뀐다.
public class UnitView : MonoBehaviour
{
    // 피격 때 번쩍일 렌더러. 그림자·이펙트처럼 번쩍이면 안 되는 자식이 섞이지 않도록 프리팹에서 직접 지정한다.
    [SerializeField] private SpriteRenderer[] _renderers;

    private Color[] _originalColors;

    private void Awake()
    {
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = _renderers[i].color;
        }
    }

    // 히트 스톱으로 게임 시간이 멈춘 동안에도 보여야 하므로 실제 시간으로 재생한다.
    public void Flash(Color flashColor, float duration)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _renderers[i];

            spriteRenderer.DOKill();
            spriteRenderer.color = flashColor;
            spriteRenderer.DOColor(_originalColors[i], duration).SetUpdate(true);
        }
    }

    public void Knockback(Transform body, Vector3 direction, float distance, float duration)
    {
        if (0f >= distance || 0f >= duration)
        {
            return;
        }

        body.DOComplete();
        body.DOPunchPosition(direction.normalized * distance, duration, 1, 0f).SetUpdate(true);
    }
}
