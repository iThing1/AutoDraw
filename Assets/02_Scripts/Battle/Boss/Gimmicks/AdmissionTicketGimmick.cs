using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 관람권. 플레이어 둘레 구역에 흩어진 관람권을 폐관 셔터가 다 내려오기 전에 주워야 한다.
// 폐관 때 못 주운 장수만큼 막을 수 없는 피해를 받고, 모두 주웠으면 그로기로 이어진다.
[CreateAssetMenu(fileName = "AdmissionTicketGimmick", menuName = "Scriptable Objects/Boss/Gimmick/AdmissionTicket")]
public class AdmissionTicketGimmick : BossGimmick
{
    private const int MAX_PICK_TRIES = 12;

    [Header("구역")]
    // 관람권은 플레이어 둘레 이 크기 안에만 뿌린다. 카메라와 셔터도 이 구역을 비춘다.
    [SerializeField] private Vector2 _fieldSize = new Vector2(28f, 16f);

    [Header("관람권")]
    [SerializeField, Min(1)] private int _ticketCount = 4;
    [SerializeField] private GameObject _ticketPrefab;
    [SerializeField] private Vector2 _ticketSize = new Vector2(0.8f, 1.1f);
    [SerializeField] private Color _ticketColor = new Color(1f, 0.62f, 0.1f, 1f);
    [SerializeField, Min(0.01f)] private float _ticketEdgeWidth = 0.12f;
    [SerializeField] private Color _ticketEdgeColor = new Color(0.15f, 0.08f, 0.03f, 1f);
    // 바닥에 놓인 관람권은 이 색과 오가며 반짝인다.
    [SerializeField] private Color _glintColor = new Color(1f, 0.97f, 0.8f, 1f);
    [SerializeField, Min(0.05f)] private float _glintPeriod = 0.6f;
    // 관람권끼리, 그리고 플레이어 시작 자리에서 떨어뜨릴 거리.
    [SerializeField, Min(0f)] private float _minSpacing = 6f;
    [SerializeField, Min(0.1f)] private float _pickRadius = 0.8f;
    [SerializeField] private ImpactSettings _pickImpact = new();
    [SerializeField] private Vector3 _heldOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField, Min(0f)] private float _heldSpacing = 0.6f;
    [SerializeField, Range(0.1f, 1f)] private float _heldScale = 0.6f;

    [Header("폐관")]
    // 플레이어 자리에서 가까운 관람권부터 차례로 도는 거리를 routeSpeed로 걸을 시간에 graceTime을 더해 준다.
    [SerializeField, Min(0f)] private float _graceTime = 2.5f;
    [SerializeField, Min(0.1f)] private float _routeSpeed = 5f;
    [SerializeField] private GameObject _shutterPrefab;
    [SerializeField, Min(0.01f)] private float _edgeWidth = 0.3f;
    [SerializeField] private Color _shutterEdgeColor = new Color(0.35f, 0.38f, 0.45f, 0.85f);
    [SerializeField] private Color _shutterFillColor = new Color(0.35f, 0.38f, 0.45f, 0.18f);
    // 셔터가 내려올수록 이 색으로 붉어진다.
    [SerializeField] private Color _shutterWarnColor = new Color(0.85f, 0.1f, 0.1f, 0.3f);
    // 못 주운 관람권 한 장마다 한 번씩 맞는다.
    [SerializeField] private HitSettings _missHit = new();
    [SerializeField] private ImpactSettings _missImpact = new();
    [SerializeField, Min(0f)] private float _missInterval = 0.15f;

    [Header("물감 방울")]
    [SerializeField, Min(0.1f)] private float _dropInterval = 1.4f;
    [SerializeField] private GroundDropSettings _drop = new();

    [Header("결과")]
    [SerializeField] private ImpactSettings _successImpact = new();

    [Header("카메라")]
    [SerializeField, Min(0f)] private float _cameraPadding = 1f;
    [SerializeField, Min(0f)] private float _cameraBlendDuration = 0.5f;

    public override void Prewarm()
    {
        if (null != _ticketPrefab)
        {
            GameManager.Pool.Prewarm(_ticketPrefab, _ticketCount * GimmickFrame.SPRITE_COUNT);
        }

        if (null != _shutterPrefab)
        {
            GameManager.Pool.Prewarm(_shutterPrefab, GimmickFrame.SPRITE_COUNT);
        }

        GroundDrop.Prewarm(GameManager.Pool, _drop, 4);
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null == _ticketPrefab || null == _shutterPrefab || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            Debug.LogError($"관람권 : {name}에 그림 프리팹이 없거나 플레이어 이동 범위를 알 수 없습니다.");
            return true;
        }

        Vector3 start = context.Target.FollowTransform.position;
        Rect field = GetField(area, start);
        GimmickFrame shutter = new GimmickFrame(_shutterPrefab, field.size, _edgeWidth, _shutterEdgeColor, _shutterFillColor);
        List<GimmickFrame> tickets = ListPool<GimmickFrame>.Get();
        List<GimmickFrame> held = ListPool<GimmickFrame>.Get();

        using (CancellationTokenSource dropSource = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            try
            {
                GameManager.CameraRig.ShowArea(new Bounds(field.center, field.size), _cameraPadding, _cameraBlendDuration);

                shutter.Place(field.center);
                shutter.SetFillDown(0f);
                SpawnTickets(field, start, tickets);

                float duration = _graceTime + GetRouteLength(start, tickets) / _routeSpeed;

                await CollectAsync(context, shutter, tickets, held, duration, dropSource.Token, token);

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
                GameManager.CameraRig.EndShowArea(_cameraBlendDuration);
                dropSource.Cancel();
                shutter.Despawn();
                DespawnAll(tickets);
                DespawnAll(held);
                ListPool<GimmickFrame>.Release(tickets);
                ListPool<GimmickFrame>.Release(held);
            }
        }
    }

    // 판이 넓어 끝과 끝에 뿌리면 시간 안에 다 돌 수 없으므로, 플레이어 둘레로 구역을 좁히되 판 밖으로는 나가지 않게 한다.
    private Rect GetField(Rect area, Vector3 player)
    {
        Vector2 size = Vector2.Min(_fieldSize, area.size);
        float x = Mathf.Clamp(player.x - size.x * 0.5f, area.xMin, area.xMax - size.x);
        float y = Mathf.Clamp(player.y - size.y * 0.5f, area.yMin, area.yMax - size.y);

        return new Rect(x, y, size.x, size.y);
    }

    // 가장 가까운 관람권부터 차례로 줍는 길이. 가장 짧은 길은 아니지만 사람이 실제로 도는 길에 가깝다.
    private static float GetRouteLength(Vector3 start, List<GimmickFrame> tickets)
    {
        List<Vector3> left = ListPool<Vector3>.Get();

        try
        {
            for (int i = 0; i < tickets.Count; i++)
            {
                left.Add(tickets[i].Center);
            }

            float length = 0f;
            Vector3 current = start;

            while (0 < left.Count)
            {
                int nearest = 0;
                float nearestDistance = float.MaxValue;

                for (int i = 0; i < left.Count; i++)
                {
                    float distance = Vector2.Distance(current, left[i]);

                    if (distance < nearestDistance)
                    {
                        nearest = i;
                        nearestDistance = distance;
                    }
                }

                length += nearestDistance;
                current = left[nearest];
                left.RemoveAt(nearest);
            }

            return length;
        }
        finally
        {
            ListPool<Vector3>.Release(left);
        }
    }

    private void SpawnTickets(Rect area, Vector3 playerPosition, List<GimmickFrame> tickets)
    {
        List<Vector3> taken = ListPool<Vector3>.Get();

        try
        {
            taken.Add(playerPosition);

            for (int i = 0; i < _ticketCount; i++)
            {
                Vector3 point = PickTicketPoint(area, taken);
                GimmickFrame ticket = CreateTicket(1f);

                taken.Add(point);
                PlaceTicket(ticket, point);
                tickets.Add(ticket);
            }
        }
        finally
        {
            ListPool<Vector3>.Release(taken);
        }
    }

    private GimmickFrame CreateTicket(float scale)
    {
        return new GimmickFrame(_ticketPrefab, _ticketSize * scale, _ticketEdgeWidth * scale, _ticketEdgeColor, _ticketColor);
    }

    // 액자는 Place가 테두리만 옮기므로, 안쪽 면도 다시 채워 같이 옮긴다.
    private static void PlaceTicket(GimmickFrame ticket, Vector3 point)
    {
        ticket.Place(point);
        ticket.SetFill(1f);
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

    // 주운 관람권은 바닥에서 거두고 머리 위에 작게 새로 띄운다. 다 줍거나 시간이 다 되면 끝난다.
    private async UniTask CollectAsync(AttackContext context, GimmickFrame shutter, List<GimmickFrame> tickets, List<GimmickFrame> held, float duration,
        CancellationToken dropToken, CancellationToken token)
    {
        AttackTarget target = context.Target;
        float elapsed = 0f;
        float nextDrop = _dropInterval;

        while (elapsed < duration && 0 < tickets.Count && false == target.IsDefeated)
        {
            for (int i = tickets.Count - 1; i >= 0; i--)
            {
                Vector3 point = tickets[i].Center;

                if (false == target.IsTouching(point, _pickRadius))
                {
                    continue;
                }

                GameManager.Feedback.PlayImpact(_pickImpact, point);
                tickets[i].Despawn();
                tickets.RemoveAt(i);
                held.Add(CreateTicket(_heldScale));
            }

            Glint(tickets, elapsed);
            PlaceHeld(target, held);

            float progress = elapsed / duration;
            shutter.SetFillDown(progress);
            shutter.SetFillColor(Color.Lerp(_shutterFillColor, _shutterWarnColor, progress * progress));

            if (elapsed >= nextDrop)
            {
                nextDrop += _dropInterval;
                GroundDrop.PlayAsync(GameManager.Pool, GameManager.Feedback, context, _drop, target.GetAimPoint(Vector3.zero), dropToken).Forget();
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    private void Glint(List<GimmickFrame> tickets, float elapsed)
    {
        float wave = 0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI * 2f / _glintPeriod);
        Color color = Color.Lerp(_ticketColor, _glintColor, wave);

        for (int i = 0; i < tickets.Count; i++)
        {
            tickets[i].SetFillColor(color);
        }
    }

    private void PlaceHeld(AttackTarget target, List<GimmickFrame> held)
    {
        Vector3 center = target.FollowTransform.position + _heldOffset;
        float firstOffset = -_heldSpacing * (held.Count - 1) * 0.5f;

        for (int i = 0; i < held.Count; i++)
        {
            PlaceTicket(held[i], center + new Vector3(firstOffset + _heldSpacing * i, 0f, 0f));
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

    private static void DespawnAll(List<GimmickFrame> frames)
    {
        for (int i = 0; i < frames.Count; i++)
        {
            frames[i].Despawn();
        }
    }
}
