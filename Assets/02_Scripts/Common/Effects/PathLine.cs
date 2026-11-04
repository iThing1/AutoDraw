using DG.Tweening;
using UnityEngine;

// 꺾은선을 처음부터 조금씩 그어 나가는 선 하나. 예고선과 남는 먹 선이 함께 쓴다.
// 프리팹은 월드 좌표를 쓰는 LineRenderer여야 한다. 꺼내고 돌려주는 것은 풀에 붙는 확장 메서드다.
public static class PathLine
{
    public static LineRenderer SpawnLine(this PoolManager pool, LineRenderer prefab, float width, Color color)
    {
        LineRenderer line = pool.Spawn(prefab, Vector3.zero, Quaternion.identity);

        line.positionCount = 0;
        line.widthMultiplier = width;
        SetColor(line, color);

        return line;
    }

    // 길 처음부터 segment 구간 안의 tip까지만 보이게 한다. 이미 지난 꺾인 점은 그대로 두고, 지난번 끝점 자리부터 다시 채운다.
    public static void Reveal(LineRenderer line, Polyline path, int segment, Vector3 tip)
    {
        int count = segment + 2;

        if (line.positionCount != count)
        {
            int from = Mathf.Max(0, line.positionCount - 1);
            line.positionCount = count;

            for (int i = from; i <= segment; i++)
            {
                line.SetPosition(i, path.Points[i]);
            }
        }

        line.SetPosition(count - 1, tip);
    }

    public static void SetColor(LineRenderer line, Color color)
    {
        line.startColor = color;
        line.endColor = color;
    }

    // 걸려 있던 트윈을 함께 끊어, 다음에 꺼낸 선이 이전 트윈에 흐려지지 않게 한다.
    public static void DespawnLine(this PoolManager pool, LineRenderer line)
    {
        if (null == line)
        {
            return;
        }

        DOTween.Kill(line);
        pool.Despawn(line.gameObject);
    }
}
