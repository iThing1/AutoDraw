using UnityEngine;

// 플레이어 유닛 데이터. 스탯은 데이터 표에서 오므로 여기에는 겉모습과 참조만 둔다.
[CreateAssetMenu(fileName = "PlayerUnitData", menuName = "Scriptable Objects/PlayerUnitData")]
public class PlayerUnitData : BattleUnitData
{
    // 지금 플레이어 모델의 스탯이라 특성으로 바뀐 값도 함께 읽힌다. 게임을 시작해 모델을 만든 뒤에만 읽는다.
    public override IUnitStats Stats { get { return GameManager.Player.Stats; } }
}
