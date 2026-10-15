using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class DefeatCutscene : MonoBehaviour
{
    [SerializeField] private DefeatPreset _preset;

    public ResultTitleSettings ResultTitle { get { return _preset.Title; } }

    public bool HasRequiredReferences()
    {
        if (null == _preset)
        {
            Debug.LogError("패배 컷신 : DefeatPreset이 지정되지 않았습니다.");
            return false;
        }

        return true;
    }

    // 쓰러지는 순간이 보이도록 게임 시간을 늦춘 채 카메라를 플레이어에게 옮기고, 레터박스는 결과 화면 뒤에 그대로 둔다.
    public async UniTask PlayAsync(BattleUnit player, CancellationToken token)
    {
        using (GameManager.TimeScale.BeginSlowMotion(_preset.SlowTimeScale))
        {
            await UniTask.WhenAll(
                CinematicPlayer.EnterAsync(player.transform, _preset.Cinematic, token),
                RealtimeDelay.WaitAsync(_preset.SlowDuration, token));
        }
    }
}
