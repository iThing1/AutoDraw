using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public static class RealtimeDelay
{
    // 컷신과 UI 연출은 히트 스톱이나 슬로 모션과 상관없이 정해진 시간만큼 보여야 하므로 실제 시간으로 기다린다.
    // 일시정지 동안 흐른 시간은 세지 않아, 실제 시간으로 재는 공격(돌진 베기)이 멈춘 사이에 맞지 않게 한다.
    public static async UniTask WaitAsync(float seconds, CancellationToken token)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);

            if (false == GameManager.TimeScale.IsPaused)
            {
                elapsed += Time.unscaledDeltaTime;
            }
        }
    }
}
