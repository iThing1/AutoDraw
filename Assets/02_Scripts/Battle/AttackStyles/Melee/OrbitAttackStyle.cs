using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 몸에서 구슬이 나와 둘레를 돌며 닿은 목표를 계속 때린다. 이 공격을 고른 동안에는 사라지지 않고,
// 다른 공격으로 바꾸면 몸으로 들어간다. 에셋에서 WaitUntilFinished를 켜 두어 도는 동안 다른 공격이 겹치지 않게 한다.
[CreateAssetMenu(fileName = "OrbitAttackStyle", menuName = "Scriptable Objects/AttackStyle/Orbit")]
public class OrbitAttackStyle : AttackStyle
{
    [Header("구슬")]
    [SerializeField] private GameObject _orbPrefab;
    [SerializeField, Min(1)] private int _orbCount = 3;
    [SerializeField, Min(0.1f)] private float _orbDiameter = 0.7f;
    [SerializeField, Min(0.1f)] private float _orbitRadius = 1.6f;
    // 한 바퀴 도는 시간.
    [SerializeField, Min(0.1f)] private float _orbitPeriod = 1.2f;
    // 몸에서 나오고 몸으로 들어가는 시간.
    [SerializeField, Min(0.01f)] private float _transitionDuration = 0.25f;

    [Header("타격")]
    // 같은 구슬이 다시 때리기까지의 간격.
    [SerializeField, Min(0.05f)] private float _hitInterval = 0.4f;
    [SerializeField] private HitSettings _hit = new();

    public override void Prewarm()
    {
        if (null != _orbPrefab)
        {
            GameManager.Pool.Prewarm(_orbPrefab, _orbCount);
        }
    }

    // 끊기거나 전투가 끝나 취소되면 들어가는 연출 없이 바로 거둔다.
    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _orbPrefab)
        {
            Debug.LogError($"공전 구슬 : {name}에 구슬 프리팹이 지정되지 않았습니다.");
            return;
        }

        SpriteRenderer[] orbs = new SpriteRenderer[_orbCount];
        float[] nextHitTimes = new float[_orbCount];
        float angle = 0f;

        try
        {
            for (int i = 0; i < _orbCount; i++)
            {
                orbs[i] = PooledSprite.Spawn(_orbPrefab, context.Owner.Body.position, 0f, Vector2.zero, context.Damage.PickColor());
            }

            for (float t = 0f; t < _transitionDuration; t += Time.deltaTime)
            {
                angle = Orbit(context, orbs, nextHitTimes, angle, t / _transitionDuration, gaugeGain);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            while (IsSelected(context))
            {
                angle = Orbit(context, orbs, nextHitTimes, angle, 1f, gaugeGain);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            for (float t = 0f; t < _transitionDuration; t += Time.deltaTime)
            {
                angle = Orbit(context, orbs, nextHitTimes, angle, 1f - t / _transitionDuration, gaugeGain);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            for (int i = 0; i < orbs.Length; i++)
            {
                PooledSprite.Despawn(orbs[i]);
            }
        }
    }

    // 지금 공격이 이 에셋인지로 고른 상태를 본다. AttackStyleSet 안에 넣으면 묶음이 지금 공격이 되어 바로 들어가 버리므로 단독으로 쓴다.
    private bool IsSelected(AttackContext context)
    {
        return this == context.Owner.AttackStyle && false == context.Owner.Health.IsDead && false == context.Target.IsDefeated;
    }

    // spread가 0이면 몸 안, 1이면 다 나온 상태다. 게임 시간으로 돌아 히트 스톱 동안에는 함께 멈춘다.
    private float Orbit(AttackContext context, SpriteRenderer[] orbs, float[] nextHitTimes, float angle, float spread, float gaugeGain)
    {
        angle = Mathf.Repeat(angle + 360f / _orbitPeriod * Time.deltaTime, 360f);

        Vector3 center = context.Owner.Body.position;
        float radius = _orbitRadius * spread;
        float diameter = _orbDiameter * spread;

        for (int i = 0; i < orbs.Length; i++)
        {
            float orbAngle = (angle + 360f * i / orbs.Length) * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(Mathf.Cos(orbAngle), Mathf.Sin(orbAngle), 0f) * radius;

            orbs[i].transform.position = position;
            orbs[i].transform.localScale = new Vector3(diameter, diameter, 1f);

            if (Time.time >= nextHitTimes[i] && context.Target.IsTouching(position, diameter * 0.5f))
            {
                nextHitTimes[i] = Time.time + _hitInterval;
                context.Damage.Strike(_hit, position, gaugeGain);
            }
        }

        return angle;
    }
}
