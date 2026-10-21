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
        await CinematicPlayer.EnterAsync(landingPoint, _preset.Cinematic, token);
        await PlayDropAsync(boss, landingPoint.position, token);
        await PlayBannerAsync(boss.Data as BossData, token);
        await CinematicPlayer.ExitAsync(_preset.Cinematic, token);
    }

    private async UniTask PlayDropAsync(BattleUnit boss, Vector3 landingPosition, CancellationToken token)
    {
        boss.transform.position = landingPosition + Vector3.up * _preset.BossDropHeight;

        await RealtimeDelay.WaitAsync(_preset.DelayBeforeDrop, token);

        boss.gameObject.SetActive(true);

        Tween drop = boss.transform.DOMove(landingPosition, _preset.BossDropDuration)
            .SetEase(_preset.BossDropEase)
            .SetUpdate(true);

        await drop.ToUniTaskKillOnCancel(token);

        ImpactPlayer.Play(_preset.LandImpact, landingPosition);
    }

    // 보스 데이터가 아니면(배너 문구가 없는 유닛) 띄우지 않는다.
    private async UniTask PlayBannerAsync(BossData bossData, CancellationToken token)
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
}
