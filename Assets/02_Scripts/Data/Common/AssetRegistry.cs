using System;
using System.Collections.Generic;
using UnityEngine;

// 키는 어드레서블 주소처럼 쓴다(AttackStyle/Light). 나중에 어드레서블로 옮겨도 키는 그대로 둔다.
[CreateAssetMenu(fileName = "AssetRegistry", menuName = "Scriptable Objects/AssetRegistry")]
public class AssetRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [SerializeField] private string _key;
        [SerializeField] private UnityEngine.Object _asset;

        public string Key { get { return _key; } }
        public UnityEngine.Object Asset { get { return _asset; } }
    }

    [SerializeField] private List<Entry> _entries = new();

    public IReadOnlyList<Entry> Entries { get { return _entries; } }
}
