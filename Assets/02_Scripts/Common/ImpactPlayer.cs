using UnityEngine;

public static class ImpactPlayer
{
    public static void Play(ImpactSettings settings, Vector3 position)
    {
        GameManager.CameraRig.Shake(settings.ShakeForce);
        GameManager.Sound.PlaySfx(settings.Sound);

        EffectSpawner.Spawn(settings.EffectPrefab, position, settings.EffectColor, settings.EffectScale, settings.EffectLifetime);
    }
}
