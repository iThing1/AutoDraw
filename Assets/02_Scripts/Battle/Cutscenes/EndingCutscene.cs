using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class EndingCutscene : MonoBehaviour
{
    [SerializeField] private EndingPreset _preset;

    public ResultTitleSettings ResultTitle { get { return _preset.ResultTitle; } }

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
    // 끝나면 잉크가 다 걷힌 종이 전체를 찍어 돌려준다.
    public async UniTask<Texture2D> PlayAsync(BattleUnit boss, PaintCanvas paintCanvas, CancellationToken token)
    {
        await PlayFinishAsync(boss.transform, token);
        await PlayShatterAsync(boss, token);

        // 카메라가 뒤로 빠지는 동안 잉크가 걷히고, 둘 다 끝나야 물감만 남은 그림을 찍을 수 있다.
        await UniTask.WhenAll(
            PlayExitAsync(token),
            paintCanvas.FadeOutInkAsync(_preset.InkFadeDuration, _preset.InkFadeDelay, _preset.InkFadeEase, token));

        return CapturePainting();
    }

    // 경계가 없는 스테이지는 찍을 영역을 알 수 없어 그림을 남기지 않는다.
    private Texture2D CapturePainting()
    {
        Collider2D stageBounds = GameManager.CameraRig.CameraBounds;

        if (null == stageBounds)
        {
            return null;
        }

        Vector2Int size = _preset.PaintingSize;

        return StageCapture.Capture(GameManager.CameraRig.RenderCamera, stageBounds.bounds, size.x, size.y);
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

        // 손 달린 보스처럼 그림이 여러 장이면 함께 조각내, 남은 손이 완성된 그림에 찍히지 않게 한다.
        // 보스 오브젝트는 끄지 않고 그림만 숨긴다. 전투 카메라가 보스 자리를 기준으로 화면을 잡고 있어서,
        // 꺼 버리면 카메라가 돌아올 때 그림이 그려진 자리에서 벗어날 수 있다.
        IReadOnlyList<SpriteRenderer> renderers = boss.View.Renderers;
        List<UniTask> shatters = new List<UniTask>(renderers.Count);

        for (int i = 0; i < renderers.Count; i++)
        {
            shatters.Add(SpriteShatter.PlayAsync(renderers[i], _preset.Shatter, token));
        }

        await UniTask.WhenAll(shatters);
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
