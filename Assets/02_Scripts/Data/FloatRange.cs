using System;
using UnityEngine;

// 최소~최대 사이에서 무작위로 고르는 값. 최대를 최소보다 작게 넣어도 최소로 맞춘다.
[Serializable]
public struct FloatRange
{
    [SerializeField, Min(0f)] private float _min;
    [SerializeField, Min(0f)] private float _max;

    public FloatRange(float min, float max)
    {
        _min = min;
        _max = max;
    }

    public float Min { get { return _min; } }
    public float Max { get { return Mathf.Max(_min, _max); } }

    public float PickRandom()
    {
        return UnityEngine.Random.Range(Min, Max);
    }
}
