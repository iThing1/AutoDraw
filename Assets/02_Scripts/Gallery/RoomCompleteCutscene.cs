using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 전시실의 그림을 모두 채우면 한 번 튼다. 깬 순서대로 그림을 둘러보고 문의 잠금이 풀린 뒤 제자리로 돌아와 완성을 알린다.
public class RoomCompleteCutscene
{
    private readonly GalleryPlayer _player;
    private readonly RoomCompleteSettings _settings;

    public RoomCompleteCutscene(GalleryPlayer player, RoomCompleteSettings settings)
    {
        _player = player;
        _settings = settings;
    }

    public async UniTask PlayAsync(RoomData room, GalleryFrame[] frames, GalleryDoor door, CancellationToken token)
    {
        CinematicUI cinematicUI = GameManager.UI.Cinematic;
        CinematicSettings cinematic = _settings.Cinematic;

        GameManager.Sound.PlaySfx(cinematic.EnterSound);
        await cinematicUI.Letterbox.ShowAsync(cinematic.LetterboxHeightRatio, cinematic.LetterboxInDuration, cinematic.LetterboxEase, token);

        await TourPaintingsAsync(room.Stages, frames, token);

        await _player.MoveCameraToAsync(door.ViewPoint, _settings.MoveDuration, _settings.MoveEase, token);
        await door.PlayUnlockAsync(token);

        await _player.MoveCameraHomeAsync(_settings.HomeDuration, _settings.MoveEase, token);

        GameManager.Sound.PlaySfx(_settings.Banner.Sound);
        await cinematicUI.BossBanner.PlayAsync(room.CompleteTitle, room.CompleteSubtitle, _settings.Banner, token, true);

        GameManager.Sound.PlaySfx(cinematic.ExitSound);
        await cinematicUI.Letterbox.HideAsync(cinematic.LetterboxOutDuration, cinematic.LetterboxEase, token);
    }

    private async UniTask TourPaintingsAsync(IReadOnlyList<StageData> stages, GalleryFrame[] frames, CancellationToken token)
    {
        for (int i = 0; i < stages.Count; i++)
        {
            GalleryFrame frame = FindFrame(frames, stages[i]);

            if (null == frame)
            {
                Debug.LogError($"전시실 완성 연출 : {stages[i].name}이(가) 걸린 액자가 없어 건너뜁니다.");
                continue;
            }

            await _player.MoveCameraToAsync(frame.ShowcasePosition, frame.ViewPoint.rotation, _settings.MoveDuration, _settings.MoveEase, token);
            frame.PlayShine();
            await GameManager.TimeScale.WaitRealtimeAsync(_settings.HoldDuration, token);
        }
    }

    private static GalleryFrame FindFrame(GalleryFrame[] frames, StageData stage)
    {
        for (int i = 0; i < frames.Length; i++)
        {
            if (stage == frames[i].Stage)
            {
                return frames[i];
            }
        }

        return null;
    }
}
