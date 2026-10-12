using UnityEngine;

public static class EffectSpawner
{
    private const string EFFECT_SORTING_LAYER = "Effect";

    public static void Spawn(GameObject prefab, Vector3 position, Color color, float scale, float lifetime)
    {
        if (null == prefab)
        {
            return;
        }

        GameObject effect = Object.Instantiate(prefab, position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * scale;

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            main.startColor = color;

            // 기본값(Local)은 부모 크기를 무시하므로, 루트 크기가 하위 파티클까지 적용되게 바꾼다.
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystemRenderer particleRenderer = particles[i].GetComponent<ParticleSystemRenderer>();

            if (null != particleRenderer)
            {
                particleRenderer.sortingLayerName = EFFECT_SORTING_LAYER;
            }
        }

        Object.Destroy(effect, lifetime);
    }
}
