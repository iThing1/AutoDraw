using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;

// 발밑 잉크로 가라앉았다가, 목표가 있던 자리에 잉크가 번지는 예고를 띄우고 그 자리에서 솟아오르며 친다.
// 가라앉아 있는 동안은 맞지 않는다. 예고를 보고 비켜서거나 막으면 된다.
[CreateAssetMenu(fileName = "InkDiveAttackStyle", menuName = "Scriptable Objects/AttackStyle/InkDive")]
public class InkDiveAttackStyle : AttackStyle
{
    [Header("가라앉음")]
    [SerializeField, Min(0f)] private float _sinkDuration = 0.3f;
    [SerializeField] private Ease _sinkEase = Ease.InBack;
    [SerializeField, Min(0f)] private float _hiddenDuration = 0.35f;
    // 가라앉은 자리와 솟아오를 자리에 남는 잉크 자국. 비워 두면 남기지 않는다.
    [SerializeField] private HitFeedbackPreset _inkSplat;

    [Header("예고")]
    [SerializeField] private GameObject _telegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.4f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.7f;

    [Header("솟아오름")]
    [SerializeField, Min(0f)] private float _riseDuration = 0.25f;
    [SerializeField] private Ease _riseEase = Ease.OutBack;
    [SerializeField, Min(0f)] private float _blastRadius = 2.4f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _riseImpact = new();

    [Header("여러 번")]
    // 2 이상이면 솟아오른 자리에서 곧바로 다시 가라앉아 또 노린다.
    [SerializeField, Min(1)] private int _diveCount = 1;

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        Transform owner = context.Owner.transform;
        Vector3 startScale = owner.localScale;

        try
        {
            for (int i = 0; i < _diveCount; i++)
            {
                await DiveOnceAsync(context, owner, startScale, gaugeGain, token);
            }
        }
        finally
        {
            // 기믹이 끊어도 보스가 작아진 채로 남지 않게 한다.
            owner.DOKill();
            owner.localScale = startScale;
        }
    }

    private async UniTask DiveOnceAsync(AttackContext context, Transform owner, Vector3 startScale, float gaugeGain, CancellationToken token)
    {
        using (context.Owner.Health.BeginInvulnerable())
        {
            LeaveInk(context, owner.position);

            await owner.DOScale(Vector3.zero, _sinkDuration)
                .SetEase(_sinkEase)
                .ToUniTaskKillOnCancel(token);

            await UniTask.Delay(TimeSpan.FromSeconds(_hiddenDuration), cancellationToken: token);

            Vector3 risePoint = context.Target.GetAimPoint(Vector3.zero);
            risePoint.z = owner.position.z;

            LeaveInk(context, risePoint);
            await Telegraph.PlayCircleAsync(_telegraphPrefab, risePoint, _blastRadius, _telegraphColor, _warnDuration, token);

            owner.position = risePoint;

            await owner.DOScale(startScale, _riseDuration)
                .SetEase(_riseEase)
                .ToUniTaskKillOnCancel(token);
        }

        ImpactPlayer.Play(_riseImpact, owner.position);
        context.Damage.Blast(_hit, owner.position, _blastRadius, gaugeGain);
    }

    private void LeaveInk(AttackContext context, Vector3 position)
    {
        if (null != _inkSplat && null != context.PaintCanvas)
        {
            context.PaintCanvas.LeaveSplats(position, context.Damage.PickColor(), true, _inkSplat);
        }
    }
}
