using UnityEngine;

// 2D 평면 계산을 모은 곳. 각도는 도 단위로, 오른쪽이 0이고 반시계로 커진다.
public static class Geometry2D
{
    public static Vector3 ToDirection(float degrees)
    {
        float radian = degrees * Mathf.Deg2Rad;

        return new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f);
    }

    public static float ToAngle(Vector3 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    // from에서 to까지의 선분 위에서 point와 가장 가까운 점. 길이가 0이면 from이다.
    public static Vector3 GetClosestPointOnSegment(Vector3 from, Vector3 to, Vector3 point)
    {
        Vector3 segment = to - from;
        float sqrLength = segment.sqrMagnitude;

        if (0f >= sqrLength)
        {
            return from;
        }

        float t = Mathf.Clamp01(Vector3.Dot(point - from, segment) / sqrLength);

        return from + segment * t;
    }

    // center를 가운데로 하는 size 크기 네모를 area 안으로 밀어 넣는다. area보다 크면 area 크기로 줄인다.
    public static Rect FitRect(Rect area, Vector3 center, Vector2 size)
    {
        size = Vector2.Min(size, area.size);

        float x = Mathf.Clamp(center.x - size.x * 0.5f, area.xMin, area.xMax - size.x);
        float y = Mathf.Clamp(center.y - size.y * 0.5f, area.yMin, area.yMax - size.y);

        return new Rect(x, y, size.x, size.y);
    }

    // rect를 columns x rows 칸으로 나눴을 때 position이 든 칸 번호. 왼쪽 아래부터 가로로 세고, 밖이면 가장 가까운 칸이다.
    public static int GetCellIndex(Rect rect, int columns, int rows, Vector3 position)
    {
        int column = Mathf.Clamp(Mathf.FloorToInt((position.x - rect.xMin) / rect.width * columns), 0, columns - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt((position.y - rect.yMin) / rect.height * rows), 0, rows - 1);

        return row * columns + column;
    }
}
