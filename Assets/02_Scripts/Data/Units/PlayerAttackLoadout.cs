using UnityEngine;

// 플레이어가 키로 고르는 기본 공격들과, 게이지를 모두 써서 발동하는 특수 스킬. 게이지 최대치는 플레이어 스탯(데이터 표)에 있다.
[CreateAssetMenu(fileName = "PlayerAttackLoadout", menuName = "Scriptable Objects/PlayerAttackLoadout")]
public class PlayerAttackLoadout : ScriptableObject
{
    [SerializeField] private AttackStyle[] _basicStyles;
    [SerializeField] private AttackStyle _skillStyle;

    public AttackStyle[] BasicStyles { get { return _basicStyles; } }
    public AttackStyle SkillStyle { get { return _skillStyle; } }
}
