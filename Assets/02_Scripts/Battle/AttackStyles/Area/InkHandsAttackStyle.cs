using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 잉크 손. 보스 2의 잉크 팔을 뒤집어, 보스 둘레에 먹물 웅덩이를 흩뿌리고 그림자 팔이 보스를 붙잡아 때리게 한다.
// 웅덩이는 플레이어 물감 자국으로 남아, 리무버에 지워지거나 보스 잉크 연출에 끌려가지 않는다.
// 쓰는 동안에도 움직이고 무기를 쏠 수 있도록 플레이어를 멈추지 않는다.
[CreateAssetMenu(fileName = "InkHandsAttackStyle", menuName = "Scriptable Objects/AttackStyle/InkHands")]
public class InkHandsAttackStyle : AttackStyle
{
    [SerializeField] private InkArmsSettings _inkArms = new();

    public override void Prewarm()
    {
        InkArmsRun.Prewarm(GameManager.Pool, _inkArms);
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (false == InkArmsRun.CanRun(context, _inkArms))
        {
            Debug.LogError($"잉크 손 : {name}에 팔 프리팹이 없거나 물감 캔버스를 찾을 수 없습니다.");
            return;
        }

        List<PaintMark> pools = ListPool<PaintMark>.Get();

        try
        {
            await InkArmsRun.ScatterPoolsAsync(context, _inkArms, pools, token);
            await InkArmsRun.LungeAsync(GameManager.Pool, context, _inkArms, pools, gaugeGain, token);
        }
        finally
        {
            ListPool<PaintMark>.Release(pools);
        }
    }
}
