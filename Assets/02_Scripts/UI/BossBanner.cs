using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class BossBanner : MonoBehaviour
{
    private const float START_SCALE = 1.2f;
    private const float EXIT_MOVE_Y = 40f;

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

    public async UniTask PlayAsync(string title, string subtitle, float inDuration, float holdDuration, float outDuration, CancellationToken token)
    {
        _titleText.text = title;
        _subtitleText.text = subtitle;

        _content.anchoredPosition = _contentPosition;
        _content.localScale = Vector3.one * START_SCALE;
        _canvasGroup.alpha = 0f;

        Sequence enter = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(1f, inDuration))
            .Join(_content.DOScale(1f, inDuration).SetEase(Ease.OutBack))
            .SetUpdate(true);

        await enter.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

        await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), ignoreTimeScale: true, cancellationToken: token);

        Sequence exit = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(0f, outDuration))
            .Join(_content.DOAnchorPosY(_contentPosition.y + EXIT_MOVE_Y, outDuration).SetEase(Ease.InQuad))
            .SetUpdate(true);

        await exit.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }
}
