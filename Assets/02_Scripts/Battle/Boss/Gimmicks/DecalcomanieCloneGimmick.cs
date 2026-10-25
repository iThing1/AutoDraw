using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 데칼코마니 분신. 판 가운데에 세로로 접는 선이 그어지고, 반대편에 플레이어를 좌우 반전으로 따라 하는 먹 분신이 나타난다.
// 먹 방울은 플레이어와 분신 둘레에 따로 떨어지고, 분신이 맞아도 플레이어가 피해를 입는다. 두 몸이 함께 빈자리에 서는 자리를 찾아 버틴다.
// 끝까지 버티면 그로기로 이어진다.
[CreateAssetMenu(fileName = "DecalcomanieCloneGimmick", menuName = "Scriptable Objects/Boss/Gimmick/DecalcomanieClone")]
public class DecalcomanieCloneGimmick : BossGimmick
{
    private const float VERTICAL_ANGLE = 90f;

    [Header("접는 선")]
    // 접는 선과 먹 방울 예고 그림. 접는 선은 1x1 네모 그림, 먹 방울은 동그라미 그림을 쓴다.
    [SerializeField] private GameObject _foldPrefab;
    [SerializeField] private Color _foldColor = new Color(0.35f, 0.35f, 0.38f, 0.6f);
    [SerializeField, Min(0.01f)] private float _foldWidth = 0.25f;
    [SerializeField, Min(1f)] private float _foldLength = 60f;
    [SerializeField, Min(0f)] private float _foldDuration = 0.6f;

    [Header("분신")]
    // 몸 그림을 옮겨 받는 프리팹. 대시 잔상과 같은 것을 쓴다.
    [SerializeField] private GameObject _clonePrefab;
    [SerializeField] private Color _cloneColor = new Color(0.04f, 0.04f, 0.06f, 0.85f);
    [SerializeField, Min(0f)] private float _cloneFadeDuration = 0.5f;
    [SerializeField] private ImpactSettings _cloneImpact = new();

    [Header("먹 방울")]
    [SerializeField, Min(1)] private int _roundCount = 6;
    // 한 몸 둘레에 떨어지는 방울 수. 첫 방울은 몸 바로 위에 떨어져, 두 몸 모두 움직이게 한다.
    [SerializeField, Min(1)] private int _dropsPerBody = 3;
    [SerializeField, Min(0.1f)] private float _dropRadius = 1.6f;
    // 몸에서 이 거리 안에 나머지 방울을 흩뿌린다.
    [SerializeField, Min(0f)] private float _spreadRadius = 5f;
    [SerializeField] private GameObject _dropPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.35f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.9f;
    [SerializeField, Min(0f)] private float _roundInterval = 0.35f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();
    // 떨어진 자리에 남는 먹물 자국. 비워 두면 남기지 않는다.
    [SerializeField] private HitFeedbackPreset _groundSplat;

