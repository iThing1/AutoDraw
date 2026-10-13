using System;
using UnityEngine;

[Serializable]
public class TrajectorySettings
{
    [SerializeField] private TrajectoryType _type = TrajectoryType.Arc;
    [SerializeField, Min(0f)] private float _weight = 1f;
    [SerializeField] private FloatRange _height = new(2f, 4f);
    [SerializeField] private FloatRange _duration = new(0.5f, 0.7f);

    public TrajectoryType Type { get { return _type; } }
    public float Weight { get { return _weight; } }
    public FloatRange Height { get { return _height; } }
    public FloatRange Duration { get { return _duration; } }
}
