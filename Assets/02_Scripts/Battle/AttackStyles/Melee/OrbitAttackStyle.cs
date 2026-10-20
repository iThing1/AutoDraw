using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 몸 둘레를 도는 구슬을 간격마다 하나씩 목표로 날렸다가 되돌린다. 돌아온 구슬만 다시 날릴 수 있다.
// 고른 동안 계속 돌므로 에셋에서 WaitUntilFinished를 켜 둔다.
[CreateAssetMenu(fileName = "OrbitAttackStyle", menuName = "Scriptable Objects/AttackStyle/Orbit")]
public class OrbitAttackStyle : AttackStyle
{
    private enum OrbState
    {
        Orbiting,
        Flying,
        Returning,
    }

    [Header("구슬")]
    [SerializeField] private GameObject _orbPrefab;
    [SerializeField, Min(1)] private int _orbCount = 3;
    [SerializeField, Min(0.1f)] private float _orbDiameter = 0.7f;
    [SerializeField, Min(0.1f)] private float _orbitRadius = 1.6f;
    [SerializeField, Min(0.1f)] private float _orbitPeriod = 1.2f;
    // 몸에서 나오고 몸으로 들어가는 시간.
    [SerializeField, Min(0.01f)] private float _transitionDuration = 0.25f;

    [Header("날리기")]
    [SerializeField, Min(0.05f)] private float _launchInterval = 0.3f;
    [SerializeField, Min(0.1f)] private float _flySpeed = 10f;
    [SerializeField] private HitSettings _hit = new();

