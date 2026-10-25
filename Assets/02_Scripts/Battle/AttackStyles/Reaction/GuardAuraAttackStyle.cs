using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 보스를 감싸는 막을 잠시 펼쳐, 그동안 닿은 플레이어 공격(탄·근접·스킬)을 막는다. 막은 공격을 어떻게 돌려줄지는 하위 클래스가 정한다.
// 보스가 막는 것은 플레이어 방패와 같은 규칙(BattleUnit.BeginGuard)이라, 탄은 막 가장자리에 닿는 순간 막힌다.
// 막이 떠 있는 동안에도 보스는 다른 패턴을 이어 가도록, 에셋에서 끝까지 기다리기를 끈다.
public abstract class GuardAuraAttackStyle : UtilityReactionAttackStyle
{
    [Header("막")]
    [SerializeField, Min(0.1f)] private float _radius = 2.5f;
    [SerializeField, Min(0f)] private float _duration = 4f;
    // 막 그림. 지름을 막 반경의 두 배로 맞춰, 보이는 막과 막히는 범위를 같게 한다.
    [SerializeField] private GameObject _auraPrefab;
    [SerializeField] private Color _auraColor = new Color(0.4f, 0.6f, 0.95f, 0.3f);
    [SerializeField] private ImpactSettings _startImpact = new();

    public override void Prewarm()
    {
        if (null != _auraPrefab)
        {
            GameManager.Pool.Prewarm(_auraPrefab, 1);
        }
    }

    protected override async UniTask ReactAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        BattleUnit owner = context.Owner;
        float diameter = _radius * 2f;
        SpriteRenderer aura = null != _auraPrefab ? PooledSprite.Spawn(_auraPrefab, owner.Body.position, 0f, new Vector2(diameter, diameter), _auraColor) : null;
        Action<Vector3> handleBlocked = point => OnBlocked(context, point, gaugeGain, token);

        ImpactPlayer.Play(_startImpact, owner.Body.position);
        owner.OnBlocked += handleBlocked;

        try
        {
            using (owner.BeginGuard(_radius))
            {
                float elapsed = 0f;

                while (elapsed < _duration)
                {
                    if (null != aura)
                    {
                        aura.transform.position = owner.Body.position;
                    }

                    OnTick(context);

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    elapsed += Time.deltaTime;
                }
            }
        }
        finally
        {
            owner.OnBlocked -= handleBlocked;
            OnEnd(context);
            PooledSprite.Despawn(aura);
        }
    }

    // 막이 플레이어 공격을 막은 자리. 하위 클래스가 되쏘거나 흘려보내는 연출을 붙인다.
    protected abstract void OnBlocked(AttackContext context, Vector3 point, float gaugeGain, CancellationToken token);

    // 막이 떠 있는 동안 매 프레임 부른다.
    protected virtual void OnTick(AttackContext context)
    {
    }

    // 막이 끝나거나 끊겼을 때 부른다. 매 프레임 바꾼 것을 되돌린다.
    protected virtual void OnEnd(AttackContext context)
    {
    }
}
