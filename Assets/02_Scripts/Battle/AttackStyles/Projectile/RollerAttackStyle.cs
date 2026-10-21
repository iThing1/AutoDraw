using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 쏜 순간 목표가 있던 쪽으로 원통이 느리게 굴러간다. 유도가 없어 목표가 비켜서면 빗나가고, 지나간 길에 물감 띠를 남긴다.
// 몸을 빠져나갔다가 다시 닿으면 또 맞히고, 횟수를 다 채우면 그 자리에서 터진다.
[CreateAssetMenu(fileName = "RollerAttackStyle", menuName = "Scriptable Objects/AttackStyle/Roller")]
public class RollerAttackStyle : AttackStyle
{
    [Header("원통")]
    [SerializeField] private GameObject _rollerPrefab;
    // 굴러가는 방향에 수직인 길이.
    [SerializeField, Min(0.1f)] private float _width = 2.4f;
    [SerializeField, Min(0.1f)] private float _thickness = 0.7f;
    [SerializeField, Min(0.1f)] private float _speed = 4f;
    [SerializeField, Min(0.1f)] private float _length = 14f;
    [SerializeField, Range(0f, 1f)] private float _rollWobble = 0.15f;
    [SerializeField, Min(0.1f)] private float _rollWobbleSpeed = 12f;

    [Header("물감 띠")]
    [SerializeField] private SplatStyle _stripeStyle;
    // 자국 수가 상한에 닿으면 잉크 자국부터 지워지므로 띄엄띄엄 찍는다.
    [SerializeField, Min(0.05f)] private float _stripeSpacing = 0.8f;
    [SerializeField] private FloatRange _stripeScale = new(0.9f, 1.2f);

    [Header("타격")]
    // 몸에 닿아 있는 동안은 한 번으로 센다.
    [SerializeField, Min(1)] private int _maxHits = 2;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _burstImpact = new();

    // 원통이 다 구르기 전에 다음 원통이 나온다.
    public override void Prewarm()
    {
        if (null != _rollerPrefab)
        {
            GameManager.Pool.Prewarm(_rollerPrefab, Mathf.CeilToInt(_length / _speed / AttackInterval));
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _rollerPrefab)
        {
            Debug.LogError($"롤러 : {name}에 원통 프리팹이 지정되지 않았습니다.");
            return;
        }

        Vector3 start = context.Owner.Body.position;
        Vector3 aim = context.Target.GetAimPoint(Vector3.zero) - start;
        Vector3 direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector3.right;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;

        Color color = context.Damage.PickColor();
        SpriteRenderer roller = PooledSprite.Spawn(_rollerPrefab, start, angle, new Vector2(_width, _thickness), color);
        PaintPalette palette = context.Owner.Data.Palette;
        bool isInk = null != palette && palette.IsInk;

        float travelled = 0f;
        float nextStripe = 0f;
        int hitCount = 0;
        bool wasTouching = false;

        try
        {
            while (travelled < _length)
            {
                Vector3 previous = roller.transform.position;
                float step = _speed * Time.deltaTime;

                roller.transform.position = previous + direction * step;
                travelled += step;

                float wobble = 1f + Mathf.Sin(travelled * _rollWobbleSpeed) * _rollWobble;
                roller.transform.localScale = new Vector3(_width, _thickness * wobble, 1f);

                bool isTouching = context.Target.IsTouchingSegment(previous, roller.transform.position, _width * 0.5f);

                if (isTouching && false == wasTouching)
                {
                    hitCount++;
                    context.Damage.Strike(_hit, context.Target.GetAimPoint(Vector3.zero), gaugeGain);

                    if (hitCount >= _maxHits)
                    {
                        ImpactPlayer.Play(_burstImpact, roller.transform.position, color);
                        return;
                    }
                }

                wasTouching = isTouching;

                for (; nextStripe <= travelled; nextStripe += _stripeSpacing)
                {
                    context.PaintCanvas.LeaveSplat(start + direction * nextStripe, _stripeStyle, color, isInk, _stripeScale.PickRandom());
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            PooledSprite.Despawn(roller);
        }
    }
}
