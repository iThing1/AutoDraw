using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 붓을 털듯 목표 쪽 부채꼴로 먹물 방울을 뿌린다. 추적하지 않아 부채꼴 밖으로 비켜서면 피한다.
// 여러 번 뿌릴 때 회전을 주면 부채꼴이 돌아가며 빈자리가 옮겨 간다.
[CreateAssetMenu(fileName = "InkScatterAttackStyle", menuName = "Scriptable Objects/AttackStyle/InkScatter")]
public class InkScatterAttackStyle : AttackStyle
{
    [Header("흩뿌리기")]
    [SerializeField, Min(1)] private int _shotCount = 9;
    [SerializeField, Range(0f, 360f)] private float _spreadAngle = 70f;
    [SerializeField, Min(1f)] private float _range = 14f;

    [Header("여러 번")]
    [SerializeField, Min(1)] private int _waveCount = 1;
    [SerializeField, Min(0f)] private float _waveInterval = 0.25f;
    // 뿌릴 때마다 부채꼴을 이만큼(도) 돌린다.
    [SerializeField] private float _angleStepPerWave;

    [SerializeField] private ShotSettings _shot = new();

    public override async UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token)
    {
        float centerAngle = GetAngleToTarget(attacker);

        for (int wave = 0; wave < _waveCount; wave++)
        {
            FireWave(attacker, centerAngle + _angleStepPerWave * wave, gaugeGain, token);

            if (wave < _waveCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_waveInterval), cancellationToken: token);
            }
        }
    }

    private void FireWave(AutoAttacker attacker, float centerAngle, float gaugeGain, CancellationToken token)
    {
        Vector3 origin = null != attacker.Hands ? attacker.Hands.RightHand.position : attacker.Owner.Body.position;
        float step = 1 < _shotCount ? _spreadAngle / (_shotCount - 1) : 0f;
        float startAngle = centerAngle - _spreadAngle * 0.5f;

        if (1 == _shotCount)
        {
            startAngle = centerAngle;
        }

        for (int i = 0; i < _shotCount; i++)
        {
            Vector2 direction = Geometry2D.ToDirection(startAngle + step * i);

            attacker.FireShotToward(_shot, origin, direction, _range, gaugeGain, token);
        }
    }

    private float GetAngleToTarget(AutoAttacker attacker)
    {
        return Geometry2D.ToAngle(attacker.Target.FollowTransform.position - attacker.Owner.Body.position);
    }
}
