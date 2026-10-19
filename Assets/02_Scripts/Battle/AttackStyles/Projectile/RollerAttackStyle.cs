using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 쏜 순간 목표가 있던 쪽으로 원통이 느리게 굴러간다. 유도가 없어 목표가 비켜서면 빗나가고, 지나간 길에 물감 띠를 남긴다.
// 맞혀도 멈추지 않고 끝까지 굴러가 띠를 마저 깐다.
[CreateAssetMenu(fileName = "RollerAttackStyle", menuName = "Scriptable Objects/AttackStyle/Roller")]
public class RollerAttackStyle : AttackStyle
{
    [Header("원통")]
    [SerializeField] private GameObject _rollerPrefab;
    // 굴러가는 방향에 수직인 길이. 이만큼의 폭으로 맞히고 띠를 깐다.
    [SerializeField, Min(0.1f)] private float _width = 2.4f;
    [SerializeField, Min(0.1f)] private float _thickness = 0.7f;
    [SerializeField, Min(0.1f)] private float _speed = 4f;
    [SerializeField, Min(0.1f)] private float _length = 14f;
    // 굴러가는 느낌을 내려고 두께를 이만큼 늘였다 줄인다.
    [SerializeField, Range(0f, 1f)] private float _rollWobble = 0.15f;
    [SerializeField, Min(0.1f)] private float _rollWobbleSpeed = 12f;

    [Header("물감 띠")]
    [SerializeField] private SplatStyle _stripeStyle;
    // 자국 수가 상한에 닿으면 잉크 자국부터 지워지므로, 촘촘히 찍기보다 크게 띄엄띄엄 찍는다.
    [SerializeField, Min(0.05f)] private float _stripeSpacing = 0.8f;
    [SerializeField] private FloatRange _stripeScale = new(0.9f, 1.2f);

    [Header("타격")]
    [SerializeField] private HitSettings _hit = new();

    public override void Prewarm()
    {
        if (null != _rollerPrefab)
        {
            GameManager.Pool.Prewarm(_rollerPrefab, 2);
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
        bool hasHit = false;

        try
        {
            // 게임 시간으로 굴러 히트 스톱 동안에는 함께 멈춘다. 느려도 판정이 건너뛰지 않게 이전 자리와 잇는 선분으로 본다.
            while (travelled < _length)
            {
                Vector3 previous = roller.transform.position;
                float step = _speed * Time.deltaTime;

                roller.transform.position = previous + direction * step;
                travelled += step;

                float wobble = 1f + Mathf.Sin(travelled * _rollWobbleSpeed) * _rollWobble;
                roller.transform.localScale = new Vector3(_width, _thickness * wobble, 1f);

                if (false == hasHit && context.Target.IsTouchingSegment(previous, roller.transform.position, _width * 0.5f))
                {
                    hasHit = true;
                    context.Damage.Strike(_hit, context.Target.GetAimPoint(Vector3.zero), gaugeGain);
                }

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
