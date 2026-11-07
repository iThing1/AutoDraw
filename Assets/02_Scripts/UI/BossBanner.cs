using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using TMPro;
using UnityEngine;

public class BossBanner : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _content;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _subtitleText;
    // 배경이 어두운 곳에서 글자가 묻히지 않게 뒤에 까는 띠.
    [SerializeField] private GameObject _backdrop;

    private Vector2 _contentPosition;

    private void Awake()
    {
        _contentPosition = _content.anchoredPosition;
        _canvasGroup.alpha = 0f;
    }

    public async UniTask PlayAsync(string title, string subtitle, BannerSettings settings, CancellationToken token, bool showsBackdrop = false)
    {
        _titleText.text = title;
        _subtitleText.text = subtitle;
        _backdrop.SetActive(showsBackdrop);

        _content.anchoredPosition = _contentPosition;
        _content.localScale = Vector3.one * settings.StartScale;
        _canvasGroup.alpha = 0f;

        Sequence enter = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(1f, settings.InDuration))
            .Join(_content.DOScale(1f, settings.InDuration).SetEase(settings.InEase))
            .SetUpdate(true);

        await enter.ToUniTaskKillOnCancel(token);

        await GameManager.TimeScale.WaitRealtimeAsync(settings.HoldDuration, token);

        Sequence exit = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(0f, settings.OutDuration))
            .Join(_content.DOAnchorPosY(_contentPosition.y + settings.ExitMoveY, settings.OutDuration).SetEase(settings.OutEase))
            .SetUpdate(true);

        await exit.ToUniTaskKillOnCancel(token);
    }
}
