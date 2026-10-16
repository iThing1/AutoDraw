using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 먹물 소용돌이. 보스가 그 자리에서 두 손을 돌리며 나선으로 먹물 방울을 뿌리고, 그동안 플레이어를 보스 쪽으로 끌어당긴다.
// 멀리 도망가 있으면 끌려 들어오며 나선 사이 빈틈을 찾아야 하고, 가까이 있으면 방울이 촘촘해 더 위험하다.
[CreateAssetMenu(fileName = "InkVortexAttackStyle", menuName = "Scriptable Objects/AttackStyle/InkVortex")]
public class InkVortexAttackStyle : AttackStyle
{
    [Header("나선")]
    [SerializeField, Min(0f)] private float _duration = 3.5f;
    [SerializeField, Min(1)] private int _armCount = 4;
    [SerializeField, Min(0.02f)] private float _waveInterval = 0.12f;
    // 한 번 뿌릴 때마다 나선이 도는 각도(도).
    [SerializeField] private float _angleStep = 9f;
    [SerializeField, Min(1f)] private float _range = 25f;
    [SerializeField] private ShotSettings _shot = new();

    [Header("끌어당김")]
    // 플레이어를 보스 쪽으로 끄는 빠르기(초당 거리). 플레이어 이동 속도보다 낮게 둬야 버틸 수 있다.
    [SerializeField, Min(0f)] private float _pullSpeed = 2.5f;

    [Header("손")]
    [SerializeField, Min(0f)] private float _handOrbitSpeed = 540f;

    public override async UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token)
    {
        UnitMover targetMover = attacker.Target.Unit.Mover;
        BossHands hands = attacker.Hands;
        Vector3 center = attacker.Owner.Body.position;
        float angle = UnityEngine.Random.Range(0f, 360f);
        float handAngle = 0f;
        float elapsed = 0f;
        float nextWave = 0f;

        try
        {
            // 게임 시간으로 돌아 히트 스톱 동안에는 함께 멈춘다.
            while (elapsed < _duration)
            {
                if (elapsed >= nextWave)
                {
                    FireWave(attacker, center, angle, gaugeGain, token);
                    angle += _angleStep;
                    nextWave += _waveInterval;
                }

                if (null != targetMover)
                {
                    targetMover.SetExternalVelocity(GetPullVelocity(center, attacker.Target.FollowTransform.position));
                }

                if (null != hands)
                {
                    handAngle += _handOrbitSpeed * Time.deltaTime;
                    OrbitHands(hands, center, handAngle);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }
        }
        finally
        {
            if (null != targetMover)
            {
                targetMover.SetExternalVelocity(Vector2.zero);
            }
        }
    }

    // 플레이어는 물리 단계마다 속도 × fixedDeltaTime만큼 움직인다. 남은 거리보다 더 끌면 중심을 지나쳤다 되돌아오며 떨려서 그 이하로 자른다.
    private Vector2 GetPullVelocity(Vector3 center, Vector3 targetPosition)
    {
        Vector2 toBoss = center - targetPosition;
        float distance = toBoss.magnitude;

        if (0f >= distance)
        {
            return Vector2.zero;
        }

        return toBoss / distance * Mathf.Min(_pullSpeed, distance / Time.fixedDeltaTime);
    }

    private void FireWave(AutoAttacker attacker, Vector3 center, float angle, float gaugeGain, CancellationToken token)
    {
        float angleBetweenArms = 360f / _armCount;

        for (int i = 0; i < _armCount; i++)
        {
            Vector2 direction = Geometry2D.ToDirection(angle + angleBetweenArms * i);

            attacker.FireShotToward(null, _shot, center, direction, _range, gaugeGain, token);
        }
    }

    // 두 손이 몸통 둘레를 마주 보며 돈다. 손은 예고(Orbit) 자세를 잡은 채라 떠 있기 흔들림과 겹치지 않는다.
    private void OrbitHands(BossHands hands, Vector3 center, float angle)
    {
        float radius = Vector2.Distance(hands.LeftHand.position, center);
        Vector3 offset = Geometry2D.ToDirection(angle) * radius;

        hands.LeftHand.position = center + offset;
        hands.RightHand.position = center - offset;
    }
}
