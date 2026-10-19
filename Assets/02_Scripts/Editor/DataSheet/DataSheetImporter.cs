using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEditor;
using UnityEngine;

// 오류가 하나라도 있으면 파일을 하나도 바꾸지 않는다(일부 표만 바뀌어 서로 어긋나지 않게).
public static class DataSheetImporter
{
    private const string MENU_PATH = "AutoDraw/데이터 가져오기";
    private const string DIALOG_TITLE = "데이터 가져오기";
    private const int TIMEOUT_SECONDS = 15;

    private static readonly IDataSheetRule[] RULES = { new PerkTreeRule(), new PerkEffectTargetRule(), new StageSkillRule() };

    [MenuItem(MENU_PATH)]
    private static void Import()
    {
        DataSheetConfig config = FindConfig();

        if (null == config)
        {
            return;
        }

        List<string> errors = new();
        Dictionary<string, DataSheetTable> tables = DownloadTables(config, errors);
        Dictionary<string, string> jsons = new();

        if (0 == errors.Count)
        {
            foreach (IDataSheetRule rule in RULES)
            {
                rule.Check(tables, errors);
            }

            foreach (DataSheetTable table in tables.Values)
            {
                jsons.Add(table.Name, table.ToJson(errors));
            }
        }

        if (0 < errors.Count)
        {
            ReportErrors(errors);
            return;
        }

        int changedCount = WriteJsons(config.OutputFolder, jsons);
        AssetDatabase.Refresh();

        Debug.Log($"데이터 가져오기 : 표 {jsons.Count}개를 받았고 그중 {changedCount}개가 바뀌었습니다.");
    }

    private static DataSheetConfig FindConfig()
    {
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(DataSheetConfig)}");

        if (0 == guids.Length)
        {
            EditorUtility.DisplayDialog(DIALOG_TITLE, "DataSheetConfig 에셋이 없습니다.", "확인");
            return null;
        }

        if (1 < guids.Length)
        {
            Debug.LogWarning("데이터 가져오기 : DataSheetConfig가 여러 개라 처음 찾은 것을 씁니다.");
        }

        return AssetDatabase.LoadAssetAtPath<DataSheetConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static Dictionary<string, DataSheetTable> DownloadTables(DataSheetConfig config, List<string> errors)
    {
        Dictionary<string, DataSheetTable> tables = new();
        Dictionary<string, Type> gameEnums = CollectGameEnums();

        if (string.IsNullOrWhiteSpace(config.SheetId))
        {
            errors.Add("DataSheetConfig : 시트 ID가 비어 있습니다.");
            return tables;
        }

        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS) };

        try
        {
            for (int i = 0; i < config.Tabs.Count; i++)
            {
                DataSheetConfig.Tab tab = config.Tabs[i];
                EditorUtility.DisplayProgressBar(DIALOG_TITLE, tab.Name, (float)i / config.Tabs.Count);

                if (string.IsNullOrWhiteSpace(tab.Name) || string.IsNullOrWhiteSpace(tab.Gid))
                {
                    errors.Add($"DataSheetConfig : {i}번 탭의 이름이나 gid가 비어 있습니다.");
                    continue;
                }

                if (tables.ContainsKey(tab.Name))
                {
                    errors.Add($"DataSheetConfig : 탭 {tab.Name}이(가) 두 번 있습니다.");
                    continue;
                }

                string csv = Download(client, config.GetCsvUrl(tab), tab.Name, errors);

                if (null == csv)
                {
                    continue;
                }

                DataSheetTable table = DataSheetTable.Parse(tab.Name, CsvReader.Read(csv), gameEnums.GetValueOrDefault, errors);

                if (null != table)
                {
                    tables.Add(tab.Name, table);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        return tables;
    }

    // 패키지의 같은 이름 enum과 섞이지 않게 게임 코드 어셈블리에서만 찾는다.
    private static Dictionary<string, Type> CollectGameEnums()
    {
        Dictionary<string, Type> enums = new();

        foreach (Type type in typeof(BaseData).Assembly.GetTypes())
        {
            if (type.IsEnum && false == type.IsNested)
            {
                enums[type.Name] = type;
            }
        }

        return enums;
    }

    // 공유가 꺼져 있으면 CSV 대신 구글 로그인 화면(HTML)이 온다.
    private static string Download(HttpClient client, string url, string tabName, List<string> errors)
    {
        try
        {
            using HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();

            if (false == response.IsSuccessStatusCode)
            {
                errors.Add($"{tabName} : 받지 못했습니다({(int)response.StatusCode}). gid가 맞는지 확인하세요.");
                return null;
            }

            string mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

            if (false == mediaType.Contains("csv"))
            {
                errors.Add($"{tabName} : CSV가 아닌 응답({mediaType})이 왔습니다. 시트를 '링크가 있는 모든 사용자: 뷰어'로 공유했는지 확인하세요.");
                return null;
            }

            byte[] bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            return Encoding.UTF8.GetString(bytes).TrimStart('﻿');
        }
        catch (Exception exception)
        {
            errors.Add($"{tabName} : 받는 중 오류가 났습니다. {exception.Message}");
            return null;
        }
    }

    private static int WriteJsons(string folder, Dictionary<string, string> jsons)
    {
        Directory.CreateDirectory(folder);
        int changedCount = 0;

        foreach (KeyValuePair<string, string> pair in jsons)
        {
            string path = Path.Combine(folder, pair.Key + ".json");

            if (File.Exists(path) && pair.Value == File.ReadAllText(path))
            {
                continue;
            }

            File.WriteAllText(path, pair.Value, new UTF8Encoding(false));
            changedCount++;
        }

        return changedCount;
    }

    private static void ReportErrors(List<string> errors)
    {
        foreach (string error in errors)
        {
            Debug.LogError($"데이터 가져오기 : {error}");
        }

        EditorUtility.DisplayDialog(DIALOG_TITLE, $"오류 {errors.Count}개가 있어 파일을 바꾸지 않았습니다. 콘솔을 확인하세요.", "확인");
    }
}
