using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 천천히 목표를 쫓는 미사일을 시간차로 쏜다. 닿거나 방패에 막히거나 수명이 다하면 그 자리에서 터져 범위 피해를 준다.
[CreateAssetMenu(fileName = "HomingMissileAttackStyle", menuName = "Scriptable Objects/AttackStyle/HomingMissile")]
public class HomingMissileAttackStyle : AttackStyle
{
    [Header("발사")]
    [SerializeField] private GameObject _missilePrefab;
    [SerializeField, Min(1)] private int _missileCount = 3;
    [SerializeField, Min(0f)] private float _launchInterval = 0.4f;
    // 처음엔 목표 방향에서 이 각도 안으로 흩어져 나가, 세 발이 다른 길로 돌아 들어오게 한다.
    [SerializeField, Range(0f, 180f)] private float _launchSpreadAngle = 60f;
    [SerializeField] private AudioClip _launchSound;

    [Header("비행")]
    [SerializeField, Min(0f)] private float _speed = 6f;
    // 초당 꺾을 수 있는 각도. 작을수록 크게 돌아 들어와 피하기 쉽다.
    [SerializeField, Min(0f)] private float _turnSpeed = 120f;
    [SerializeField, Min(0f)] private float _lifetime = 5f;
    [SerializeField, Min(0f)] private float _collisionRadius = 0.3f;

    [Header("폭발")]
    [SerializeField, Min(0f)] private float _blastRadius = 2.5f;
    [SerializeField] private ImpactSettings _blastImpact = new();
    [SerializeField] private HitSettings _hit = new();

    // 미사일은 수명 동안 남아 있어 다음 공격과 겹칠 수 있으므로, 두 번 쏠 만큼 만들어 둔다.
    public override void Prewarm()
    {
        if (null != _missilePrefab)
        {
            GameManager.Pool.Prewarm(_missilePrefab, _missileCount * 2);
        }
    }

    // 발사만 기다리고, 날아가는 미사일은 각자 끝까지 날게 둔다. 5초씩 기다리면 다음 공격이 너무 늦어진다.
    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _missilePrefab)
        {
            Debug.LogError($"호밍 미사일 : {name}에 미사일 프리팹이 지정되지 않았습니다.");
            return;
        }

        for (int i = 0; i < _missileCount; i++)
        {
            GameManager.Sound.PlaySfx(_launchSound);
            FlyMissileAsync(context, gaugeGain, token).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(_launchInterval), cancellationToken: token);
        }
    }

    // 게임 시간으로 날아 히트 스톱 동안에는 함께 멈춘다. 전투가 끝나 취소되면 터지지 않고 사라진다.
    private async UniTaskVoid FlyMissileAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        AttackTarget target = context.Target;
        Vector3 start = context.Owner.Body.position;

        GameObject missile = GameManager.Pool.Spawn(_missilePrefab, start, Quaternion.identity);
        Transform missileTransform = missile.transform;

        if (missile.TryGetComponent(out SpriteRenderer missileRenderer))
        {
            missileRenderer.color = context.Damage.PickColor();
        }

        if (missile.TryGetComponent(out TrailRenderer trail))
        {
            trail.Clear();
        }

        float spread = UnityEngine.Random.Range(-_launchSpreadAngle, _launchSpreadAngle);
        Vector3 direction = Quaternion.Euler(0f, 0f, spread) * (target.GetAimPoint(Vector3.zero) - start).normalized;
        float elapsed = 0f;

        try
        {
            while (elapsed < _lifetime)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);

                float deltaTime = Time.deltaTime;
                elapsed += deltaTime;

                Vector3 toTarget = target.GetAimPoint(Vector3.zero) - missileTransform.position;
                direction = Vector3.RotateTowards(direction, toTarget.normalized, _turnSpeed * Mathf.Deg2Rad * deltaTime, 0f);

                missileTransform.position += direction * (_speed * deltaTime);
                missileTransform.rotation = Quaternion.Euler(0f, 0f, Geometry2D.ToAngle(direction));

                if (target.IsBlockedAt(missileTransform.position, _collisionRadius) || target.IsTouching(missileTransform.position, _collisionRadius))
                {
                    break;
                }
            }

            Explode(context, missileTransform.position, gaugeGain);
        }
        catch (OperationCanceledException)
        {
            // 전투가 끝나 날던 미사일이 취소된 경우라 터뜨리지 않는다.
        }
        finally
        {
            GameManager.Pool.Despawn(missile);
        }
    }

    // 방패에 닿아 멈췄어도 그 자리에서 터지고, 피해는 Blast가 방패를 다시 확인해 막는다.
    private void Explode(AttackContext context, Vector3 position, float gaugeGain)
    {
        ImpactPlayer.Play(_blastImpact, position);

        context.Damage.Blast(_hit, position, _blastRadius, gaugeGain);
    }
}
