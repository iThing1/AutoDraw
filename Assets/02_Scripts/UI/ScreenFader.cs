using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

// 결과 화면보다 위에 그려지도록 프리팹에서 자체 Canvas로 정렬 순서를 높여 둔다.
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.5f;

    public void ShowImmediately()
    {
        _canvasGroup.DOKill();
        _canvasGroup.alpha = 1f;
    }

    public UniTask FadeOutAsync(CancellationToken token)
    {
        return FadeAsync(1f, token);
    }

    public UniTask FadeInAsync(CancellationToken token)
    {
        return FadeAsync(0f, token);
    }

    // 슬로 모션 중일 수 있어 시간 배율을 무시한다.
    private UniTask FadeAsync(float alpha, CancellationToken token)
    {
        _canvasGroup.DOKill();

        return _canvasGroup.DOFade(alpha, _fadeDuration)
            .SetUpdate(true)
            .ToUniTaskKillOnCancel(token);
    }
}
