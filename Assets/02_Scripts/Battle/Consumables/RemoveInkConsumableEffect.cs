using UnityEngine;

// 둘레의 검은 잉크 자국과 먹물 웅덩이(SlowZone)를 지운다. 잉크 팔 기믹의 웅덩이도 잉크 자국이라 함께 지워진다.
[CreateAssetMenu(fileName = "RemoveInkConsumableEffect", menuName = "Scriptable Objects/Consumable/RemoveInk")]
public class RemoveInkConsumableEffect : ConsumableEffect
{
    [Header("연출")]
    [SerializeField] private ImpactSettings _impact = new();

    public override void Apply(AttackContext context, float value)
    {
        Vector3 center = context.Owner.Body.position;

        context.PaintCanvas.RemoveInkAround(center, value);
        SlowZone.ClearAround(center, value);
        ImpactPlayer.Play(_impact, center);
    }
}
