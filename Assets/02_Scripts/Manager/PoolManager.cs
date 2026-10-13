using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
    private Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private Dictionary<GameObject, GameObject> _prefabByInstance = new();

    private Transform _root;

    public void Init()
    {
        _root = new GameObject("PoolRoot").transform;
    }

    public void Prewarm(GameObject prefab, int count)
    {
        Queue<GameObject> pool = GetPool(prefab);

        for (int i = pool.Count; i < count; i++)
        {
            pool.Enqueue(CreateInstance(prefab));
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Queue<GameObject> pool = GetPool(prefab);

        GameObject instance = 0 < pool.Count ? pool.Dequeue() : CreateInstance(prefab);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        return instance;
    }

    // UI처럼 특정 부모(Canvas) 아래에 있어야 하는 오브젝트용. 위치는 호출하는 쪽에서 맞춘다.
    public GameObject Spawn(GameObject prefab, Transform parent)
    {
        Queue<GameObject> pool = GetPool(prefab);

        GameObject instance = 0 < pool.Count ? pool.Dequeue() : CreateInstance(prefab);
        instance.transform.SetParent(parent, false);
        instance.SetActive(true);

        return instance;
    }

    public void Despawn(GameObject instance)
    {
        if (null == instance)
        {
            return;
        }

        if (false == _prefabByInstance.TryGetValue(instance, out GameObject prefab))
        {
            Debug.LogError($"풀 매니저 : {instance.name}은 풀에서 만든 오브젝트가 아닙니다.");
            return;
        }

        // 이미 반환된 오브젝트를 또 넣으면 같은 인스턴스가 두 번 꺼내지므로 막는다.
        if (false == instance.activeSelf)
        {
            return;
        }

        instance.SetActive(false);
        _pools[prefab].Enqueue(instance);
    }

    private Queue<GameObject> GetPool(GameObject prefab)
    {
        if (false == _pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _pools.Add(prefab, pool);
        }

        return pool;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        GameObject instance = Object.Instantiate(prefab, _root);
        instance.name = prefab.name;
        instance.SetActive(false);

        _prefabByInstance.Add(instance, prefab);

        return instance;
    }
}
