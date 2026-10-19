using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

// 유닛의 겉모습 연출(피격 플래시, 넉백, 맞을수록 물드는 바탕색)을 맡는다.
public class UnitView : MonoBehaviour
{
    // 피격 때 번쩍일 렌더러. 그림자·이펙트처럼 번쩍이면 안 되는 자식이 섞이지 않도록 프리팹에서 직접 지정한다.
    [SerializeField] private SpriteRenderer[] _renderers;

    private Color[] _originalColors;

    public IReadOnlyList<SpriteRenderer> Renderers { get { return _renderers; } }

    private void Awake()
    {
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = _renderers[i].color;
        }
    }

    // 맞을수록 물드는 보스처럼 바탕색이 바뀌는 유닛용. 피격 플래시는 맞은 뒤에 시작해 바뀐 색으로 돌아온다.
    // 플래시가 도는 중이면 그 플래시가 끝날 때 맞추도록 색은 바로 칠하지 않는다.
    public void SetBaseColor(Color color)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = color;

            if (false == DOTween.IsTweening(_renderers[i]))
            {
                _renderers[i].color = color;
            }
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
        body.DOPunchPosition(direction.normalized * distance, duration, PunchTween.VIBRATO, PunchTween.ELASTICITY).SetUpdate(true);
    }
}