    public override void Prewarm()
    {
        if (null != _orbPrefab)
        {
            GameManager.Pool.Prewarm(_orbPrefab, _orbCount);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _orbPrefab)
        {
            Debug.LogError($"공전 구슬 : {name}에 구슬 프리팹이 지정되지 않았습니다.");
            return;
        }

        Orb[] orbs = new Orb[_orbCount];
        float angle = 0f;

        try
        {
            for (int i = 0; i < _orbCount; i++)
            {
                orbs[i] = new Orb(PooledSprite.Spawn(_orbPrefab, context.Owner.Body.position, 0f, Vector2.zero, context.Damage.PickColor()));
            }

            for (float t = 0f; t < _transitionDuration; t += Time.deltaTime)
            {
                angle = Spin(angle);
                PlaceOnOrbit(context, orbs, angle, t / _transitionDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            PlaceOnOrbit(context, orbs, angle, 1f);

            float nextLaunchTime = Time.time;
            int launchIndex = 0;

            while (IsSelected(context))
            {
                angle = Spin(angle);

                int index = FindOrbiting(orbs, launchIndex);

                if (Time.time >= nextLaunchTime && 0 <= index)
                {
                    orbs[index].Launch(context.Damage.RollDamage(_hit));
                    launchIndex = index + 1;
                    nextLaunchTime = Time.time + _launchInterval * context.Owner.WeaponModifier.IntervalScale;
                }

                for (int i = 0; i < orbs.Length; i++)
                {
                    if (orbs[i].Move(GetSlot(context, angle, i, 1f), context.Target, _flySpeed, _orbDiameter * 0.5f))
                    {
                        context.Damage.DealHit(_hit, orbs[i].Damage, orbs[i].Position, gaugeGain);
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            for (int i = 0; i < orbs.Length; i++)
            {
                orbs[i].BeginGather();
            }

            for (float t = 0f; t < _transitionDuration; t += Time.deltaTime)
            {
                angle = Spin(angle);

                float progress = t / _transitionDuration;
                float spread = 1f - progress;

                for (int i = 0; i < orbs.Length; i++)
                {
                    orbs[i].Gather(GetSlot(context, angle, i, spread), progress, _orbDiameter * spread);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            for (int i = 0; i < orbs.Length; i++)
            {
                if (null != orbs[i])
                {
                    PooledSprite.Despawn(orbs[i].Sprite);
                }
            }
        }
    }

    // AttackStyleSet 안에 넣으면 묶음이 지금 공격이 되어 바로 들어가 버리므로 단독으로 쓴다.
    private bool IsSelected(AttackContext context)
    {
        return this == context.Owner.AttackStyle && false == context.Owner.Health.IsDead && false == context.Target.IsDefeated;
    }

    private float Spin(float angle)
    {
        return Mathf.Repeat(angle + 360f / _orbitPeriod * Time.deltaTime, 360f);
    }

    // spread가 0이면 몸 안, 1이면 다 나온 상태다.
    private void PlaceOnOrbit(AttackContext context, Orb[] orbs, float angle, float spread)
    {
        for (int i = 0; i < orbs.Length; i++)
        {
            orbs[i].Place(GetSlot(context, angle, i, spread), _orbDiameter * spread);
        }
    }

    private Vector3 GetSlot(AttackContext context, float angle, int index, float spread)
    {
        float orbAngle = (angle + 360f * index / _orbCount) * Mathf.Deg2Rad;

        return context.Owner.Body.position + new Vector3(Mathf.Cos(orbAngle), Mathf.Sin(orbAngle), 0f) * (_orbitRadius * spread);
    }

    // 모두 날아가 있으면 -1이다.
    private static int FindOrbiting(Orb[] orbs, int startIndex)
    {
        for (int n = 0; n < orbs.Length; n++)
        {
            int index = (startIndex + n) % orbs.Length;

            if (orbs[index].IsOrbiting)
            {
                return index;
            }
        }

        return -1;
    }

    // 돌아올 때는 움직이는 제자리를 쫓지 않고 시간에 맞춰 이어 붙여 꼭 도착하게 한다.
    private class Orb
    {
        private readonly SpriteRenderer _sprite;
        private OrbState _state = OrbState.Orbiting;
        private DamageInfo _damage;
        private Vector3 _returnFrom;
        private float _returnProgress;
        private float _returnRate;

        public Orb(SpriteRenderer sprite)
        {
            _sprite = sprite;
        }

        public SpriteRenderer Sprite { get { return _sprite; } }
        public Vector3 Position { get { return _sprite.transform.position; } }
        public bool IsOrbiting { get { return OrbState.Orbiting == _state; } }
        public DamageInfo Damage { get { return _damage; } }

        public void Place(Vector3 position, float diameter)
        {
            _sprite.transform.position = position;
            _sprite.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        public void Launch(DamageInfo damage)
        {
            _damage = damage;
            _sprite.color = damage.PaintColor;
            _state = OrbState.Flying;
        }

        // 날아가던 구슬이 목표에 닿은 프레임에만 true를 돌려준다.
        public bool Move(Vector3 slot, AttackTarget target, float flySpeed, float hitRadius)
        {
            Transform orbTransform = _sprite.transform;

            switch (_state)
            {
                case OrbState.Orbiting:
                    orbTransform.position = slot;
                    return false;

                case OrbState.Flying:
                    Vector3 previous = orbTransform.position;
                    orbTransform.position = Vector3.MoveTowards(previous, target.GetAimPoint(Vector3.zero), flySpeed * Time.deltaTime);

                    if (false == target.IsTouchingSegment(previous, orbTransform.position, hitRadius))
                    {
                        return false;
                    }

                    StartReturn(slot, flySpeed);
                    return true;

                default:
                    _returnProgress = Mathf.Min(1f, _returnProgress + _returnRate * Time.deltaTime);
                    orbTransform.position = Vector3.Lerp(_returnFrom, slot, _returnProgress);

                    if (1f <= _returnProgress)
                    {
                        _state = OrbState.Orbiting;
                    }

                    return false;
            }
        }

        public void BeginGather()
        {
            _returnFrom = _sprite.transform.position;
        }

        public void Gather(Vector3 slot, float progress, float diameter)
        {
            Place(Vector3.Lerp(_returnFrom, slot, progress), diameter);
        }

        private void StartReturn(Vector3 slot, float flySpeed)
        {
            _state = OrbState.Returning;
            _returnFrom = _sprite.transform.position;
            _returnProgress = 0f;
            _returnRate = flySpeed / Mathf.Max(Vector2.Distance(_returnFrom, slot), 0.01f);
        }
    }
}
