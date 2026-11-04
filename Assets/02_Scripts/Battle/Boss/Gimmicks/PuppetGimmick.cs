using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 꼭두각시. 플레이어와 보스 사이에 연필 실이 이어지고, 흉내쟁이 보스가 플레이어의 움직임을 같은 방향·같은 거리로 따라 한다(대시도 따라 한다).
// 판에 빈 액자가 하나씩 걸리고, 플레이어가 움직여 보스를 액자 안에 잠시 머물게 하면 액자가 닫히며 보스를 가둔다.
// 그동안 플레이어 둘레에는 먹 방울이 떨어져, 보스를 보내는 길이 플레이어에게도 안전해야 한다.
// 정해진 시간 안에 정해진 수만큼 가두면 그로기, 시간이 다 되면 보스가 실을 끊고 반격한다. 실은 시간이 갈수록 옅어져 남은 시간을 보여 준다.
[CreateAssetMenu(fileName = "PuppetGimmick", menuName = "Scriptable Objects/Boss/Gimmick/Puppet")]
public class PuppetGimmick : PunishingBossGimmick
{
    [Header("실")]
    // 월드 좌표를 쓰는 LineRenderer 프리팹(03_Prefabs/Effects/InkPath).
    [SerializeField] private LineRenderer _threadPrefab;
    [SerializeField] private Color _threadColor = new Color(0.35f, 0.35f, 0.38f, 0.8f);
    [SerializeField, Min(0.01f)] private float _threadWidth = 0.12f;
    // 다 옅어졌을 때 남는 실의 굵기·투명도 비율.
    [SerializeField, Range(0f, 1f)] private float _endThreadRatio = 0.2f;
    [SerializeField] private ImpactSettings _linkImpact = new();

    [Header("가두기")]
    [SerializeField, Min(0.1f)] private float _duration = 14f;
    [SerializeField, Min(1)] private int _requiredCaptures = 3;
    // 액자 그림. 1x1 네모 그림을 늘려 쓴다.
    [SerializeField] private GameObject _framePrefab;
    [SerializeField] private Vector2 _frameSize = new Vector2(3.2f, 3.2f);
    [SerializeField, Min(0.01f)] private float _edgeWidth = 0.25f;
    [SerializeField] private Color _edgeColor = new Color(0.1f, 0.1f, 0.12f, 1f);
    [SerializeField] private Color _fillColor = new Color(0.1f, 0.1f, 0.12f, 0.18f);
    [SerializeField] private Color _capturedColor = new Color(0.95f, 0.72f, 0.2f, 1f);
    // 보스에게서 이 거리만큼 떨어진 곳에 액자를 건다.
    [SerializeField] private FloatRange _frameDistance = new FloatRange(5f, 9f);
    // 보스 몸이 액자 안에 이만큼 머물러야 갇힌다. 벗어나면 처음부터 센다.
    [SerializeField, Min(0f)] private float _captureHold = 0.4f;
    [SerializeField, Min(0f)] private float _frameFadeDuration = 0.3f;
    [SerializeField] private ImpactSettings _captureImpact = new();

    [Header("먹 방울")]
    [SerializeField, Min(0.1f)] private float _dropInterval = 1.3f;
    // 첫 방울은 플레이어 바로 위에 떨어져 계속 움직이게 한다.
    [SerializeField, Min(1)] private int _dropCount = 3;
    [SerializeField, Min(0.1f)] private float _dropRadius = 1.6f;
    [SerializeField, Min(0f)] private float _spreadRadius = 4f;
    [SerializeField] private GameObject _dropPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.35f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.8f;
    [SerializeField] private HitSettings _dropHit = new();
    [SerializeField] private ImpactSettings _dropImpact = new();

    [Header("결과")]
    [SerializeField] private ImpactSettings _successImpact = new();
    // 시간이 다 되어 실이 끊길 때. 이어서 막을 수 없는 반격이 온다.
    [SerializeField] private ImpactSettings _snapImpact = new();

