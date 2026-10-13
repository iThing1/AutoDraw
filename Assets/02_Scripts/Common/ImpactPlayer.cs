using UnityEngine;

public static class ImpactPlayer
{
    public static void Play(ImpactSettings settings, Vector3 position)
    {
        Play(settings, position, settings.EffectColor);
    }

    // 타격처럼 공격마다 물감 색이 달라지는 경우, 설정의 기본 색 대신 넘겨받은 색으로 파티클을 칠한다.
    public static void Play(ImpactSettings settings, Vector3 position, Color effectColor)
    {
        GameManager.CameraRig.Shake(settings.ShakeForce);
        GameManager.Sound.PlaySfx(settings.PickSound(), settings.PickPitch());

        EffectSpawner.Spawn(settings.EffectPrefab, position, effectColor, settings.EffectScale, settings.EffectLifetime);
    }
}
