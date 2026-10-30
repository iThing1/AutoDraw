using UnityEngine;

// 공격이 목표에 닿기 전, 쏘는 순간의 연출. 자동 공격도 "내가 쏜다"는 느낌이 나게 소리와 몸 반응을 함께 낸다.
public static class FireFeedbackPlayer
{
    public static void Play(FireFeedbackSettings settings, BattleUnit owner, Vector3 position)
    {
        Play(settings, owner, position, settings.Impact.EffectColor);
    }

    // 구슬처럼 쏘는 것마다 물감 색이 다르면, 설정의 기본 색 대신 그 색으로 파티클을 칠한다.
    public static void Play(FireFeedbackSettings settings, BattleUnit owner, Vector3 position, Color effectColor)
    {
        if (false == settings.Enabled)
        {
            return;
        }

        ImpactPlayer.Play(settings.Impact, position, effectColor);

        if (settings.Recoils && null != owner.View)
        {
            owner.View.PlayAttack();
        }
    }
}
