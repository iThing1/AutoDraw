using DG.Tweening;
using TMPro;
using UnityEngine;

// HUD에 잠깐 떠올랐다 사라지는 글자 하나. 피해 숫자와 짧은 문구(GUARD·NO INK·BREAK)를 같은 연출로 띄우고 풀로 돌아간다.
public class FloatingText : MonoBehaviour
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

    public void Play(int amount, bool isCritical, Vector2 screenPosition, FloatingTextSettings settings)
    {
        // int를 문자열로 바꾸지 않고 TMP가 직접 숫자를 채워, 타격마다 문자열이 새로 생기지 않게 한다.
        _text.SetText("{0}", amount);

        Color color = isCritical ? settings.CriticalColor : settings.NormalColor;
        float scale = isCritical ? settings.CriticalScale : settings.NormalScale;

        Pop(color, scale, screenPosition, settings);
    }

    // "+{0}"처럼 숫자 자리가 있는 문구. 피해 숫자처럼 TMP가 숫자를 채워 문자열을 만들지 않는다.
    public void PlayNumber(string format, int amount, Color color, Vector2 screenPosition, FloatingTextSettings settings)
    {
        _text.SetText(format, amount);

        Pop(color, settings.NormalScale, screenPosition, settings);
    }

    public void PlayText(string text, Color color, Vector2 screenPosition, FloatingTextSettings settings)
    {
        _text.SetText(text);

        Pop(color, settings.NormalScale, screenPosition, settings);
    }

    private void Pop(Color color, float scale, Vector2 screenPosition, FloatingTextSettings settings)
    {
        _text.color = color;
        _canvasGroup.alpha = 1f;

        _rect.position = screenPosition;
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
