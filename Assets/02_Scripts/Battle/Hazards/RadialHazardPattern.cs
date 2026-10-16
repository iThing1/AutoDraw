using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 쏘는 자리에서 사방으로 탄을 퍼뜨린다. 추적하지 않아 플레이어가 빈 곳을 찾아 움직여야 한다.
// 빈틈을 두고 발사마다 옮기면 '빈틈 링', 빈틈 없이 몇 갈래를 조금씩 돌려 쏘면 '회전 나선'이 된다.
[CreateAssetMenu(fileName = "RadialHazardPattern", menuName = "Scriptable Objects/Hazard/Radial")]
public class RadialHazardPattern : HazardPattern
{
    private const float FULL_CIRCLE = 360f;

    [Header("투사체")]
    // 비워 두면 쏘는 쪽(보스)의 투사체를 쓴다.
    [SerializeField] private GameObject _projectilePrefab;

    [Header("예고")]
    [SerializeField] private AudioClip _warningSound;
    // 소리가 난 뒤 첫 발사까지의 시간. 다른 공격을 피하던 중에도 대비할 틈을 준다.
    [SerializeField, Min(0f)] private float _warningDuration = 0.6f;

    [Header("발사")]
    [SerializeField, Min(1)] private int _waveCount = 3;
    [SerializeField, Min(0f)] private float _waveInterval = 0.8f;
    [SerializeField, Min(1)] private int _shotsPerWave = 36;
    // 발사마다 전체 방향을 이만큼(도) 돌린다. 나선은 이 값으로 갈래가 회전한다.
    [SerializeField] private float _angleStepPerWave;
    // 화면 끝까지 날아가도록 넉넉히 잡는다. 비행 시간이 정해져 있어 사거리가 길수록 탄도 빨라진다.
    [SerializeField, Min(1f)] private float _range = 25f;
    [SerializeField] private ShotSettings _shot = new();

    [Header("빈틈")]
    // 탄을 쏘지 않고 비워 둘 각도(도). 0이면 빈틈 없이 모든 방향으로 쏜다.
    [SerializeField, Range(0f, FULL_CIRCLE)] private float _gapAngle;
    // 빈틈이 발사마다 옮겨 가는 각도(도). 첫 빈틈은 플레이어 쪽에서 이만큼 비켜난 곳에 낸다.
    // 너무 크면 반대편까지 달려갈 수 없어 피할 수 없는 탄막이 되므로, 탄 간격 동안 걸어갈 수 있을 만큼만 옮긴다.
    [SerializeField] private FloatRange _gapShift = new FloatRange(30f, 60f);

    // 한 번 발동할 때 동시에 날아갈 수 있는 탄 수만큼 만든다.
    public override void Prewarm(GameObject defaultProjectile)
    {
        GameObject prefab = GetProjectilePrefab(defaultProjectile);

        if (null != prefab)
        {
            GameManager.Pool.Prewarm(prefab, _waveCount * _shotsPerWave);
        }
    }

    public override async UniTask PlayAsync(AutoAttacker shooter, CancellationToken token)
    {
        GameManager.Sound.PlaySfx(_warningSound);

        await UniTask.Delay(TimeSpan.FromSeconds(_warningDuration), cancellationToken: token);

        float angleBetweenShots = FULL_CIRCLE / _shotsPerWave;
        float waveAngle = UnityEngine.Random.Range(0f, FULL_CIRCLE);
        float gapCenter = GetAngleToTarget(shooter) + PickGapShift();

        for (int wave = 0; wave < _waveCount; wave++)
        {
            FireWave(shooter, waveAngle, angleBetweenShots, gapCenter, token);

            waveAngle += _angleStepPerWave;
            gapCenter += PickGapShift();

            await UniTask.Delay(TimeSpan.FromSeconds(_waveInterval), cancellationToken: token);
        }
    }

    // 쏘는 쪽이 움직이는 중이어도 발사마다 그 자리에서 퍼진다.
    private void FireWave(AutoAttacker shooter, float startAngle, float angleBetweenShots, float gapCenter, CancellationToken token)
    {
        Vector3 origin = shooter.Owner.Body.position;

        for (int i = 0; i < _shotsPerWave; i++)
        {
            float angle = startAngle + angleBetweenShots * i;

            if (IsInGap(angle, gapCenter))
            {
                continue;
            }

            Vector2 direction = Geometry2D.ToDirection(angle);

            // 환경 탄막은 보스의 공격이 아니라서 게이지를 쌓지 않는다.
            shooter.FireShotToward(_projectilePrefab, _shot, origin, direction, _range, 0f, token);
        }
    }

    // DeltaAngle은 두 각도의 차이를 -180~180도로 돌려줘, 360도를 넘어간 각도도 바르게 비교한다.
    private bool IsInGap(float angle, float gapCenter)
    {
        return 0f < _gapAngle && Mathf.Abs(Mathf.DeltaAngle(angle, gapCenter)) <= _gapAngle * 0.5f;
    }

    private float PickGapShift()
    {
        float shift = _gapShift.PickRandom();

        return 0.5f > UnityEngine.Random.value ? -shift : shift;
    }

    private GameObject GetProjectilePrefab(GameObject defaultProjectile)
    {
        return null != _projectilePrefab ? _projectilePrefab : defaultProjectile;
    }

    private float GetAngleToTarget(AutoAttacker shooter)
    {
        return Geometry2D.ToAngle(shooter.Target.FollowTransform.position - shooter.Owner.Body.position);
    }
}
