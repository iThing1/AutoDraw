using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 잉크 손. 보스 2의 잉크 팔을 뒤집어, 보스 둘레에 먹물 웅덩이를 흩뿌리고 그림자 팔이 보스를 붙잡아 때리게 한다.
// 쓰는 동안에도 움직이고 무기를 쏠 수 있도록 플레이어를 멈추지 않는다.
[CreateAssetMenu(fileName = "InkHandsAttackStyle", menuName = "Scriptable Objects/AttackStyle/InkHands")]
public class InkHandsAttackStyle : AttackStyle
{
    [SerializeField] private InkArmsSettings _inkArms = new();

    public override void Prewarm()
    {
        InkArmsRun.Prewarm(_inkArms);
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (false == InkArmsRun.CanRun(context, _inkArms))
        {
            Debug.LogError($"잉크 손 : {name}에 팔 프리팹이 없거나 물감 캔버스를 찾을 수 없습니다.");
            return;
        }

        List<PaintMark> pools = new List<PaintMark>(_inkArms.PoolCount);

        await InkArmsRun.ScatterPoolsAsync(context, _inkArms, pools, token);
        await InkArmsRun.LungeAsync(context, _inkArms, pools, gaugeGain, token);
    }
}
