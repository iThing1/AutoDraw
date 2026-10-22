using UnityEngine;

[CreateAssetMenu(fileName = "PlayerUnitData", menuName = "Scriptable Objects/PlayerUnitData")]
public class PlayerUnitData : BattleUnitData
{
    // 특성 보너스를 더한 스탯은 플레이어 모델에 있어, 전투를 시작할 때 PlayerSetup이 넘긴다.
    public override IUnitStats Stats { get { return null; } }
    // 플레이어 공격은 장비 칸에서 고른다(PlayerAttack).
    public override AttackStyle AttackStyle { get { return null; } }
}
