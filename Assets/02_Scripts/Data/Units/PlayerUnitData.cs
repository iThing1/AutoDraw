using UnityEngine;

[CreateAssetMenu(fileName = "PlayerUnitData", menuName = "Scriptable Objects/PlayerUnitData")]
public class PlayerUnitData : BattleUnitData
{
    // 게임을 시작해 플레이어 모델을 만든 뒤에만 읽는다.
    public override IUnitStats Stats { get { return GameManager.Player.Stats; } }
}