    public override void Prewarm()
    {
        if (null != _foldPrefab)
        {
            GameManager.Pool.Prewarm(_foldPrefab, 1);
        }

        if (null != _dropPrefab)
        {
            GameManager.Pool.Prewarm(_dropPrefab, _dropsPerBody * 4);
        }

        if (null != _clonePrefab)
        {
            GameManager.Pool.Prewarm(_clonePrefab, 1);
        }
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        if (null == _clonePrefab || null == context.Target.Unit.BodyRenderer)
        {
            Debug.LogError($"데칼코마니 분신 : {name}에 분신 프리팹이 없거나 목표에 몸 그림이 없습니다.");
            return true;
        }

        AttackTarget target = context.Target;
        float foldX = PickFoldX(context);
        Vector3 foldCenter = new Vector3(foldX, target.FollowTransform.position.y, target.FollowTransform.position.z);
        SpriteRenderer fold = null;
        SpriteRenderer clone = null;

        // 분신이 접는 선 위에서 생겨나는 것처럼 보이도록, 선을 먼저 긋고 분신을 띄운다.
        using (CancellationTokenSource followSource = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            try
            {
                if (null != _foldPrefab)
                {
                    fold = PooledSprite.Spawn(_foldPrefab, foldCenter, VERTICAL_ANGLE, new Vector2(_foldLength, _foldWidth), _foldColor);
                    fold.transform.localScale = new Vector3(0f, _foldWidth, 1f);

                    await fold.transform.DOScaleX(_foldLength, _foldDuration)
                        .SetEase(Ease.OutQuad)
                        .ToUniTaskKillOnCancel(token);
                }

                clone = SpawnClone(target, foldX);
                FollowAsync(target, clone, foldX, followSource.Token).Forget();
                ImpactPlayer.Play(_cloneImpact, clone.transform.position);

                await clone.DOFade(_cloneColor.a, _cloneFadeDuration)
                    .ToUniTaskKillOnCancel(token);

                List<Vector3> drops = new List<Vector3>(_dropsPerBody * 2);

                for (int round = 0; round < _roundCount && false == target.IsDefeated; round++)
                {
                    await PlayRoundAsync(context, clone.transform, drops, token);

                    if (round < _roundCount - 1)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(_roundInterval), cancellationToken: token);
                    }
                }

                UniTask cloneFade = clone.DOFade(0f, _cloneFadeDuration).ToUniTaskKillOnCancel(token);
                UniTask foldFade = null != fold ? fold.DOFade(0f, _cloneFadeDuration).ToUniTaskKillOnCancel(token) : UniTask.CompletedTask;

                await UniTask.WhenAll(cloneFade, foldFade);

                return true;
            }
            finally
            {
                followSource.Cancel();
                PooledSprite.Despawn(clone);
                PooledSprite.Despawn(fold);
            }
        }
    }

    // 플레이어 이동 범위의 가운데에 접어, 양쪽 모두 같은 넓이를 쓰게 한다. 범위가 없으면 플레이어와 보스 사이에 접는다.
    private static float PickFoldX(AttackContext context)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null != mover && mover.TryGetMoveArea(out Rect area))
        {
            return area.center.x;
        }

        return (context.Target.FollowTransform.position.x + context.Owner.transform.position.x) * 0.5f;
    }

    private SpriteRenderer SpawnClone(AttackTarget target, float foldX)
    {
        Color hidden = new Color(_cloneColor.r, _cloneColor.g, _cloneColor.b, 0f);
        SpriteRenderer clone = PooledSprite.Spawn(_clonePrefab, Vector3.zero, 0f, Vector2.one, hidden);

        MirrorBody(target.Unit.BodyRenderer, clone, foldX);

        return clone;
    }

    // 몸 그림을 접는 선 너머로 뒤집어 옮긴다. 애니메이션으로 기울고 늘어난 모습도 거울에 비친 것처럼 따라 한다.
    private static void MirrorBody(SpriteRenderer body, SpriteRenderer clone, float foldX)
    {
        Transform visual = body.transform;
        Vector3 position = visual.position;

        clone.transform.SetPositionAndRotation(new Vector3(2f * foldX - position.x, position.y, position.z), Quaternion.Euler(0f, 0f, -visual.eulerAngles.z));
        clone.transform.localScale = visual.lossyScale;
        clone.sprite = body.sprite;
        clone.flipX = false == body.flipX;
    }

    private static async UniTaskVoid FollowAsync(AttackTarget target, SpriteRenderer clone, float foldX, CancellationToken token)
    {
        SpriteRenderer body = target.Unit.BodyRenderer;

        while (false == token.IsCancellationRequested)
        {
            MirrorBody(body, clone, foldX);

            if (await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token).SuppressCancellationThrow())
            {
                return;
            }
        }
    }

    // 플레이어와 분신 둘레에 따로 방울을 정하고 함께 떨어뜨린다. 한 묶음은 같은 순간에 떨어져, 두 몸이 함께 맞아도 한 번만 맞는다.
    private async UniTask PlayRoundAsync(AttackContext context, Transform clone, List<Vector3> drops, CancellationToken token)
    {
        AttackTarget target = context.Target;

        drops.Clear();
        AddDrops(target.FollowTransform.position, drops);
        AddDrops(clone.position, drops);

        List<UniTask> telegraphs = new List<UniTask>(drops.Count);

        for (int i = 0; i < drops.Count; i++)
        {
            telegraphs.Add(Telegraph.PlayCircleAsync(_dropPrefab, drops[i], _dropRadius, _telegraphColor, _warnDuration, token));
        }

        await UniTask.WhenAll(telegraphs);

        ImpactPlayer.Play(_impact, (target.FollowTransform.position + clone.position) * 0.5f);

        if (IsBodyHit(target, drops) || IsCloneHit(target, clone.position, drops))
        {
            context.Damage.Strike(_hit, target.GetAimPoint(Vector3.zero), 0f);
        }

        if (null == _groundSplat || null == context.PaintCanvas)
        {
            return;
        }

        for (int i = 0; i < drops.Count; i++)
        {
            context.PaintCanvas.LeaveSplats(drops[i], context.Damage.PickColor(), true, _groundSplat);
        }
    }

    private void AddDrops(Vector3 center, List<Vector3> drops)
    {
        drops.Add(center);

        for (int i = 1; i < _dropsPerBody; i++)
        {
            drops.Add(center + (Vector3)(UnityEngine.Random.insideUnitCircle * _spreadRadius));
        }
    }

    private bool IsBodyHit(AttackTarget target, List<Vector3> drops)
    {
        for (int i = 0; i < drops.Count; i++)
        {
            if (target.IsInBlast(drops[i], _dropRadius))
            {
                return true;
            }
        }

        return false;
    }

    // 분신도 플레이어와 같은 규칙으로 맞는다. 플레이어가 대시로 피하는 중이면 분신도 함께 피한다.
    private bool IsCloneHit(AttackTarget target, Vector3 clone, List<Vector3> drops)
    {
        if (target.Unit.IsDodging)
        {
            return false;
        }

        for (int i = 0; i < drops.Count; i++)
        {
            if (Vector2.Distance(drops[i], clone) <= _dropRadius)
            {
                return true;
            }
        }

        return false;
    }
}
