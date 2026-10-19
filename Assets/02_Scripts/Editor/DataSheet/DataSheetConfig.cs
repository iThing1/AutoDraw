using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DataSheetConfig", menuName = "Scriptable Objects/DataSheetConfig")]
public class DataSheetConfig : ScriptableObject
{
    [Serializable]
    public class Tab
    {
        // 탭 이름 = 표 이름 = JSON 파일 이름.
        [SerializeField] private string _name;
        // 탭 주소의 gid= 뒤 숫자.
        [SerializeField] private string _gid;

        public string Name { get { return _name; } }
        public string Gid { get { return _gid; } }
    }

    // 시트 주소의 /d/와 /edit 사이 값.
    [SerializeField] private string _sheetId;
    [SerializeField] private string _outputFolder = "Assets/Resources/JsonOutput";
    [SerializeField] private List<Tab> _tabs = new();

    public string SheetId { get { return _sheetId; } }
    public string OutputFolder { get { return _outputFolder; } }
    public IReadOnlyList<Tab> Tabs { get { return _tabs; } }

    public string GetCsvUrl(Tab tab)
    {
        return $"https://docs.google.com/spreadsheets/d/{_sheetId}/export?format=csv&gid={tab.Gid}";
    }
}
