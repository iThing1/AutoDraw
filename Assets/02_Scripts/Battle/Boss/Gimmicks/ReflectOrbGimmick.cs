using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 구체 되받아치기. 보스가 오른손에 큰 구체를 모아 플레이어에게 던지고, 막기로 받아내면 색이 바뀌어 보스에게 되돌아간다.
// 여러 번 던지는 동안 정해진 횟수 이상 되돌려 보내면 깨진다. 남은 시도로 성공·실패가 정해지면 그 자리에서 끝낸다.
[CreateAssetMenu(fileName = "ReflectOrbGimmick", menuName = "Scriptable Objects/Boss/Gimmick/ReflectOrb")]
public class ReflectOrbGimmick : PunishingBossGimmick
{
    [Header("횟수")]
    [SerializeField, Min(1)] private int _castCount = 5;
    [SerializeField, Min(1)] private int _requiredReflects = 3;
    // 한 번 던진 구체가 끝난 뒤 다음을 모으기 시작할 때까지.
    [SerializeField, Min(0f)] private float _castInterval = 0.8f;

    [Header("모으기")]
    // 오른손에 구체가 맺히며 커지는 예고. 다 커지는 순간 던진다.
    [SerializeField] private GameObject _chargePrefab;
    [SerializeField, Min(0f)] private float _chargeDuration = 0.8f;
    [SerializeField, Min(0f)] private float _chargeDiameter = 1.6f;

    [Header("구체")]
    // 보스 탄 프리팹을 크게 키워 쓴다.
    [SerializeField] private GameObject _orbPrefab;
    [SerializeField] private Color _orbColor = new Color(0.35f, 0.08f, 0.45f, 1f);
    [SerializeField, Min(0.1f)] private float _orbScale = 2.6f;
    // 던질 때마다 이 범위에서 빠르기를 골라, 같은 박자로만 막아서는 깨기 어렵게 한다.
    [SerializeField] private FloatRange _orbSpeed = new FloatRange(7f, 15f);
    // 초당 꺾을 수 있는 각도. 클수록 끈질기게 따라온다.
    [SerializeField, Min(0f)] private float _turnSpeed = 90f;
    [SerializeField, Min(0f)] private float _lifetime = 6f;
    [SerializeField, Min(0f)] private float _trailTime = 0.2f;
    // 투사체 관련 값은 쓰지 않고 피해·타격 프리셋만 쓴다.
    [SerializeField] private ShotSettings _orbHit = new();

    [Header("되받아침")]
    [SerializeField] private Color _reflectColor = new Color(0.2f, 0.75f, 1f, 1f);
    [SerializeField] private ShotSettings _reflectShot = new();
    [SerializeField] private ImpactSettings _reflectImpact = new();

    public override void Prewarm()
    {
        if (null != _chargePrefab)
        {
            GameManager.Pool.Prewarm(_chargePrefab, 1);
        }

        if (null != _orbPrefab)
        {
            GameManager.Pool.Prewarm(_orbPrefab, 2);
        }
    }

    public override async UniTask<bool> PlayAsync(BattleUnit boss, CancellationToken token)
    {
        if (null == _orbPrefab || null == _chargePrefab)
        {
            Debug.LogError($"구체 되받아치기 : {name}에 구체나 모으기 프리팹이 지정되지 않았습니다.");
            return true;
        }

        int reflectCount = 0;

        for (int cast = 0; cast < _castCount; cast++)
        {
            int remaining = _castCount - cast;

            if (reflectCount >= _requiredReflects || reflectCount + remaining < _requiredReflects)
            {
                break;
            }

            if (await CastAsync(boss, token))
            {
                reflectCount++;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_castInterval), cancellationToken: token);
        }

        bool isBroken = reflectCount >= _requiredReflects;

        if (false == isBroken)
        {
            PunishTarget(boss);
        }

