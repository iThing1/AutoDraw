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

        instance.GetOrAddComponent<PooledEffect>().Play(color, scale, lifetime);
    }
}
