public static class UtilityTypeExtensions
{
    // 유틸리티는 표가 없어 아이콘 키를 종류 이름으로 정한다. AssetRegistry의 키와 맞춘다.
    public static string GetIconKey(this UtilityType utility)
    {
        return $"Icon/Utility/{utility}";
    }
}
