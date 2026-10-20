using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;

public static class TweenAwaitExtensions
{
    // KillAndCancelAwait는 트윈을 지우는 도중에 기다리던 코드를 이어 가, 그 finally가 같은 대상을 DOKill하면 DOTween이 깨진다.
    // 기다림을 먼저 끊고 트윈은 그 뒤에 죽인다.
    public static async UniTask ToUniTaskKillOnCancel(this Tween tween, CancellationToken token)
    {
        try
        {
            await tween.ToUniTask(TweenCancelBehaviour.CancelAwait, token);
        }
        catch (OperationCanceledException)
        {
            if (tween.IsActive())
            {
                tween.Kill();
            }

            throw;
        }
    }
}
