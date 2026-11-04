using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 잉크 팔에서 버티기. 종이 곳곳의 먹물 웅덩이에서 그림자 팔이 플레이어를 앞질러 노리며 뻗어 나온다. 끝까지 버티면 그로기로 이어진다.
[CreateAssetMenu(fileName = "InkArmsGimmick", menuName = "Scriptable Objects/Boss/Gimmick/InkArms")]
public class InkArmsGimmick : BossGimmick
{
    [SerializeField] private InkArmsSettings _inkArms = new();

    public override void Prewarm()
    {
        InkArmsRun.Prewarm(GameManager.Pool, _inkArms);
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        if (false == InkArmsRun.CanRun(context, _inkArms))
        {
            Debug.LogError($"잉크 팔 : {name}에 팔 프리팹이 없거나 물감 캔버스를 찾을 수 없습니다.");
            return true;
        }

        List<PaintMark> pools = ListPool<PaintMark>.Get();

        try
        {
            if (null != context.Hands)
            {
                context.Hands.PoseAsync(HandGesture.BothRaise, context.Target.GetAimPoint(Vector3.zero), _inkArms.PoolSettleDuration, token).Forget();
            }

            await InkArmsRun.ScatterPoolsAsync(context, _inkArms, pools, token);

            if (null != context.Hands)
            {
                context.Hands.PoseAsync(HandGesture.BothPress, context.Target.GetAimPoint(Vector3.zero), _inkArms.LungeInterval, token).Forget();
            }

            await InkArmsRun.LungeAsync(GameManager.Pool, context, _inkArms, pools, 0f, token);
        }
        finally
        {
            if (null != context.Hands)
            {
                context.Hands.ReturnToRest();
            }

            ListPool<PaintMark>.Release(pools);
        }

        return true;
    }
}
