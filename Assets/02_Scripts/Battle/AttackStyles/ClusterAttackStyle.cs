using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(fileName = "ClusterAttackStyle", menuName = "Scriptable Objects/AttackStyle/Cluster")]
public class ClusterAttackStyle : AttackStyle
{
    [Header("발사체")]
    [SerializeField] private ShotSettings _shot = new();

    [Header("파편")]
    [SerializeField] private ClusterSettings _cluster = new();

    // 덩어리가 공중의 분열 지점까지 날아가 터지고, 조각들이 시간차를 두고 목표 주변으로 쏟아진다.
    public override async UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token)
    {
        Vector3 start = attacker.Owner.Body.position;
        Vector3 end = attacker.Target.GetAimPoint(Vector3.zero);
        Vector3 splitPoint = Vector3.Lerp(start, end, _cluster.SplitRatio) + Vector3.up * _cluster.SplitHeight;

        Color color = attacker.PickColor();
        bool hasArrived = await attacker.FlyToPointAsync(_shot, start, splitPoint, color, token);

        if (false == hasArrived || attacker.Target.IsDefeated)
        {
            return;
        }

        ImpactPlayer.Play(_cluster.SplitImpact, splitPoint, color);

        for (int i = 0; i < _cluster.FragmentCount; i++)
        {
            Vector3 landingOffset = UnityEngine.Random.insideUnitCircle * _cluster.LandingSpread;

            attacker.FireShot(_cluster.Fragment, splitPoint, landingOffset, gaugeGain, token);

            await UniTask.Delay(TimeSpan.FromSeconds(_cluster.FragmentInterval), cancellationToken: token);
        }
    }
}
