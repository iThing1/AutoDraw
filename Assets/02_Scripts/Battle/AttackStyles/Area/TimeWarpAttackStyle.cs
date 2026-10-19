using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// 목표 자리에 회백색 원을 펼쳐, 안에 든 플레이어와 탄을 모두 느리게 한다. 보스는 느려지지 않는다.
// 원 안으로 쏜 탄은 거의 멈춘 채 쌓였다가, 원이 깨지는 순간 한꺼번에 원래 속도로 날아온다.
// 원은 스스로 수명을 다하므로 패턴은 펼치기만 하고 바로 끝나, 그동안 보스는 다른 패턴을 이어 쓴다.
[CreateAssetMenu(fileName = "TimeWarpAttackStyle", menuName = "Scriptable Objects/AttackStyle/TimeWarp")]
public class TimeWarpAttackStyle : AttackStyle
{
    [SerializeField] private SlowZone _zonePrefab;
    [SerializeField] private SlowZoneSettings _zone = new();

    [Header("여러 개")]
    // 원을 여러 개 펼칠 때 첫 원은 지금 자리, 다음 원부터는 움직이는 방향으로 이 시간(초)씩 앞질러 펼쳐 도망갈 길을 막는다.
    [SerializeField, Min(1)] private int _zoneCount = 1;
    [SerializeField, Min(0f)] private float _zoneLeadTime = 0.8f;

    public override void Prewarm()
    {
        if (null != _zonePrefab)
        {
            GameManager.Pool.Prewarm(_zonePrefab.gameObject, _zoneCount);
        }
    }

    public override UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _zonePrefab)
        {
            Debug.LogError($"시간 왜곡 : {name}에 구역 프리팹이 지정되지 않았습니다.");
            return UniTask.CompletedTask;
        }

        for (int i = 0; i < _zoneCount; i++)
        {
            Vector3 center = context.Target.GetPredictedPoint(_zoneLeadTime * i);
            SlowZone zone = GameManager.Pool.Spawn(_zonePrefab.gameObject, center, Quaternion.identity).GetComponent<SlowZone>();

            zone.PlayAsync(_zone, token).Forget();
        }

        return UniTask.CompletedTask;
    }
}
