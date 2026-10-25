using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class DefeatCutscene : MonoBehaviour
{
    [SerializeField] private DefeatPreset _preset;

    public ResultTitleSettings ResultTitle { get { return _preset.ResultTitle; } }

    public bool HasRequiredReferences()
    {
        if (null == _preset)
        {
            Debug.LogError("패배 컷신 : DefeatPreset이 지정되지 않았습니다.");
            return false;
        }

        return true;
    }

    // 그림자 팔은 패배 때 한꺼번에 여러 개 꺼내므로, 첫 패배에서 끊기지 않게 미리 만들어 둔다.
    public void Prewarm()
    {
        InkArm armPrefab = _preset.Swallow.ArmPrefab;

        if (null != armPrefab)
        {
            GameManager.Pool.Prewarm(armPrefab.gameObject, _preset.Swallow.MaxArmCount);
        }
    }

    // 레터박스는 결과 화면 뒤에 그대로 둔다.
    public async UniTask PlayAsync(BattleUnit player, PaintCanvas paintCanvas, CancellationToken token)
    {
        using (GameManager.TimeScale.BeginSlowMotion(_preset.SlowTimeScale))
        {
            await UniTask.WhenAll(
                CinematicPlayer.EnterAsync(player.transform, _preset.Cinematic, token),
                InkSwallow.PlayAsync(player, _preset.Swallow, paintCanvas, token));
        }

        await RealtimeDelay.WaitAsync(_preset.HoldBeforeResult, token);
    }
}
