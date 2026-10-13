using UnityEngine;

// 플레이어가 키로 고르는 기본 공격들과, 게이지를 모두 써서 발동하는 특수 스킬.
[CreateAssetMenu(fileName = "PlayerAttackLoadout", menuName = "Scriptable Objects/PlayerAttackLoadout")]
public class PlayerAttackLoadout : ScriptableObject
{
    [SerializeField] private AttackStyle[] _basicStyles;
    [SerializeField] private AttackStyle _skillStyle;
    [SerializeField, Min(1f)] private float _skillGaugeMax = 100f;

    public AttackStyle[] BasicStyles { get { return _basicStyles; } }
    public AttackStyle SkillStyle { get { return _skillStyle; } }
    public float SkillGaugeMax { get { return _skillGaugeMax; } }
}
