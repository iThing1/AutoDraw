using UnityEngine;

// 프리셋 하나로 히트 스톱, 플래시, 넉백, 임팩트, 물감 자국을 한 번에 재생한다.
public static class HitFeedbackPlayer
{
    public static void Play(HitFeedbackPreset preset, BattleUnit target, DamageInfo damage, PaintCanvas canvas)
    {
        if (null == preset)
        {
            return;
        }

        GameManager.TimeScale.PlayHitStop(preset.HitStopDuration, preset.HitStopTimeScale);

        UnitView view = target.View;

        if (null != view)
        {
            view.Flash(preset.FlashColor, preset.FlashDuration);

            // 공격자에게서 멀어지는 쪽으로 밀어낸다. 공격자를 모르면 오른쪽으로 민다.
            Vector3 direction = Vector3.right;

            if (null != damage.Attacker)
            {
                direction = target.Body.position - damage.Attacker.Body.position;
            }

            view.Knockback(target.Body, direction, preset.KnockbackDistance, preset.KnockbackDuration);
        }

        ImpactPlayer.Play(preset.Impact, damage.HitPoint, damage.PaintColor);

        if (null != canvas)
        {
            canvas.LeaveSplats(damage.HitPoint, damage.PaintColor, damage.IsInk, preset);
        }
    }
}
