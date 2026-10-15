using System.IO;
using UnityEngine;

// 클리어할 때 찍은 그림을 저장하고 불러온다. 그림이 있으면 깬 스테이지로 본다.
public static class PaintingStorage
{
    private const string FOLDER_NAME = "Paintings";
    private const string EXTENSION = ".jpg";
    private const int JPG_QUALITY = 90;

    // 다시 깨면 덮어쓴다. 투명한 곳이 없는 그림이라 PNG보다 압축이 빠르고 작은 JPG로 저장한다.
    public static void Save(StageData stage, Texture2D painting)
    {
        if (null == stage || null == painting)
        {
            return;
        }

        string path = GetPath(stage);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, painting.EncodeToJPG(JPG_QUALITY));
    }

    // 불러온 텍스처는 쓰는 쪽이 다 쓰고 지운다. 그림이 없으면 null이다.
    public static Texture2D Load(StageData stage)
    {
        if (null == stage)
        {
            return null;
        }

        string path = GetPath(stage);

        if (false == File.Exists(path))
        {
            return null;
        }

        Texture2D painting = new Texture2D(2, 2);
        painting.LoadImage(File.ReadAllBytes(path));

        return painting;
    }

    private static string GetPath(StageData stage)
    {
        return Path.Combine(Application.persistentDataPath, FOLDER_NAME, stage.SaveKey + EXTENSION);
    }
}
