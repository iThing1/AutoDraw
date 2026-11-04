using System;
using System.IO;
using UnityEngine;

// 저장 파일(플레이어 진행, 설정)을 persistentDataPath 아래 Save 폴더에 JSON으로 읽고 쓴다.
public static class JsonFileStorage
{
    private const string FOLDER_NAME = "Save";
    private const string BACKUP_EXTENSION = ".bak";
    private const string TEMP_EXTENSION = ".tmp";

    // 깨진 파일은 .bak으로 옮겨 다음 저장이 덮어쓰지 않게 한다. label은 경고 문구 앞에 붙는다.
    public static bool TryLoad<T>(string fileName, string label, out T data) where T : class
    {
        data = null;
        string path = GetPath(fileName);

        if (false == File.Exists(path))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<T>(File.ReadAllText(path));
        }
        catch (Exception exception) when (exception is ArgumentException || exception is IOException)
        {
            Debug.LogWarning($"{label} : 저장 파일을 읽지 못했습니다. {exception.Message}");
        }

        if (null == data)
        {
            MoveToBackup(path, label);
            return false;
        }

        return true;
    }

    // 쓰는 도중 꺼져도 예전 파일이 남도록 임시 파일에 쓴 뒤 바꿔 넣는다.
    public static void Save<T>(string fileName, T data) where T : class
    {
        if (null == data)
        {
            return;
        }

        string path = GetPath(fileName);
        string tempPath = path + TEMP_EXTENSION;

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.Move(tempPath, path);
    }

    // 깨진 파일을 옮겨 둔 .bak은 남긴다.
    public static void Delete(string fileName)
    {
        string path = GetPath(fileName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void MoveToBackup(string path, string label)
    {
        string backupPath = path + BACKUP_EXTENSION;

        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        File.Move(path, backupPath);
        Debug.LogWarning($"{label} : 처음 값으로 시작합니다. 읽지 못한 파일은 {backupPath}에 옮겨 두었습니다.");
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, FOLDER_NAME, fileName);
    }
}
