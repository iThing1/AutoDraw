using UnityEngine;

public static class GameObjectExtensions
{
    // 풀에서 처음 꺼낸 인스턴스에만 붙이고, 이후에는 이미 붙은 컴포넌트를 그대로 쓴다.
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        if (false == gameObject.TryGetComponent(out T component))
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