    public override void Prewarm()
    {
        if (null != _threadPrefab)
        {
            GameManager.Pool.Prewarm(_threadPrefab.gameObject, 1);
        }

        if (null != _framePrefab)
        {
            GameManager.Pool.Prewarm(_framePrefab, _requiredCaptures * GimmickFrame.SPRITE_COUNT);
        }

        if (null != _dropPrefab)
        {
            GameManager.Pool.Prewarm(_dropPrefab, _dropCount * 4);
        }
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        if (null == _threadPrefab || null == _framePrefab)
        {
            Debug.LogError($"꼭두각시 : {name}에 실이나 액자 그림이 지정되지 않았습니다.");
            return true;
        }

        LineRenderer thread = GameManager.Pool.SpawnLine(_threadPrefab, _threadWidth, _threadColor);
        List<GimmickFrame> frames = ListPool<GimmickFrame>.Get();
        List<UniTask> fades = ListPool<UniTask>.Get();

        thread.positionCount = 2;

        // 먹 방울은 기믹이 끝나면 함께 끊어, 끝난 뒤에 떨어지지 않게 한다.
        using (CancellationTokenSource dropSource = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            try
            {
                GameManager.Feedback.PlayImpact(_linkImpact, context.Owner.Body.position);

                bool isCaptured = await PullStringsAsync(context, thread, frames, fades, dropSource.Token, token);

                dropSource.Cancel();

                if (isCaptured)
                {
                    GameManager.Feedback.PlayImpact(_successImpact, context.Owner.Body.position);
                }
                else
                {
                    GameManager.Feedback.PlayImpact(_snapImpact, context.Owner.Body.position);
                    PunishTarget(context);
                }

                await UniTask.WhenAll(fades);

                return isCaptured;
            }
            finally
            {
                dropSource.Cancel();
                GameManager.Pool.DespawnLine(thread);

                for (int i = 0; i < frames.Count; i++)
                {
                    frames[i].Despawn();
                }

                ListPool<GimmickFrame>.Release(frames);
                ListPool<UniTask>.Release(fades);
            }
        }
    }

