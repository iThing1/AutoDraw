using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public static class RealtimeDelay
{
    // 컷신과 UI 연출은 히트 스톱이나 슬로 모션과 상관없이 정해진 시간만큼 보여야 하므로 실제 시간으로 기다린다.
    public static UniTask WaitAsync(float seconds, CancellationToken token)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: true, cancellationToken: token);
    }
}
