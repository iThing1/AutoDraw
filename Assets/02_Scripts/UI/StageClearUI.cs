using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageClearUI : MonoBehaviour
{
    [SerializeField] private RectTransform _letterRoot;
    [SerializeField] private TMP_Text _letterPrefab;

    private readonly List<TMP_Text> _letters = new();

    public async UniTask PlayAsync(StageClearSettings settings, CancellationToken token)
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

        await _letterRoot.DOPunchScale(Vector3.one * settings.FinishPunch, settings.LetterPopDuration, 1, 0f)
            .SetUpdate(true)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }

    // async 메서드 안에서 트윈을 기다리지 않고 버리면 CS4014 경고가 나므로, 기다리지 않는 트윈은 일반 메서드로 뺀다.
    private void PopLetter(TMP_Text letter, StageClearSettings settings)
    {
        GameManager.Sound.PlaySfx(settings.LetterSound);

        letter.rectTransform.DOScale(1f, settings.LetterPopDuration)
            .SetEase(settings.LetterEase)
            .SetUpdate(true);
    }

    // 글자를 하나씩 추가하면 가운데 정렬이 매번 다시 계산돼 글자가 밀려나므로, 모두 크기 0으로 먼저 배치해 둔다.
    private void CreateLetters(StageClearSettings settings)
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
