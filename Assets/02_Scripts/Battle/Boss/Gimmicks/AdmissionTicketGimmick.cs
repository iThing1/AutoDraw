using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 관람권. 판 곳곳에 흩어진 관람권을 폐관 전에 주워야 한다.
// 폐관 때 못 주운 장수만큼 막을 수 없는 피해를 받고, 모두 주웠으면 그로기로 이어진다.
[CreateAssetMenu(fileName = "AdmissionTicketGimmick", menuName = "Scriptable Objects/Boss/Gimmick/AdmissionTicket")]
public class AdmissionTicketGimmick : BossGimmick
{
    private const int MAX_PICK_TRIES = 12;

    [Header("관람권")]
    [SerializeField, Min(1)] private int _ticketCount = 4;
    [SerializeField] private GameObject _ticketPrefab;
    [SerializeField] private Vector2 _ticketSize = new Vector2(0.6f, 0.9f);
    [SerializeField] private Color _ticketColor = new Color(0.95f, 0.78f, 0.25f, 1f);
    // 관람권끼리, 그리고 플레이어 시작 자리에서 떨어뜨릴 거리.
    [SerializeField, Min(0f)] private float _minSpacing = 6f;
    [SerializeField, Min(0.1f)] private float _pickRadius = 0.8f;
    [SerializeField] private ImpactSettings _pickImpact = new();
    [SerializeField] private Vector3 _heldOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField, Min(0f)] private float _heldSpacing = 0.5f;
    [SerializeField, Range(0.1f, 1f)] private float _heldScale = 0.6f;

    [Header("폐관")]
    [SerializeField, Min(0.1f)] private float _duration = 8f;
    [SerializeField] private GameObject _shutterPrefab;
    [SerializeField, Min(0.01f)] private float _edgeWidth = 0.3f;
    [SerializeField] private Color _shutterEdgeColor = new Color(0.85f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color _shutterFillColor = new Color(0.85f, 0.1f, 0.1f, 0.18f);
    // 못 주운 관람권 한 장마다 한 번씩 맞는다.
    [SerializeField] private HitSettings _missHit = new();
    [SerializeField] private ImpactSettings _missImpact = new();
    [SerializeField, Min(0f)] private float _missInterval = 0.15f;

    [Header("물감 방울")]
    [SerializeField, Min(0.1f)] private float _dropInterval = 1.4f;
    [SerializeField, Min(0.1f)] private float _dropRadius = 1.4f;
    [SerializeField] private GameObject _dropPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.35f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.8f;
    [SerializeField] private HitSettings _dropHit = new();
    [SerializeField] private ImpactSettings _dropImpact = new();

    [Header("결과")]
    [SerializeField] private ImpactSettings _successImpact = new();

    public override void Prewarm()
    {
        if (null != _ticketPrefab)
        {
            GameManager.Pool.Prewarm(_ticketPrefab, _ticketCount);
        }

        if (null != _shutterPrefab)
        {
            GameManager.Pool.Prewarm(_shutterPrefab, GimmickFrame.SPRITE_COUNT);
        }

        if (null != _dropPrefab)
        {
            GameManager.Pool.Prewarm(_dropPrefab, 4);
        }
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null == _ticketPrefab || null == _shutterPrefab || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            Debug.LogError($"관람권 : {name}에 그림 프리팹이 없거나 플레이어 이동 범위를 알 수 없습니다.");
            return true;
        }

        GimmickFrame shutter = new GimmickFrame(_shutterPrefab, area.size, _edgeWidth, _shutterEdgeColor, _shutterFillColor);
        List<SpriteRenderer> tickets = ListPool<SpriteRenderer>.Get();
        List<SpriteRenderer> held = ListPool<SpriteRenderer>.Get();

        using (CancellationTokenSource dropSource = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            try
            {
                shutter.Place(area.center);
                shutter.SetFill(0f);
                SpawnTickets(area, context.Target.FollowTransform.position, tickets);

                await CollectAsync(context, shutter, tickets, held, dropSource.Token, token);

                dropSource.Cancel();

                int missing = tickets.Count;

                if (0 == missing)
                {
                    GameManager.Feedback.PlayImpact(_successImpact, context.Target.GetAimPoint(Vector3.zero));
                    return true;
                }

                await StrikeMissingAsync(context, missing, token);

                return false;
            }
            finally
            {
                dropSource.Cancel();
                shutter.Despawn();
                DespawnAll(tickets);
                DespawnAll(held);
                ListPool<SpriteRenderer>.Release(tickets);
                ListPool<SpriteRenderer>.Release(held);
            }
        }
    }

    private void SpawnTickets(Rect area, Vector3 playerPosition, List<SpriteRenderer> tickets)
    {
        List<Vector3> taken = ListPool<Vector3>.Get();

        try
        {
            taken.Add(playerPosition);

            for (int i = 0; i < _ticketCount; i++)
            {
                Vector3 point = PickTicketPoint(area, taken);

                taken.Add(point);
                tickets.Add(GameManager.Pool.SpawnSprite(_ticketPrefab, point, 0f, _ticketSize, _ticketColor));
            }
        }
        finally
        {
            ListPool<Vector3>.Release(taken);
        }
    }

    // 다른 자리와 충분히 떨어진 곳을 몇 번 골라 보고, 끝내 못 찾으면 마지막에 고른 자리를 쓴다.
    private Vector3 PickTicketPoint(Rect area, List<Vector3> taken)
    {
        Vector3 point = area.center;

        for (int i = 0; i < MAX_PICK_TRIES; i++)
        {
            point = new Vector3(UnityEngine.Random.Range(area.xMin, area.xMax), UnityEngine.Random.Range(area.yMin, area.yMax), 0f);

            if (IsFarFromAll(point, taken))
            {
                break;
            }
        }

        return point;
    }

    private bool IsFarFromAll(Vector3 point, List<Vector3> taken)
    {
        for (int i = 0; i < taken.Count; i++)
        {
            if (Vector2.Distance(point, taken[i]) < _minSpacing)
            {
                return false;
            }
        }

        return true;
    }

    // 주운 관람권은 tickets에서 held로 옮긴다. 다 줍거나 시간이 다 되면 끝난다.
    private async UniTask CollectAsync(AttackContext context, GimmickFrame shutter, List<SpriteRenderer> tickets, List<SpriteRenderer> held,
        CancellationToken dropToken, CancellationToken token)
    {
        AttackTarget target = context.Target;
        float elapsed = 0f;
        float nextDrop = _dropInterval;

        while (elapsed < _duration && 0 < tickets.Count && false == target.IsDefeated)
        {
            for (int i = tickets.Count - 1; i >= 0; i--)
            {
                if (false == target.IsTouching(tickets[i].transform.position, _pickRadius))
                {
                    continue;
                }

                GameManager.Feedback.PlayImpact(_pickImpact, tickets[i].transform.position);
                tickets[i].transform.localScale = new Vector3(_ticketSize.x * _heldScale, _ticketSize.y * _heldScale, 1f);
                held.Add(tickets[i]);
                tickets.RemoveAt(i);
            }

            PlaceHeld(target, held);
            shutter.SetFill(elapsed / _duration);

            if (elapsed >= nextDrop)
            {
                nextDrop += _dropInterval;
                DropAsync(context, dropToken).Forget();
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    private void PlaceHeld(AttackTarget target, List<SpriteRenderer> held)
    {
        Vector3 center = target.FollowTransform.position + _heldOffset;
        float firstOffset = -_heldSpacing * (held.Count - 1) * 0.5f;

        for (int i = 0; i < held.Count; i++)
        {
            held[i].transform.position = center + new Vector3(firstOffset + _heldSpacing * i, 0f, 0f);
        }
    }

    private async UniTask StrikeMissingAsync(AttackContext context, int missing, CancellationToken token)
    {
        AttackTarget target = context.Target;

        for (int i = 0; i < missing && false == target.IsDefeated; i++)
        {
            GameManager.Feedback.PlayImpact(_missImpact, target.GetAimPoint(Vector3.zero));
            context.Damage.StrikeUnblockable(_missHit, target.GetAimPoint(Vector3.zero));

            if (i < missing - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_missInterval), cancellationToken: token);
            }
        }
    }

    private async UniTaskVoid DropAsync(AttackContext context, CancellationToken token)
    {
        Vector3 point = context.Target.GetAimPoint(Vector3.zero);

        try
        {
            await Telegraph.PlayCircleAsync(GameManager.Pool, _dropPrefab, point, _dropRadius, _telegraphColor, _warnDuration, token);

            GameManager.Feedback.PlayImpact(_dropImpact, point);
            context.Damage.Blast(_dropHit, point, _dropRadius, 0f);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void DespawnAll(List<SpriteRenderer> sprites)
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            GameManager.Pool.DespawnSprite(sprites[i]);
        }
    }
}
