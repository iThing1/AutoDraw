using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EndingCutscene : MonoBehaviour
{
    [SerializeField] private EndingPreset _preset;
    [SerializeField] private StageClearUI _stageClearPrefab;

    public bool HasRequiredReferences()
    {
        if (null == _preset || null == _stageClearPrefab)
        {
            Debug.LogError("엔딩 컷신 : EndingPreset 또는 StageClearUI 프리팹이 지정되지 않았습니다.");
            return false;
        }

        return true;
    }

    // 프리셋 값은 캐싱하지 않고 매번 읽어, 플레이 중에 바꾼 값이 다음 재생에 바로 반영되게 한다.
    public async UniTask PlayAsync(BattleUnit boss, PaintCanvas paintCanvas, CancellationToken token)
    {
        try
        {
            await PlayFinishAsync(boss.transform, token);
            await PlayShatterAsync(boss, token);

            UniTask inkFade = paintCanvas.FadeOutInkAsync(_preset.InkFadeDuration, _preset.InkFadeDelay, _preset.InkFadeEase, token);

            await PlayExitAsync(token);
            await PlayStageClearAsync(token);
            await inkFade;
        }
        finally
        {
            GameManager.HitStop.BaseTimeScale = 1f;
        }
    }

    // 마지막 일격의 여운이 남도록 게임 시간을 늦춘 채로 카메라를 보스에게 옮긴다.
    private async UniTask PlayFinishAsync(Transform focusTarget, CancellationToken token)
    {
        CinematicSettings cinematic = _preset.Cinematic;

        GameManager.HitStop.BaseTimeScale = _preset.SlowTimeScale;

        GameManager.Sound.PlaySfx(cinematic.EnterSound);
        GameManager.CameraRig.FocusTarget(focusTarget, cinematic.EnterBlendDuration);

        await UniTask.WhenAll(
            GameManager.UI.Cinematic.Letterbox.ShowAsync(cinematic.LetterboxHeightRatio, cinematic.LetterboxInDuration, cinematic.LetterboxEase, token),
            RealtimeDelay.WaitAsync(cinematic.EnterBlendDuration, token),
            RealtimeDelay.WaitAsync(_preset.SlowDuration, token));

        GameManager.HitStop.BaseTimeScale = 1f;
    }

    private async UniTask PlayShatterAsync(BattleUnit boss, CancellationToken token)
    {
        await RealtimeDelay.WaitAsync(_preset.DelayBeforeShatter, token);

        SpriteRenderer bossRenderer = boss.Body.GetComponent<SpriteRenderer>();

        // 보스 오브젝트는 끄지 않고 그림만 숨긴다. 전투 카메라가 보스 자리를 기준으로 화면을 잡고 있어서,
        // 꺼 버리면 카메라가 돌아올 때 그림이 그려진 자리에서 벗어날 수 있다.
        if (null != bossRenderer)
        {
            await SpriteShatter.PlayAsync(bossRenderer, _preset.Shatter, token);
        }
    }

    private UniTask PlayExitAsync(CancellationToken token)
    {
        CinematicSettings cinematic = _preset.Cinematic;

        GameManager.Sound.PlaySfx(cinematic.ExitSound);
        GameManager.CameraRig.ReturnToGameplay(cinematic.ExitBlendDuration);

        return UniTask.WhenAll(
            GameManager.UI.Cinematic.Letterbox.HideAsync(cinematic.LetterboxOutDuration, cinematic.LetterboxEase, token),
            RealtimeDelay.WaitAsync(cinematic.ExitBlendDuration, token));
    }

    // 엔딩 문구는 엔딩에서만 쓰므로 엔딩 컷신이 직접 연다. 열어 둔 채로 끝나 그림 위에 남는다.
    private UniTask PlayStageClearAsync(CancellationToken token)
    {
        StageClearUI stageClear = GameManager.UI.Open(_stageClearPrefab, UILayer.Overlay);

        return stageClear.PlayAsync(_preset.StageClear, token);
    }
}
