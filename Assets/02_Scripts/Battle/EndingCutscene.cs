using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EndingCutscene : MonoBehaviour
{
    [SerializeField] private EndingPreset _preset;

    public ResultTitleSettings ResultTitle { get { return _preset.StageClear; } }

    public bool HasRequiredReferences()
    {
        if (null == _preset)
        {
            Debug.LogError("엔딩 컷신 : EndingPreset이 지정되지 않았습니다.");
            return false;
        }

        return true;
    }

    // 프리셋 값은 캐싱하지 않고 매번 읽어, 플레이 중에 바꾼 값이 다음 재생에 바로 반영되게 한다.
    public async UniTask PlayAsync(BattleUnit boss, PaintCanvas paintCanvas, CancellationToken token)
    {
        await PlayFinishAsync(boss.transform, token);
        await PlayShatterAsync(boss, token);

        // 잉크는 카메라가 뒤로 빠지는 동안 걷히고, 결과 화면은 잉크를 기다리지 않고 카메라가 멈추면 나온다.
        paintCanvas.FadeOutInkAsync(_preset.InkFadeDuration, _preset.InkFadeDelay, _preset.InkFadeEase, token).Forget();

        await PlayExitAsync(token);
    }

    // 마지막 일격의 여운이 남도록 게임 시간을 늦춘 채로 카메라를 보스에게 옮긴다.
    private async UniTask PlayFinishAsync(Transform focusTarget, CancellationToken token)
    {
        using (GameManager.TimeScale.BeginSlowMotion(_preset.SlowTimeScale))
        {
            await UniTask.WhenAll(
                CinematicPlayer.EnterAsync(focusTarget, _preset.Cinematic, token),
                RealtimeDelay.WaitAsync(_preset.SlowDuration, token));
        }
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

    // 경계가 없는 스테이지는 전체를 담을 영역을 알 수 없어 전투 화면으로 돌아간다.
    private UniTask PlayExitAsync(CancellationToken token)
    {
        if (GameManager.CameraRig.TryShowWholeStage(_preset.OverviewPadding, _preset.OverviewBlendDuration))
        {
            return CinematicPlayer.HideAsync(_preset.Cinematic, _preset.OverviewBlendDuration, token);
        }

        return CinematicPlayer.ExitAsync(_preset.Cinematic, token);
    }
}
