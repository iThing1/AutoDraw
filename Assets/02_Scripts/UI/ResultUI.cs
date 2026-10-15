using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ResultChoice
{
    Retry,
    Gallery,
}

// 승리(STAGE CLEAR)와 패배(INK OVER)가 함께 쓰는 결과 화면. 문구가 다 튀어나온 뒤 버튼을 보여 주고 고를 때까지 기다린다.
public class ResultUI : MonoBehaviour
{
    [SerializeField] private RectTransform _letterRoot;
    [SerializeField] private TMP_Text _letterPrefab;

    [Header("버튼")]
    [SerializeField] private CanvasGroup _buttonGroup;
    [SerializeField] private Button _retryButton;
    // 갤러리가 생기기 전까지는 프리팹에서 꺼 둔다.
    [SerializeField] private Button _galleryButton;
    [SerializeField, Min(0f)] private float _buttonFadeDuration = 0.3f;

    private readonly List<TMP_Text> _letters = new();

    // 문구가 나오는 동안 버튼이 눌리지 않게 숨겨 둔다.
    private void Awake()
    {
        _buttonGroup.alpha = 0f;
        _buttonGroup.interactable = false;
        _buttonGroup.blocksRaycasts = false;
    }

    public async UniTask<ResultChoice> PlayAsync(ResultTitleSettings settings, CancellationToken token)
    {
        await PlayTitleAsync(settings, token);

        await _buttonGroup.DOFade(1f, _buttonFadeDuration)
            .SetUpdate(true)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

        _buttonGroup.interactable = true;
        _buttonGroup.blocksRaycasts = true;

        // WhenAny는 먼저 눌린 버튼의 순서 번호를 돌려준다. 0은 다시 하기, 1은 갤러리다.
        int choiceIndex = await UniTask.WhenAny(_retryButton.OnClickAsync(token), _galleryButton.OnClickAsync(token));

        _buttonGroup.interactable = false;

        return 0 == choiceIndex ? ResultChoice.Retry : ResultChoice.Gallery;
    }

    private async UniTask PlayTitleAsync(ResultTitleSettings settings, CancellationToken token)
    {
        CreateLetters(settings);

        await RealtimeDelay.WaitAsync(settings.DelayBefore, token);

        for (int i = 0; i < _letters.Count; i++)
        {
            TMP_Text letter = _letters[i];

            if (string.IsNullOrWhiteSpace(letter.text))
            {
                continue;
            }

            PopLetter(letter, settings);

            await RealtimeDelay.WaitAsync(settings.LetterInterval, token);
        }

        await RealtimeDelay.WaitAsync(settings.LetterPopDuration, token);

        GameManager.Sound.PlaySfx(settings.FinishSound);

        await _letterRoot.DOPunchScale(Vector3.one * settings.FinishPunch, settings.LetterPopDuration, PunchTween.VIBRATO, PunchTween.ELASTICITY)
            .SetUpdate(true)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }

    // async 메서드 안에서 트윈을 기다리지 않고 버리면 CS4014 경고가 나므로, 기다리지 않는 트윈은 일반 메서드로 뺀다.
    private void PopLetter(TMP_Text letter, ResultTitleSettings settings)
    {
        GameManager.Sound.PlaySfx(settings.LetterSound);

        letter.rectTransform.DOScale(1f, settings.LetterPopDuration)
            .SetEase(settings.LetterEase)
            .SetUpdate(true);
    }

    // 글자를 하나씩 추가하면 가운데 정렬이 매번 다시 계산돼 글자가 밀려나므로, 모두 크기 0으로 먼저 배치해 둔다.
    private void CreateLetters(ResultTitleSettings settings)
    {
        string text = settings.Text;
        PaintPalette palette = settings.LetterPalette;
        int colorIndex = 0;

        for (int i = 0; i < text.Length; i++)
        {
            TMP_Text letter = Instantiate(_letterPrefab, _letterRoot);
            letter.text = text[i].ToString();
            letter.rectTransform.localScale = Vector3.zero;

            if (char.IsWhiteSpace(text[i]))
            {
                letter.gameObject.AddComponent<LayoutElement>().preferredWidth = settings.SpaceWidth;
            }
            else if (null != palette)
            {
                letter.color = palette.Pick(colorIndex);
                colorIndex++;
            }

            _letters.Add(letter);
        }
    }
}