    // 플레이어가 움직인 만큼 보스를 옮기고, 보스가 액자에 머문 시간을 센다. 정해진 수만큼 가뒀으면 true다.
    // 움직인 양은 넉백으로 흔들리는 몸이 아니라 루트끼리 재, 보스가 맞을 때마다 덜컹이지 않게 한다.
    private async UniTask<bool> PullStringsAsync(AttackContext context, LineRenderer thread, List<GimmickFrame> frames, List<UniTask> fades,
        CancellationToken dropToken, CancellationToken token)
    {
        AttackTarget target = context.Target;
        Transform boss = context.Owner.transform;
        Transform player = target.Unit.transform;
        UnitMover bossMover = context.Owner.Mover;
        Vector3 lastPlayer = player.position;
        GimmickFrame frame = SpawnFrame(context, frames);
        float elapsed = 0f;
        float nextDrop = _dropInterval;
        float hold = 0f;
        int captures = 0;

        while (elapsed < _duration && false == target.IsDefeated)
        {
            Vector3 moved = player.position - lastPlayer;
            Vector3 next = boss.position + new Vector3(moved.x, moved.y, 0f);

            boss.position = null != bossMover ? bossMover.ClampPosition(next) : next;
            lastPlayer = player.position;

            UpdateThread(thread, target.FollowTransform.position, context.Owner.Body.position, elapsed / _duration);

            hold = frame.Contains(context.Owner.Body.position) ? hold + Time.deltaTime : 0f;
            frame.SetFill(0f < _captureHold ? hold / _captureHold : 1f);

            if (hold >= _captureHold)
            {
                captures++;
                hold = 0f;
                CloseFrame(frame, fades, token);
                GameManager.Feedback.PlayImpact(_captureImpact, frame.Center);

                if (captures >= _requiredCaptures)
                {
                    return true;
                }

                frame = SpawnFrame(context, frames);
            }

            if (elapsed >= nextDrop)
            {
                nextDrop += _dropInterval;
                DropAsync(context, dropToken).Forget();
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }

        CloseFrame(frame, fades, token);

        return false;
    }

    // 실은 남은 시간이 줄수록 가늘고 옅어진다.
    private void UpdateThread(LineRenderer thread, Vector3 player, Vector3 boss, float progress)
    {
        float ratio = Mathf.Lerp(1f, _endThreadRatio, progress);

        thread.SetPosition(0, player);
        thread.SetPosition(1, boss);
        thread.widthMultiplier = _threadWidth * ratio;
        PathLine.SetColor(thread, new Color(_threadColor.r, _threadColor.g, _threadColor.b, _threadColor.a * ratio));
    }

    private GimmickFrame SpawnFrame(AttackContext context, List<GimmickFrame> frames)
    {
        GimmickFrame frame = new GimmickFrame(_framePrefab, _frameSize, _edgeWidth, _edgeColor, _fillColor);

        frame.Place(PickFramePoint(context));
        frame.SetFill(0f);
        frames.Add(frame);

        return frame;
    }

    // 다 찬 액자는 테두리 색을 바꾸고 옅어진다. 다음 액자를 기다리게 하지 않도록 옅어지는 동안 기믹을 이어 간다.
    private void CloseFrame(GimmickFrame frame, List<UniTask> fades, CancellationToken token)
    {
        frame.SetEdgeColor(_capturedColor);
        frame.AddFades(0f, 0f, _frameFadeDuration, fades, token);
    }

    // 보스를 보낼 자리와, 그만큼 플레이어가 움직일 자리가 둘 다 각자의 이동 범위 안인 곳을 고른다.
    // 두 몸이 함께 움직일 수 있는 거리는 두 이동 범위를 각자 자리만큼 옮겨 겹친 네모라, 고른 거리를 그 안으로 당겨 처음 보고 그대로 가도 늘 닿게 한다.
    // 판 구석에서는 정한 거리보다 가까워질 수 있다.
    private Vector3 PickFramePoint(AttackContext context)
    {
        Vector3 boss = context.Owner.transform.position;
        Vector3 player = context.Target.Unit.transform.position;
        Vector2 min = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        Vector2 max = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

        NarrowReach(context.Owner.Mover, boss, ref min, ref max);
        NarrowReach(context.Target.Unit.Mover, player, ref min, ref max);

        Vector3 offset = Geometry2D.ToDirection(UnityEngine.Random.Range(0f, 360f)) * _frameDistance.PickRandom();

        // 몸이 범위 끝에 살짝 걸쳐 있어도 제자리(0)는 늘 고를 수 있게 둔다.
        offset.x = Mathf.Clamp(offset.x, Mathf.Min(min.x, 0f), Mathf.Max(max.x, 0f));
        offset.y = Mathf.Clamp(offset.y, Mathf.Min(min.y, 0f), Mathf.Max(max.y, 0f));

        // 액자는 보스 몸이 들어갈 자리에 건다. 몸은 루트에서 조금 떨어져 있을 수 있다.
        return boss + offset + (context.Owner.Body.position - boss);
    }

    // 이 몸이 from에서 이동 범위를 벗어나지 않고 움직일 수 있는 거리로 min~max를 좁힌다. 범위가 없으면 그대로 둔다.
    private static void NarrowReach(UnitMover mover, Vector3 from, ref Vector2 min, ref Vector2 max)
    {
        if (null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            return;
        }

        min = Vector2.Max(min, area.min - (Vector2)from);
        max = Vector2.Min(max, area.max - (Vector2)from);
    }

    private async UniTaskVoid DropAsync(AttackContext context, CancellationToken token)
    {
        AttackTarget target = context.Target;
        Vector3 center = target.FollowTransform.position;
        List<Vector3> drops = ListPool<Vector3>.Get();

        drops.Add(center);

        for (int i = 1; i < _dropCount; i++)
        {
            drops.Add(center + (Vector3)(UnityEngine.Random.insideUnitCircle * _spreadRadius));
        }

        try
        {
            await Telegraph.PlayCirclesAsync(GameManager.Pool, _dropPrefab, drops, _dropRadius, _telegraphColor, _warnDuration, token);

            GameManager.Feedback.PlayImpact(_dropImpact, center);

            if (target.IsInAnyBlast(drops, _dropRadius))
            {
                context.Damage.Strike(_dropHit, target.GetAimPoint(Vector3.zero), 0f);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            ListPool<Vector3>.Release(drops);
        }
    }
}
