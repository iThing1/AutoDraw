using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 쏜 순간 목표가 있던 쪽으로 원통이 굴러가며 지나간 길에 물감 띠를 깐다. 횟수를 다 채우면 그 자리에서 터진다.
[CreateAssetMenu(fileName = "RollerAttackStyle", menuName = "Scriptable Objects/AttackStyle/Roller")]
public class RollerAttackStyle : AttackStyle
{
    [Header("원통")]
    [SerializeField] private GameObject _rollerPrefab;
    [SerializeField, Min(0.1f)] private float _width = 2.4f;
    [SerializeField, Min(0.1f)] private float _thickness = 0.7f;
    [SerializeField, Min(0.1f)] private float _speed = 4f;
    [SerializeField, Min(0.1f)] private float _length = 14f;
    [SerializeField, Range(0f, 1f)] private float _rollWobble = 0.15f;
    [SerializeField, Min(0.1f)] private float _rollWobbleSpeed = 12f;

    [Header("물감 띠")]
    // 가로로 이어 붙여도 이음매가 없는 띠 그림. 깔린 길이만큼 반복한다.
    [SerializeField] private Texture2D _carpetTexture;
    [SerializeField, Min(0.1f)] private float _carpetTileLength = 8.8f;
    [SerializeField, Min(0.1f)] private float _carpetWidth = 2.2f;

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
        PaintStripe carpet = context.PaintCanvas.BeginStripe(start, direction, _carpetWidth, _carpetTexture, _carpetTileLength, color, context.Damage.IsInk);
        float travelled = 0f;
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

                if (null != carpet)
                {
                    carpet.SetLength(travelled);
                }

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

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            // 터지거나 끊겨도 깔린 데까지는 남긴다.
            if (null != carpet)
            {
                carpet.Finish();
            }

            PooledSprite.Despawn(roller);
        }
    }
}
