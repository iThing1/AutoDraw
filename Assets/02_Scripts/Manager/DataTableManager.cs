using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 표 이름 = 클래스 이름 = 시트 탭 이름 = JSON 파일 이름이다.
public class DataTableManager
{
    private DataTable<PlayerStatData> _playerStats = CreateEmpty<PlayerStatData>();
    private DataTable<PerkNodeData> _perkNodes = CreateEmpty<PerkNodeData>();
    private DataTable<PerkEffectData> _perkEffects = CreateEmpty<PerkEffectData>();
    private DataTable<WeaponData> _weapons = CreateEmpty<WeaponData>();
    private DataTable<SkillData> _skills = CreateEmpty<SkillData>();
    private DataTable<ConsumableData> _consumables = CreateEmpty<ConsumableData>();

    public DataTable<PlayerStatData> PlayerStats { get { return _playerStats; } }
    public DataTable<PerkNodeData> PerkNodes { get { return _perkNodes; } }
    public DataTable<PerkEffectData> PerkEffects { get { return _perkEffects; } }
    public DataTable<WeaponData> Weapons { get { return _weapons; } }
    public DataTable<SkillData> Skills { get { return _skills; } }
    public DataTable<ConsumableData> Consumables { get { return _consumables; } }

    public async UniTask LoadAsync(IDataTableLoader loader, CancellationToken token)
    {
        (_playerStats, _perkNodes, _perkEffects, _weapons, _skills, _consumables) = await UniTask.WhenAll(
            LoadTableAsync<PlayerStatData>(loader, token),
            LoadTableAsync<PerkNodeData>(loader, token),
            LoadTableAsync<PerkEffectData>(loader, token),
            LoadTableAsync<WeaponData>(loader, token),
            LoadTableAsync<SkillData>(loader, token),
            LoadTableAsync<ConsumableData>(loader, token));
    }

    private static async UniTask<DataTable<T>> LoadTableAsync<T>(IDataTableLoader loader, CancellationToken token) where T : BaseData
    {
        string tableName = typeof(T).Name;
        string json = await loader.LoadTextAsync(tableName, token);

        if (null == json)
        {
            Debug.LogError($"데이터 표 : {tableName}을(를) 찾지 못했습니다. 에디터에서 AutoDraw/데이터 가져오기를 했는지 확인하세요.");
            return CreateEmpty<T>();
        }

        try
        {
            return new DataTable<T>(tableName, JsonUtility.FromJson<TableJson<T>>(json).Items);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"데이터 표 : {tableName}의 JSON을 읽지 못했습니다. {exception.Message}");
            return CreateEmpty<T>();
        }
    }

    private static DataTable<T> CreateEmpty<T>() where T : BaseData
    {
        return new DataTable<T>(typeof(T).Name, null);
    }

    [Serializable]
    private class TableJson<T>
    {
        [SerializeField] private T[] _items;

        public T[] Items { get { return _items; } }
    }
}
