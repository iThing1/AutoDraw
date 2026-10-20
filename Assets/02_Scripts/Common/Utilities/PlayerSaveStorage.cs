using System;
using System.IO;
using UnityEngine;

public static class PlayerSaveStorage
{
    private const string FOLDER_NAME = "Save";
    private const string FILE_NAME = "Player.json";
    private const string BACKUP_EXTENSION = ".bak";
    private const string TEMP_EXTENSION = ".tmp";

    // 깨진 파일은 .bak으로 옮겨 다음 저장이 덮어쓰지 않게 한다.
    public static bool TryLoad(out PlayerSaveData save)
    {
        save = null;
        string path = GetPath();

        if (false == File.Exists(path))
        {
            return false;
        }

        try
        {
            save = JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(path));
        }
        catch (Exception exception) when (exception is ArgumentException || exception is IOException)
        {
            Debug.LogWarning($"플레이어 저장 : 저장 파일을 읽지 못했습니다. {exception.Message}");
        }

        if (null == save)
        {
            MoveToBackup(path);
            return false;
        }

        return true;
    }

    // 쓰는 도중 꺼져도 예전 파일이 남도록 임시 파일에 쓴 뒤 바꿔 넣는다.
    public static void Save(PlayerSaveData save)
    {
        if (null == save)
        {
            return;
        }

        string path = GetPath();
        string tempPath = path + TEMP_EXTENSION;

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(tempPath, JsonUtility.ToJson(save, true));

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.Move(tempPath, path);
    }

    // 시험할 때 처음 상태로 돌리려고 쓴다. 깨진 파일을 옮겨 둔 .bak은 남긴다.
    public static void Delete()
    {
        string path = GetPath();

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void MoveToBackup(string path)
    {
        string backupPath = path + BACKUP_EXTENSION;

        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        File.Move(path, backupPath);
        Debug.LogWarning($"플레이어 저장 : 새로 시작합니다. 읽지 못한 파일은 {backupPath}에 옮겨 두었습니다.");
    }

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, FOLDER_NAME, FILE_NAME);
    }
}
