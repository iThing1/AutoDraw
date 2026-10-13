using UnityEngine;

public static class EffectSpawner
{
    public static void Spawn(GameObject prefab, Vector3 position, Color color, float scale, float lifetime)
    {
        if (null == prefab)
        {
            return;
        }

        GameObject instance = GameManager.Pool.Spawn(prefab, position, Quaternion.identity);

        // 처음 꺼낸 인스턴스에만 붙고, 이후에는 캐싱된 파티클 목록을 재사용한다.
        if (false == instance.TryGetComponent(out PooledEffect effect))
        {
            effect = instance.AddComponent<PooledEffect>();
        }

        effect.Play(color, scale, lifetime);
    }
}
