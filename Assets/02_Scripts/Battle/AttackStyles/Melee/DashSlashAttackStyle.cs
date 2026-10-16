using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 목표 옆으로 빠르게 붙어 시간을 늦춘 채 칼을 휘두르고, 원래 자리로 돌아간다.
[CreateAssetMenu(fileName = "DashSlashAttackStyle", menuName = "Scriptable Objects/AttackStyle/DashSlash")]
public class DashSlashAttackStyle : AttackStyle
{
    [Header("돌진")]
    [SerializeField, Min(0f)] private float _dashDuration = 0.25f;
    [SerializeField] private Ease _dashEase = Ease.OutQuad;
    [SerializeField, Min(0f)] private float _stopDistance = 1.8f;
    [SerializeField, Min(0f)] private float _delayBeforeSlash = 0.1f;

    [Header("슬로 모션")]
    [SerializeField, Range(0.01f, 1f)] private float _slowTimeScale = 0.35f;

    [Header("베기")]
    [SerializeField] private GameObject _bladePrefab;
    [SerializeField, Range(0f, 360f)] private float _swingAngle = 220f;
    [SerializeField, Min(0f)] private float _swingRadius = 2.2f;
    [SerializeField, Min(0.01f)] private float _swingDuration = 0.3f;
    [SerializeField] private Ease _swingEase = Ease.OutCubic;
    // 휘두르는 시간 중 이 비율에 맞힌다. 0.5면 칼이 반쯤 지나갔을 때다.
    [SerializeField, Range(0f, 1f)] private float _hitTiming = 0.5f;
    [SerializeField, Min(0f)] private float _trailLingerTime = 0.3f;

    // 투사체 관련 값(크기, 궤적, 꼬리)은 쓰지 않고 피해·치명타·타격 프리셋만 쓴다.
    [SerializeField] private ShotSettings _hit = new();

    [Header("복귀")]
    [SerializeField, Min(0f)] private float _returnDuration = 0.4f;
    [SerializeField] private Ease _returnEase = Ease.InOutSine;

    public override void Prewarm()
    {
        if (null != _bladePrefab)
        {
            GameManager.Pool.Prewarm(_bladePrefab, 1);
        }
    }

    public override async UniTask FireAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token)
    {
        Transform owner = attacker.Owner.transform;
        Vector3 origin = owner.position;

        await StartDash(owner, origin, attacker.Target)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);

        await UniTask.Delay(TimeSpan.FromSeconds(_delayBeforeSlash), cancellationToken: token);

        using (GameManager.TimeScale.BeginSlowMotion(_slowTimeScale))
        {
            await SwingAsync(attacker, gaugeGain, token);
        }

        await owner.DOMove(origin, _returnDuration)
            .SetEase(_returnEase)
            .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
    }

    // 세상은 느려진 채로 칼만 실제 시간으로 휘둘러, 칼끝의 검은 궤적이 또렷하게 남게 한다.
    private async UniTask SwingAsync(AutoAttacker attacker, float gaugeGain, CancellationToken token)
    {
        if (null == _bladePrefab)
        {
            Debug.LogError($"돌진 베기 : {name}에 칼끝 프리팹이 지정되지 않았습니다.");
            return;
        }

        Vector3 center = attacker.Owner.Body.position;
        Vector3 toTarget = attacker.Target.GetAimPoint(Vector3.zero) - center;
        float facingAngle = Geometry2D.ToAngle(toTarget);
        float startAngle = facingAngle + _swingAngle * 0.5f;
        float endAngle = facingAngle - _swingAngle * 0.5f;

        GameObject blade = GameManager.Pool.Spawn(_bladePrefab, GetPointOnArc(center, startAngle), Quaternion.identity);

        if (blade.TryGetComponent(out TrailRenderer trail))
        {
            // 풀에서 다시 꺼낸 칼끝에 이전 궤적이 남아 있으면 이전 자리에서부터 선이 그어진다.
            trail.Clear();
        }

        try
        {
            Tween swing = StartSwing(blade.transform, center, startAngle, endAngle);

            await RealtimeDelay.WaitAsync(_swingDuration * _hitTiming, token);

            // 칼이 닿는 거리 밖으로 벗어났으면 빗나간다.
            Vector3 hitPoint = Vector3.MoveTowards(attacker.Owner.Body.position, attacker.Target.GetAimPoint(Vector3.zero), _swingRadius);

            attacker.Strike(_hit, hitPoint, gaugeGain);

            await swing.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
            await RealtimeDelay.WaitAsync(_trailLingerTime, token);
        }
        finally
        {
            GameManager.Pool.Despawn(blade);
        }
    }

    // 목표가 움직여도 붙도록 도착 지점을 매 프레임 다시 구한다. 반동 이징이 1을 넘길 수 있어 LerpUnclamped를 쓴다.
    private Tween StartDash(Transform owner, Vector3 origin, AttackTarget target)
    {
        return DOVirtual.Float(0f, 1f, _dashDuration, progress => owner.position = Vector3.LerpUnclamped(origin, target.GetApproachPoint(origin, _stopDistance), progress))
            .SetEase(_dashEase);
    }

    private Tween StartSwing(Transform blade, Vector3 center, float startAngle, float endAngle)
    {
        return DOVirtual.Float(startAngle, endAngle, _swingDuration, angle => blade.position = GetPointOnArc(center, angle))
            .SetEase(_swingEase)
            .SetUpdate(true);
    }

    private Vector3 GetPointOnArc(Vector3 center, float angle)
    {
        return center + Geometry2D.ToDirection(angle) * _swingRadius;
    }
}
