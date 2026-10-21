using UnityEngine;

// 쓰고 나면 다시 쓸 수 있을 때까지 기다리는 시간. 막기·소모품처럼 쿨타임이 있는 것이 같이 쓴다.
// 시간은 부르는 쪽이 넘긴다(게임 시간).
public class Cooldown
{
    private float _startTime;
    private float _readyTime;

    public bool IsReady(float now)
    {
        return now >= _readyTime;
    }

    // 0이면 막 시작했고 1이면 다 찼다.
    public float GetRatio(float now)
    {
        float duration = _readyTime - _startTime;

        return 0f < duration ? Mathf.Clamp01((now - _startTime) / duration) : 1f;
    }

    public void Start(float now, float duration)
    {
        _startTime = now;
        _readyTime = now + duration;
    }

    public void Clear()
    {
        _startTime = 0f;
        _readyTime = 0f;
    }
}
