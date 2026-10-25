using UnityEngine;

// 소모품을 썼을 때 일어나는 일과 그 겉모습. 세기·횟수·쿨타임은 표에서 받는다.
public abstract class ConsumableEffect : ScriptableObject
{
    [Header("표시")]
    // 아이콘 그림이 생기기 전까지 HUD·장비 화면의 칸을 칠할 색.
    [SerializeField] private Color _iconColor = Color.white;

    public Color IconColor { get { return _iconColor; } }

    public abstract void Apply(AttackContext context, float value);
}
