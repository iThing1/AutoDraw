using System.Collections.Generic;

public static class ReadOnlyListExtensions
{
    // JsonUtility는 배열로 저장해, 읽기 전용 목록을 저장 데이터로 옮길 때 쓴다.
    public static T[] ToArray<T>(this IReadOnlyList<T> list)
    {
        T[] array = new T[list.Count];

        for (int i = 0; i < list.Count; i++)
        {
            array[i] = list[i];
        }

        return array;
    }
}
