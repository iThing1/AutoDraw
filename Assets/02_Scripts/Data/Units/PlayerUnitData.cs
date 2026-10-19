using UnityEngine;

[CreateAssetMenu(fileName = "PlayerUnitData", menuName = "Scriptable Objects/PlayerUnitData")]
public class PlayerUnitData : BattleUnitData
{
    // 게임을 시작해 플레이어 모델을 만든 뒤에만 읽는다.
    public override IUnitStats Stats { get { return GameManager.Player.Stats; } }
    // 플레이어 공격은 장비 칸에서 고른다(PlayerAttackInput).
    public override AttackStyle AttackStyle { get { return null; } }
}
