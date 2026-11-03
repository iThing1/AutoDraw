public static class PlayerSaveStorage
{
    private const string FILE_NAME = "Player.json";
    private const string LABEL = "플레이어 저장";

    public static bool TryLoad(out PlayerSaveData save)
    {
        return JsonFileStorage.TryLoad(FILE_NAME, LABEL, out save);
    }

    public static void Save(PlayerSaveData save)
    {
        JsonFileStorage.Save(FILE_NAME, save);
    }

    // 시험할 때 처음 상태로 돌리려고 쓴다.
    public static void Delete()
    {
        JsonFileStorage.Delete(FILE_NAME);
    }
}
