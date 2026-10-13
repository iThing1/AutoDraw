using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private CanvasGroup _canvasGroup;

    private RectTransform _rect;
    private Sequence _sequence;
    private TweenCallback _despawnCallback;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        // 재생할 때마다 람다를 새로 만들지 않도록 반환 콜백을 한 번만 만들어 둔다.
        _despawnCallback = Despawn;
    }

    private void OnDisable()
    {
        _sequence?.Kill();
    }

    public void Play(int amount, bool isCritical, Vector2 screenPosition, DamageTextSettings settings)
    {
        // int를 문자열로 바꾸지 않고 TMP가 직접 숫자를 채워, 타격마다 문자열이 새로 생기지 않게 한다.
        _text.SetText("{0}", amount);
        _text.color = isCritical ? settings.CriticalColor : settings.NormalColor;
        _canvasGroup.alpha = 1f;

        _rect.position = screenPosition;

        float scale = isCritical ? settings.CriticalScale : settings.NormalScale;
        _rect.localScale = Vector3.one * scale * settings.PopScale;

        // 크게 튀어나왔다가 제 크기로 줄고(Append), 동시에 위로 떠오르며(Join), 끝나기 직전부터 흐려진다(Insert).
        _sequence = DOTween.Sequence()
            .Append(_rect.DOScale(scale, settings.PopDuration).SetEase(settings.PopEase))
            .Join(_rect.DOMoveY(screenPosition.y + settings.RiseDistance, settings.Duration).SetEase(settings.RiseEase))
            .Insert(settings.Duration - settings.FadeDuration, _canvasGroup.DOFade(0f, settings.FadeDuration))
            .SetUpdate(true)
            .OnComplete(_despawnCallback);
    }

    private void Despawn()
    {
        GameManager.Pool.Despawn(gameObject);
    }
}
