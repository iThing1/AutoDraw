using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 등장 컷신과 엔딩이 함께 쓰는 시네마틱 진입·퇴장. 레터박스와 카메라 블렌드가 모두 끝날 때까지 기다린다.
public static class CinematicPlayer
{
    public static UniTask EnterAsync(Transform focusTarget, CinematicSettings settings, CancellationToken token)
    {
        GameManager.Sound.PlaySfx(settings.EnterSound);
        GameManager.CameraRig.FocusTarget(focusTarget, settings.EnterBlendDuration);

        return UniTask.WhenAll(
            GameManager.UI.Cinematic.Letterbox.ShowAsync(settings.LetterboxHeightRatio, settings.LetterboxInDuration, settings.LetterboxEase, token),
            RealtimeDelay.WaitAsync(settings.EnterBlendDuration, token));
    }

    public static UniTask ExitAsync(CinematicSettings settings, CancellationToken token)
    {
        GameManager.Sound.PlaySfx(settings.ExitSound);
        GameManager.CameraRig.ReturnToGameplay(settings.ExitBlendDuration);

        return UniTask.WhenAll(
            GameManager.UI.Cinematic.Letterbox.HideAsync(settings.LetterboxOutDuration, settings.LetterboxEase, token),
            RealtimeDelay.WaitAsync(settings.ExitBlendDuration, token));
    }
}
