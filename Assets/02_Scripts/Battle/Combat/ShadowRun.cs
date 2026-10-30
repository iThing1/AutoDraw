using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 먹 그림자를 풀에서 꺼내 길을 따라 일정한 빠르기로 달리게 하고, 몸에 닿으면 한 번 맞힌 뒤 돌려준다.
public static class ShadowRun
{
    public static async UniTask RunAsync(AttackContext context, ShadowRunSettings shadow, Polyline path, float gaugeGain, CancellationToken token)
    {
        if (null == shadow.Prefab)
        {
            return;
        }

        float diameter = shadow.Diameter;
        float radius = diameter * 0.5f;

        SpriteRenderer sprite = PooledSprite.Spawn(shadow.Prefab, path.Points[0], 0f, new Vector2(diameter, diameter), shadow.Color);
        AttackTarget target = context.Target;
        float travelled = 0f;
        int segment = 0;
        bool hasStruck = false;

        try
        {
            while (travelled < path.Length)
            {
                Vector3 previous = sprite.transform.position;
                travelled = Mathf.Min(travelled + shadow.Speed * Time.deltaTime, path.Length);
                sprite.transform.position = path.GetPointAt(travelled, ref segment);

                // 꺾인 곳을 지난 프레임은 모서리를 질러 재지만, 꺾인 점 간격보다 그림자가 커서 차이가 나지 않는다.
                if (false == hasStruck && target.IsTouchingSegment(previous, sprite.transform.position, radius))
                {
                    hasStruck = true;
                    context.Damage.Strike(shadow.Hit, target.GetAimPoint(Vector3.zero), gaugeGain);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            PooledSprite.Despawn(sprite);
        }
    }
}
