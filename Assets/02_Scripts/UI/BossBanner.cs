using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class BossBanner : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _content;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _subtitleText;

    private Vector2 _contentPosition;

    private void Awake()
    {
        _contentPosition = _content.anchoredPosition;
        _canvasGroup.alpha = 0f;
    }

    public async UniTask PlayAsync(string title, string subtitle, BannerSettings settings, CancellationToken token)
    {
        _titleText.text = title;
        _subtitleText.text = subtitle;

        _content.anchoredPosition = _contentPosition;
        _content.localScale = Vector3.one * settings.StartScale;
        _canvasGroup.alpha = 0f;

        Sequence enter = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(1f, settings.InDuration))
            .Join(_content.DOScale(1f, settings.InDuration).SetEase(settings.InEase))
            .SetUpdate(true);

        await enter.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

        await UniTask.Delay(TimeSpan.FromSeconds(settings.HoldDuration), ignoreTimeScale: true, cancellationToken: token);

        Sequence exit = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(0f, settings.OutDuration))
            .Join(_content.DOAnchorPosY(_contentPosition.y + settings.ExitMoveY, settings.OutDuration).SetEase(settings.OutEase))
            .SetUpdate(true);

        await exit.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }
}
