using System;
using UnityEngine;

[Serializable]
public class TrajectorySettings
{
    private const float MIN_SPEED = 0.01f;

    [SerializeField] private TrajectoryType _type = TrajectoryType.Arc;
    [SerializeField, Min(0f)] private float _weight = 1f;
    [SerializeField] private FloatRange _height = new(2f, 4f);
    [SerializeField] private FloatRange _duration = new(0.5f, 0.7f);
    // 켜면 거리와 상관없이 같은 속도로 난다. 끄면 정해진 시간에 도착해 멀수록 빨라진다.
    [SerializeField] private bool _useSpeed;
    [SerializeField] private FloatRange _speed = new(8f, 10f);

    public TrajectoryType Type { get { return _type; } }
    public float Weight { get { return _weight; } }
    public FloatRange Height { get { return _height; } }

    public float PickDuration(float distance)
    {
        if (_useSpeed)
        {
            return distance / Mathf.Max(MIN_SPEED, _speed.PickRandom());
        }

        return _duration.PickRandom();
    }
}
