using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 몸 양옆에 작은 포대를 꺼내 일제히 여러 번 쏜 뒤, 몸통에서 큰 한 발로 마무리한다.
[CreateAssetMenu(fileName = "BarrageAttackStyle", menuName = "Scriptable Objects/AttackStyle/Barrage")]
public class BarrageAttackStyle : AttackStyle
{
    [Header("포대")]
    [SerializeField] private GameObject _turretPrefab;
    [SerializeField] private Vector2[] _turretOffsets = { new Vector2(-2.5f, 1f), new Vector2(2.5f, 1f) };
    [SerializeField, Min(0f)] private float _turretAppearDuration = 0.25f;
    [SerializeField] private Ease _turretAppearEase = Ease.OutBack;
    [SerializeField, Min(0f)] private float _turretDisappearDuration = 0.2f;
    [SerializeField] private Ease _turretDisappearEase = Ease.InBack;
    [SerializeField, Min(0f)] private float _turretRecoil = 0.25f;
    [SerializeField] private AudioClip _turretAppearSound;

    [Header("일제사격")]
    [SerializeField, Min(1)] private int _volleyCount = 4;
    [SerializeField, Min(0f)] private float _volleyInterval = 0.15f;
    [SerializeField] private ShotSettings _turretShot = new();

    [Header("마지막 한 발")]
    [SerializeField, Min(0f)] private float _finaleDelay = 0.35f;
    [SerializeField] private ShotSettings _finaleShot = new();

    // 일제사격은 끝나야 다음 공격을 하므로 포대는 위치 수만큼만 있으면 된다.
    public override void Prewarm()
    {
        if (null != _turretPrefab)
        {
            GameManager.Pool.Prewarm(_turretPrefab, _turretOffsets.Length);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        if (null == _turretPrefab)
        {
            Debug.LogError($"일제사격 : {name}에 포대 프리팹이 지정되지 않았습니다.");
            return;
        }

        GameManager.Sound.PlaySfx(_turretAppearSound);

        List<Transform> turrets = SpawnTurrets(context.Owner.transform);
        AimTurrets(turrets, context.Target);

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_turretAppearDuration), cancellationToken: token);

            for (int volley = 0; volley < _volleyCount; volley++)
            {
                AimTurrets(turrets, context.Target);

                for (int i = 0; i < turrets.Count; i++)
                {
                    context.Launcher.FireShot(_turretShot, turrets[i].position, Vector3.zero, gaugeGain, token);
                    Recoil(turrets[i]);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_volleyInterval), cancellationToken: token);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_finaleDelay), cancellationToken: token);

            context.Launcher.FireShot(_finaleShot, context.Owner.Body.position, Vector3.zero, gaugeGain, token);
        }
        finally
        {
            DespawnTurrets(turrets);
        }
    }

    // 포대는 보스를 따라다니도록 보스 아래에 붙이고, 크기 0에서 튀어나오게 한다.
    private List<Transform> SpawnTurrets(Transform owner)
    {
        List<Transform> turrets = new List<Transform>(_turretOffsets.Length);
        Vector3 turretScale = _turretPrefab.transform.localScale;

        for (int i = 0; i < _turretOffsets.Length; i++)
        {
            Transform turret = GameManager.Pool.Spawn(_turretPrefab, owner).transform;
            turret.localPosition = _turretOffsets[i];
            turret.localScale = Vector3.zero;
            turret.DOScale(turretScale, _turretAppearDuration).SetEase(_turretAppearEase);

            turrets.Add(turret);
        }

        return turrets;
    }

    // 포신 그림이 오른쪽(+x)을 향하므로, 쏘기 전에 목표 쪽으로 돌린다.
    private static void AimTurrets(List<Transform> turrets, AttackTarget target)
    {
        if (null == target)
        {
            return;
        }

        Vector3 targetPosition = target.FollowTransform.position;

        for (int i = 0; i < turrets.Count; i++)
        {
            turrets[i].rotation = Quaternion.Euler(0f, 0f, Geometry2D.ToAngle(targetPosition - turrets[i].position));
        }
    }

    private void Recoil(Transform turret)
    {
        turret.DOComplete();
        turret.DOPunchScale(Vector3.one * _turretRecoil, _volleyInterval, PunchTween.VIBRATO, PunchTween.ELASTICITY);
    }

    private void DespawnTurrets(List<Transform> turrets)
    {
        for (int i = 0; i < turrets.Count; i++)
        {
            HideAndDespawnAsync(turrets[i]).Forget();
        }
    }

    // 끝까지 줄어든 뒤 풀로 돌려준다. 보스와 함께 파괴돼 트윈이 끊겨도 여기로 오므로, 남아 있을 때만 돌려준다.
    private async UniTaskVoid HideAndDespawnAsync(Transform turret)
    {
        turret.DOComplete();

        await turret.DOScale(0f, _turretDisappearDuration)
            .SetEase(_turretDisappearEase)
            .SetLink(turret.gameObject)
            .ToUniTask();

        if (null == turret)
        {
            return;
        }

        GameManager.Pool.Despawn(turret.gameObject);
    }
}
