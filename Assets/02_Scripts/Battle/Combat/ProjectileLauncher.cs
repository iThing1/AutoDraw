using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 투사체를 쏘고, 날아간 결과(막힘·명중)를 피해 처리로 넘긴다. 패턴은 어디서 어디로 쏠지만 정한다.
public class ProjectileLauncher
{
    private readonly BattleUnit _owner;
    private readonly AttackTarget _target;
    private readonly DamageDealer _damage;
    private bool _hasLoggedMissingProjectile;

    public ProjectileLauncher(BattleUnit owner, AttackTarget target, DamageDealer damage)
    {
        _owner = owner;
        _target = target;
        _damage = damage;
    }

    public void FireShot(ShotSettings shot, Vector3 start, Vector3 landingOffset, float gaugeGain, CancellationToken token)
    {
        if (false == HasProjectilePrefab())
        {
            return;
        }

        FireShotAsync(_owner.Data.ProjectilePrefab, shot, start, _target.GetAimPoint(landingOffset), gaugeGain, token).Forget();
    }

    // 목표를 겨누지 않고 정해진 방향으로 사거리만큼 날린다. 날아가는 길에 목표가 닿으면 맞는다.
    // 추적 강도가 0인 탄과 함께 써서, 플레이어가 피할 자리를 찾아 움직이게 하는 탄막에 쓴다.
    public void FireShotToward(ShotSettings shot, Vector3 start, Vector2 direction, float range, float gaugeGain, CancellationToken token)
    {
        FireShotToward(_owner.Data.ProjectilePrefab, shot, start, direction, range, gaugeGain, token);
    }

    // 유닛과 다른 투사체로 쏠 때 쓴다(환경 탄막).
    public void FireShotToward(GameObject projectilePrefab, ShotSettings shot, Vector3 start, Vector2 direction, float range, float gaugeGain,
        CancellationToken token)
    {
        if (false == IsProjectileAssigned(projectilePrefab))
        {
            return;
        }

        Vector3 end = start + (Vector3)(direction.normalized * range);

        FireShotAsync(projectilePrefab, shot, start, end, gaugeGain, token).Forget();
    }

    // 목표가 아닌 고정된 지점으로 날린다. 파편이 갈라지는 공중 지점처럼 맞히지 않는 비행에 쓴다.
    public async UniTask<bool> FlyToPointAsync(ShotSettings shot, Vector3 start, Vector3 end, Color color, CancellationToken token)
    {
        if (false == HasProjectilePrefab())
        {
            return false;
        }

        FlightResult flight = await ProjectileFlight.FlyAsync(_owner.Data.ProjectilePrefab, shot, start, end, color, token);

        return FlightOutcome.Arrived == flight.Outcome;
    }

    private async UniTaskVoid FireShotAsync(GameObject projectilePrefab, ShotSettings shot, Vector3 start, Vector3 end, float gaugeGain,
        CancellationToken token)
    {
        DamageInfo damage = _damage.RollDamage(shot.Hit);

        FlightResult flight = await ProjectileFlight.FlyAsync(projectilePrefab, shot, start, end, _target, damage.PaintColor, token);

        if (FlightOutcome.Blocked == flight.Outcome)
        {
            _target.NotifyBlocked(flight.LandingPoint);
        }
        else if (FlightOutcome.Arrived == flight.Outcome)
        {
            _damage.DealHit(shot.Hit, damage, flight.LandingPoint, gaugeGain);
        }
    }

    private bool HasProjectilePrefab()
    {
        return IsProjectileAssigned(_owner.Data.ProjectilePrefab);
    }

    private bool IsProjectileAssigned(GameObject projectilePrefab)
    {
        if (null != projectilePrefab)
        {
            return true;
        }

        // 쏠 때마다 남기면 같은 로그가 쌓여 다른 에러가 묻히므로 한 번만 알린다.
        if (false == _hasLoggedMissingProjectile)
        {
            _hasLoggedMissingProjectile = true;
            Debug.LogError($"투사체 발사 : {_owner.name}이 쏠 투사체 프리팹이 지정되지 않았습니다.");
        }

        return false;
    }
}