        return isBroken;
    }

    // 막아 냈으면 true. 그냥 맞으면 피해를 입고, 피하면 아무 일 없이 넘어간다.
    private async UniTask<bool> CastAsync(BattleUnit boss, CancellationToken token)
    {
        AutoAttacker attacker = boss.Attacker;
        AttackTarget target = attacker.Target;
        BossHands hands = attacker.Hands;
        Transform hand = null != hands ? hands.RightHand : boss.Body;

        try
        {
            if (null != hands)
            {
                hands.PoseAsync(HandGesture.RightWindUp, target.GetAimPoint(Vector3.zero), _chargeDuration, token).Forget();
            }

            await ChargeAsync(hand, token);

            FlightResult flight = await FlyOrbAsync(target, hand.position, token);

            if (FlightOutcome.Arrived == flight.Outcome)
            {
                attacker.StrikeUnblockable(_orbHit, flight.LandingPoint);
                return false;
            }

            if (FlightOutcome.Blocked != flight.Outcome)
            {
                return false;
            }

            target.NotifyBlocked(flight.LandingPoint);

            await ProjectileFlight.FlyAsync(_orbPrefab, _reflectShot, flight.LandingPoint, boss.Body.position, _reflectColor, token);

            ImpactPlayer.Play(_reflectImpact, boss.Body.position, _reflectColor);

            return true;
        }
        finally
        {
            if (null != hands)
            {
                hands.ReturnToRest();
            }
        }
    }

    // 일정한 빠르기로 방향만 꺾어 쫓아온다. 목표가 움직인 만큼 위치를 더해 주는 보통 탄과 달리, 플레이어가 빨리 달려도 구체가 빨라지지 않는다.
    // 게임 시간으로 날고, 시간 왜곡 안에서는 다른 탄처럼 느려진다. 수명이 다하면 빗나간 것이다.
    private async UniTask<FlightResult> FlyOrbAsync(AttackTarget target, Vector3 start, CancellationToken token)
    {
        GameObject orb = GameManager.Pool.Spawn(_orbPrefab, start, Quaternion.identity);
        Transform orbTransform = orb.transform;
        orbTransform.localScale = _orbPrefab.transform.localScale * _orbScale;

        float radius = 0f;

        if (orb.TryGetComponent(out SpriteRenderer orbRenderer))
        {
            orbRenderer.color = _orbColor;
            radius = orbRenderer.bounds.extents.x;
        }

        if (orb.TryGetComponent(out TrailRenderer trail))
        {
            // 같은 프리팹을 쓰는 보스 탄이 꼬리 설정을 바꿔 두므로 매번 다시 맞춘다.
            trail.Clear();
            trail.emitting = 0f < _trailTime;
            trail.time = _trailTime;
            trail.widthMultiplier = radius;
            trail.startColor = _orbColor;
            trail.endColor = new Color(_orbColor.r, _orbColor.g, _orbColor.b, 0f);
        }

        float speed = _orbSpeed.PickRandom();
        Vector3 direction = (target.GetAimPoint(Vector3.zero) - start).normalized;
        float elapsed = 0f;

        try
        {
            while (elapsed < _lifetime)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);

                float deltaTime = Time.deltaTime * GameManager.SlowZones.GetProjectileScale(orbTransform.position);
                elapsed += Time.deltaTime;

                Vector3 toTarget = target.GetAimPoint(Vector3.zero) - orbTransform.position;
                direction = Vector3.RotateTowards(direction, toTarget.normalized, _turnSpeed * Mathf.Deg2Rad * deltaTime, 0f);
                orbTransform.position += direction * (speed * deltaTime);

                Vector3 position = orbTransform.position;

                if (target.IsBlockedAt(position, radius))
                {
                    return new FlightResult(FlightOutcome.Blocked, position);
                }

                if (target.IsTouching(position, radius))
                {
                    return new FlightResult(FlightOutcome.Arrived, position);
                }
            }

            return new FlightResult(FlightOutcome.Missed, orbTransform.position);
        }
        finally
        {
            GameManager.Pool.Despawn(orb);
        }
    }

    // 손에 붙은 채로 구체가 커져, 던질 때까지 손을 따라다닌다.
    private async UniTask ChargeAsync(Transform hand, CancellationToken token)
    {
        SpriteRenderer charge = Telegraph.Spawn(_chargePrefab, hand.position, 0f, Vector2.zero, _orbColor);
        float elapsed = 0f;

        try
        {
            while (elapsed < _chargeDuration)
            {
                elapsed += Time.deltaTime;

                float diameter = _chargeDiameter * DOVirtual.EasedValue(0f, 1f, elapsed / _chargeDuration, Ease.OutQuad);
                charge.transform.position = hand.position;
                charge.transform.localScale = new Vector3(diameter, diameter, 1f);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            Telegraph.Despawn(charge);
        }
    }
}
