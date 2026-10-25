using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 붓을 털듯 목표 쪽 부채꼴로 먹물 방울을 뿌린다.
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

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        float centerAngle = GetAngleToTarget(context);

        for (int wave = 0; wave < _waveCount; wave++)
        {
            FireWave(context, centerAngle + _angleStepPerWave * wave, gaugeGain, token);

            if (wave < _waveCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_waveInterval), cancellationToken: token);
            }
        }
    }

    private void FireWave(AttackContext context, float centerAngle, float gaugeGain, CancellationToken token)
    {
        Vector3 origin = null != context.Hands ? context.Hands.RightHand.position : context.Owner.Body.position;
        float step = 1 < _shotCount ? _spreadAngle / (_shotCount - 1) : 0f;
        float startAngle = centerAngle - _spreadAngle * 0.5f;

        if (1 == _shotCount)
        {
            startAngle = centerAngle;
        }

        for (int i = 0; i < _shotCount; i++)
        {
            Vector2 direction = Geometry2D.ToDirection(startAngle + step * i);

            context.Launcher.FireShotToward(_shot, origin, direction, _range, gaugeGain, token);
        }
    }

    private float GetAngleToTarget(AttackContext context)
    {
        return Geometry2D.ToAngle(context.Target.FollowTransform.position - context.Owner.Body.position);
    }
}
