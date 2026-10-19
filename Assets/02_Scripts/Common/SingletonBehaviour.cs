using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    private static T _instance;

    // 하위 클래스에서 Awake가 필요하면 override하고 base.Awake()를 불러야 Init이 돈다.
    protected virtual void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {
        if (null == _instance)
        {
            _instance = this as T;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static T Instance { get { return _instance; } }
}
