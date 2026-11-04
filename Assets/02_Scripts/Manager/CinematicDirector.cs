using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 등장 컷신과 엔딩이 함께 쓰는 시네마틱 진입·퇴장. 레터박스와 카메라 블렌드가 모두 끝날 때까지 기다린다.
// 쓰는 매니저는 Init에서 넘겨받아, 어디에 기대는지 이 클래스만 봐도 알 수 있게 한다.
public class CinematicDirector
{
    private SoundManager _sound;
    private BattleCameraRig _cameraRig;
    private Letterbox _letterbox;
    private TimeScaleManager _timeScale;

    public void Init(SoundManager sound, BattleCameraRig cameraRig, Letterbox letterbox, TimeScaleManager timeScale)
    {
        _sound = sound;
        _cameraRig = cameraRig;
        _letterbox = letterbox;
        _timeScale = timeScale;
    }

    public UniTask EnterAsync(Transform focusTarget, CinematicSettings settings, CancellationToken token)
    {
        _sound.PlaySfx(settings.EnterSound);
        _cameraRig.FocusTarget(focusTarget, settings.EnterBlendDuration);

        return UniTask.WhenAll(
            _letterbox.ShowAsync(settings.LetterboxHeightRatio, settings.LetterboxInDuration, settings.LetterboxEase, token),
            _timeScale.WaitRealtimeAsync(settings.EnterBlendDuration, token));
    }

    public UniTask ExitAsync(CinematicSettings settings, CancellationToken token)
    {
        _cameraRig.ReturnToGameplay(settings.ExitBlendDuration);

        return HideAsync(settings, settings.ExitBlendDuration, token);
    }

    // 엔딩처럼 카메라를 전투 화면이 아닌 곳으로 이미 옮긴 경우에 쓴다.
    public UniTask HideAsync(CinematicSettings settings, float cameraBlendDuration, CancellationToken token)
    {
        _sound.PlaySfx(settings.ExitSound);

        return UniTask.WhenAll(
            _letterbox.HideAsync(settings.LetterboxOutDuration, settings.LetterboxEase, token),
            _timeScale.WaitRealtimeAsync(cameraBlendDuration, token));
    }
}
