using DG.Tweening;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform _fill;
    [SerializeField] private RectTransform _trail;

    [Header("연출")]
    [SerializeField, Min(0f)] private float _fillDuration = 0.1f;
    [SerializeField, Min(0f)] private float _trailDelay = 0.25f;
    [SerializeField, Min(0f)] private float _trailDuration = 0.4f;
    [SerializeField] private Ease _trailEase = Ease.OutQuad;

    private Health _health;

    private void OnDestroy()
    {
        Unbind();
    }

    public void Bind(Health health)
    {
        Unbind();

        _health = health;
        _health.OnDamaged += HandleDamaged;
        _health.OnHealed += HandleHealed;

        SetRatioImmediately(_health.Ratio);
    }

    private void Unbind()
    {
        if (null == _health)
        {
            return;
        }

        _health.OnDamaged -= HandleDamaged;
        _health.OnHealed -= HandleHealed;
        _health = null;
    }

    // 깎인 만큼 흰 잔상이 잠깐 남았다가 따라 줄어들어, 얼마나 맞았는지 눈에 보이게 한다.
    private void HandleDamaged(DamageInfo info)
    {
        float ratio = _health.Ratio;

        TweenFill(ratio);

        _trail.DOKill();
        _trail.DOAnchorMax(new Vector2(ratio, 1f), _trailDuration)
            .SetDelay(_trailDelay)
            .SetEase(_trailEase)
            .SetUpdate(true);
    }

    private void HandleHealed(int amount)
    {
        float ratio = _health.Ratio;

        TweenFill(ratio);

        _trail.DOKill();
        _trail.anchorMax = new Vector2(ratio, 1f);
    }

    private void TweenFill(float ratio)
    {
        _fill.DOKill();
        _fill.DOAnchorMax(new Vector2(ratio, 1f), _fillDuration).SetUpdate(true);
    }

    // 막대는 왼쪽에 고정하고 오른쪽 앵커만 체력 비율로 움직여, 스프라이트 없이도 해상도에 맞게 줄어든다.
    private void SetRatioImmediately(float ratio)
    {
        _fill.DOKill();
        _trail.DOKill();

        _fill.anchorMax = new Vector2(ratio, 1f);
        _trail.anchorMax = new Vector2(ratio, 1f);
    }
}
