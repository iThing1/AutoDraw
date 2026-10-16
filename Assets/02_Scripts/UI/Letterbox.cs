using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

public class Letterbox : MonoBehaviour
{
    [SerializeField] private RectTransform _top;
    [SerializeField] private RectTransform _bottom;

    private void Awake()
    {
        HideImmediately();
    }

    // 패배 연출은 레터박스를 내리지 않고 결과 화면으로 넘어간다.
    public void HideImmediately()
    {
        _top.anchorMin = GetTopAnchorMin(0f);
        _bottom.anchorMax = GetBottomAnchorMax(0f);
    }

    public UniTask ShowAsync(float heightRatio, float duration, Ease ease, CancellationToken token)
    {
        return TweenHeightRatioAsync(heightRatio, duration, ease, token);
    }

    public UniTask HideAsync(float duration, Ease ease, CancellationToken token)
    {
        return TweenHeightRatioAsync(0f, duration, ease, token);
    }

    // 띠의 높이를 앵커 비율로 조절해 해상도와 관계없이 화면 대비 같은 두께가 되게 한다.
    // 히트 스톱이나 슬로 모션 중에도 UI 연출 속도는 그대로 유지한다.
    private UniTask TweenHeightRatioAsync(float ratio, float duration, Ease ease, CancellationToken token)
    {
        Sequence sequence = DOTween.Sequence()
            .Join(_top.DOAnchorMin(GetTopAnchorMin(ratio), duration))
            .Join(_bottom.DOAnchorMax(GetBottomAnchorMax(ratio), duration))
            .SetEase(ease)
            .SetUpdate(true);

        return sequence.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }

    private Vector2 GetTopAnchorMin(float ratio)
    {
        return new Vector2(0f, 1f - ratio);
    }

    private Vector2 GetBottomAnchorMax(float ratio)
    {
        return new Vector2(1f, ratio);
    }
}
