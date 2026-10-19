using UnityEngine;

// 먹물 삼킴 연출에서 매 프레임 바뀌는 값. 모든 팔이 같은 값을 쓰므로 한 번 계산해 넘긴다.
public struct SwallowFrame
{
    public float Elapsed;
    // 모든 손이 닿은 뒤 움켜쥐는 정도(0~1).
    public float Grab;
    // 끌고 가는 진행도(0~1). 이징을 거치지 않은 값이다.
    public float PullProgress;
    public Vector3 Center;
    public Vector3 TargetPosition;
    // 대상의 남은 크기 비율. 가라앉으며 0이 된다.
    public float Remain;

    public static float GetProgress(float elapsed, float start, float duration)
    {
        if (0f >= duration)
        {
            return elapsed >= start ? 1f : 0f;
        }

        return Mathf.Clamp01((elapsed - start) / duration);
    }
}
