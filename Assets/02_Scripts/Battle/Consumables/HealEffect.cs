using UnityEngine;

// 체력을 채우고 발밑에 물감을 크게 찍는다. 물감 자리는 먹물 범람 기믹의 안전지대가 된다.
[CreateAssetMenu(fileName = "HealEffect", menuName = "Scriptable Objects/Consumable/Heal")]
public class HealEffect : ConsumableEffect
{
    [Header("물감")]
    [SerializeField] private SplatStyle _splat;
    [SerializeField, Min(0f)] private float _splatScale = 3f;

    public override void Apply(AttackContext context, float value)
    {
        BattleUnit owner = context.Owner;

        owner.Health.Heal(Mathf.RoundToInt(value));
        context.PaintCanvas.LeaveSplat(owner.Body.position, _splat, context.Damage.PickColor(), false, _splatScale);
    }
}
