using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using Object = UnityEngine.Object;

// 빈 키는 아직 만들지 않은 에셋이라 넘어간다. IconKey는 아이콘을 넣을 때 검사를 더한다.
public class AssetKeyRule : IDataSheetRule
{
    // 표, 키 열, 그 키가 가리켜야 하는 에셋 형식.
    private static readonly (string Table, string Field, Type AssetType)[] KEYS =
    {
        ("WeaponData", "AttackStyleKey", typeof(AttackStyle)),
        ("WeaponData", "PreviewKey", typeof(VideoClip)),
        ("SkillData", "AttackStyleKey", typeof(AttackStyle)),
        ("SkillData", "PreviewKey", typeof(VideoClip)),
        ("ConsumableData", "EffectKey", typeof(ConsumableEffect)),
    };

    public void Check(IReadOnlyDictionary<string, DataSheetTable> tables, List<string> errors)
    {
        Dictionary<string, Object> assets = CollectAssets(errors);

        if (null == assets)
        {
            return;
        }

        foreach ((string tableName, string field, Type assetType) in KEYS)
        {
            if (tables.TryGetValue(tableName, out DataSheetTable table))
            {
                CheckKeys(table, tableName, field, assetType, assets, errors);
            }
        }
    }

    private void CheckKeys(DataSheetTable table, string tableName, string field, Type assetType, Dictionary<string, Object> assets, List<string> errors)
    {
        for (int row = 0; row < table.RowCount; row++)
        {
            string key = table.GetCell(row, field);

            if (0 == key.Length)
            {
                continue;
            }

            string prefix = $"{tableName} {table.GetLineNumber(row)}줄 {field}";

            if (false == assets.TryGetValue(key, out Object asset))
            {
                errors.Add($"{prefix} : {key}이(가) AssetRegistry에 없습니다.");
            }
            else if (false == assetType.IsInstanceOfType(asset))
            {
                errors.Add($"{prefix} : {key}은(는) {assetType.Name}이(가) 아닙니다.");
            }
        }
    }

    private Dictionary<string, Object> CollectAssets(List<string> errors)
    {
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(AssetRegistry)}");

        if (0 == guids.Length)
        {
            errors.Add("AssetRegistry 에셋이 없습니다.");
            return null;
        }

        AssetRegistry registry = AssetDatabase.LoadAssetAtPath<AssetRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
        Dictionary<string, Object> assets = new();

        foreach (AssetRegistry.Entry entry in registry.Entries)
        {
            if (string.IsNullOrEmpty(entry.Key) || null == entry.Asset)
            {
                errors.Add("AssetRegistry : 키나 에셋이 빈 줄이 있습니다.");
                continue;
            }

            if (false == assets.TryAdd(entry.Key, entry.Asset))
            {
                errors.Add($"AssetRegistry : 키 {entry.Key}이(가) 겹칩니다.");
            }
        }

        return assets;
    }
}
