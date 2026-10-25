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
}
