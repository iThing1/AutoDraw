using System;
using UnityEngine;

// 최소~최대 사이에서 무작위로 고르는 정수. 최대값도 나올 수 있다.
[Serializable]
public struct IntRange
{
    [SerializeField, Min(0)] private int _min;
    [SerializeField, Min(0)] private int _max;

    public IntRange(int min, int max)
    {
        _min = min;
        _max = max;
    }

    public int Min { get { return _min; } }
    public int Max { get { return Mathf.Max(_min, _max); } }

    // int 버전 Random.Range는 최대값을 포함하지 않으므로 1을 더한다.
    public int Pick()
    {
        return UnityEngine.Random.Range(Min, Max + 1);
    }
}
