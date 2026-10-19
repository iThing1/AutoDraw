using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 시작할 때 미리 읽어 두고 그 뒤로는 바로 꺼낸다. 어드레서블로 바꿀 때는 이 클래스 안쪽만 바꾼다.
public class AssetManager
{
    private readonly Dictionary<string, Object> _assets = new();

    // 레지스트리가 에셋을 이미 들고 있어 읽을 것은 없고 찾기용 사전만 만든다.
    public UniTask PreloadAsync(AssetRegistry registry, CancellationToken token)
    {
        _assets.Clear();

        if (null == registry)
        {
            Debug.LogError("에셋 매니저 : AssetRegistry가 지정되지 않았습니다.");
            return UniTask.CompletedTask;
        }

        foreach (AssetRegistry.Entry entry in registry.Entries)
        {
            if (string.IsNullOrEmpty(entry.Key) || null == entry.Asset)
            {
                Debug.LogError("에셋 매니저 : AssetRegistry에 키나 에셋이 빈 줄이 있습니다.");
                continue;
            }

            if (false == _assets.TryAdd(entry.Key, entry.Asset))
            {
                Debug.LogError($"에셋 매니저 : AssetRegistry에 키 {entry.Key}이(가) 겹칩니다.");
            }
        }

        return UniTask.CompletedTask;
    }

    // 빈 키는 아직 만들지 않은 것이라 조용히 null이다. 없거나 형식이 다르면 오류를 남기고 null이다.
    public T Get<T>(string key) where T : Object
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (false == _assets.TryGetValue(key, out Object asset))
        {
            Debug.LogError($"에셋 매니저 : {key}이(가) AssetRegistry에 없습니다.");
            return null;
        }

        if (asset is T typed)
        {
            return typed;
        }

        Debug.LogError($"에셋 매니저 : {key}은(는) {typeof(T).Name}이(가) 아닙니다.");
        return null;
    }
}
