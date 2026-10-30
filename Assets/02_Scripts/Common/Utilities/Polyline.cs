using System.Collections.Generic;
using UnityEngine;

// 점을 차례로 이은 꺾은선. 각 점까지 선을 따라 잰 거리를 미리 구해 두어, 처음부터 얼마만큼 간 자리를 바로 찾는다.
public class Polyline
{
    private readonly List<Vector3> _points;
    // 첫 점은 0, 마지막 점은 선 전체 길이다.
    private readonly float[] _distances;

    // 점이 둘 이상이어야 한다. 넘긴 목록을 그대로 쓰므로 넘긴 뒤에는 고치지 않는다.
    public Polyline(List<Vector3> points)
    {
        _points = points;
        _distances = new float[points.Count];

        for (int i = 1; i < points.Count; i++)
        {
            _distances[i] = _distances[i - 1] + Vector2.Distance(points[i - 1], points[i]);
        }
    }

    public IReadOnlyList<Vector3> Points { get { return _points; } }
    public float Length { get { return _distances[_distances.Length - 1]; } }

    // 처음부터 distance만큼 간 자리. segment는 지난번에 찾은 구간으로, 앞으로만 가는 쪽이 넘겨 매번 처음부터 찾지 않게 한다.
    public Vector3 GetPointAt(float distance, ref int segment)
    {
        while (segment < _points.Count - 2 && _distances[segment + 1] < distance)
        {
            segment++;
        }

        float t = Mathf.InverseLerp(_distances[segment], _distances[segment + 1], distance);

        return Vector3.Lerp(_points[segment], _points[segment + 1], t);
    }
}
