using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

public class BossIntroCutscene : MonoBehaviour
{
    [SerializeField] private CutscenePreset _preset;

    public bool HasRequiredReferences()
    {
        if (null == _preset)
        {
            Debug.LogError("보스 등장 컷신 : CutscenePreset이 지정되지 않았습니다.");
            return false;
        }

        return true;
    }

    // 프리셋 값은 캐싱하지 않고 매번 읽어, 플레이 중에 바꾼 값이 다음 재생에 바로 반영되게 한다.
    public async UniTask PlayAsync(BattleUnit boss, Transform landingPoint, CancellationToken token)
    {
        await PlayEnterAsync(landingPoint, token);
        await PlayDropAsync(boss, landingPoint.position, token);
        await PlayBannerAsync(boss.Data, token);
        await PlayExitAsync(token);
    }

    private UniTask PlayEnterAsync(Transform focusTarget, CancellationToken token)
    {
        CinematicSettings cinematic = _preset.Cinematic;

        GameManager.Sound.PlaySfx(cinematic.EnterSound);
        GameManager.CameraRig.FocusTarget(focusTarget, cinematic.EnterBlendDuration);

        return UniTask.WhenAll(
            GameManager.UI.Cinematic.Letterbox.ShowAsync(cinematic.LetterboxHeightRatio, cinematic.LetterboxInDuration, cinematic.LetterboxEase, token),
            RealtimeDelay.WaitAsync(cinematic.EnterBlendDuration, token));
    }

    private async UniTask PlayDropAsync(BattleUnit boss, Vector3 landingPosition, CancellationToken token)
    {
        boss.transform.position = landingPosition + Vector3.up * _preset.BossDropHeight;

        await RealtimeDelay.WaitAsync(_preset.DelayBeforeDrop, token);

        boss.gameObject.SetActive(true);

        Tween drop = boss.transform.DOMove(landingPosition, _preset.BossDropDuration)
            .SetEase(_preset.BossDropEase)
            .SetUpdate(true);

        await drop.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

        ImpactPlayer.Play(_preset.LandImpact, landingPosition);
    }

    private async UniTask PlayBannerAsync(BattleUnitData bossData, CancellationToken token)
    {
        if (null == bossData)
        {
            return;
        }

        BannerSettings banner = _preset.Banner;

        GameManager.Sound.PlaySfx(banner.Sound);

        await GameManager.UI.Cinematic.BossBanner.PlayAsync(
            bossData.BannerTitle,
            bossData.BannerSubtitle,
            banner,
            token);
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
}
