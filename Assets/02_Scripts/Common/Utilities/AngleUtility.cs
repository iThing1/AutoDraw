using UnityEngine;

// 2D 평면에서 각도(도, 오른쪽이 0이고 반시계로 커짐)와 방향을 서로 바꾼다. 원·부채꼴로 쏘거나 도는 패턴이 함께 쓴다.
public static class AngleUtility
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
}
