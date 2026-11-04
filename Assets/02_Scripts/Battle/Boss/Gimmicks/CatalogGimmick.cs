using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 소장 목록. 판을 칸으로 나눠 터질 순서를 한 칸씩 보여 준 뒤 감추고, 그 순서대로 아주 짧은 예고만 주고 터뜨린다.
// 이미 터진 칸으로 옮겨 다니며 모든 판을 맞지 않고 버티면 그로기, 한 번이라도 맞으면 실패다.
[CreateAssetMenu(fileName = "CatalogGimmick", menuName = "Scriptable Objects/Boss/Gimmick/Catalog")]
public class CatalogGimmick : PunishingBossGimmick
{
    [Header("칸")]
    [SerializeField, Min(1)] private int _columns = 3;
    [SerializeField, Min(1)] private int _rows = 3;
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField, Min(0f)] private float _tileGap = 0.4f;
    [SerializeField, Min(0.01f)] private float _edgeWidth = 0.2f;
    [SerializeField] private Color _edgeColor = new Color(0.62f, 0.48f, 0.2f, 0.7f);
    [SerializeField] private Color _showColor = new Color(0.95f, 0.78f, 0.25f, 0.45f);
    [SerializeField] private Color _warnColor = new Color(0.85f, 0.1f, 0.1f, 0.45f);
    [SerializeField] private Color _clearedColor = new Color(0.1f, 0.1f, 0.12f, 0.25f);

    [Header("순서")]
    [SerializeField, Min(1)] private int _rounds = 2;
    [SerializeField, Min(0.05f)] private float _showDuration = 0.35f;
    [SerializeField, Min(0f)] private float _showGap = 0.1f;
    [SerializeField, Min(0f)] private float _memorizeDelay = 1f;

    [Header("터짐")]
    [SerializeField, Min(0.05f)] private float _flashDuration = 0.25f;
    [SerializeField, Min(0f)] private float _detonateGap = 0.35f;
    [SerializeField] private ImpactSettings _detonateImpact = new();

    [Header("결과")]
    [SerializeField] private ImpactSettings _successImpact = new();

    public override void Prewarm()
    {
        if (null != _tilePrefab)
        {
            GameManager.Pool.Prewarm(_tilePrefab, _columns * _rows * GimmickFrame.SPRITE_COUNT);
        }
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null == _tilePrefab || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            Debug.LogError($"소장 목록 : {name}에 칸 그림이 없거나 플레이어 이동 범위를 알 수 없습니다.");
            return true;
        }

        Vector2 cell = new Vector2(area.width / _columns, area.height / _rows);
        List<GimmickFrame> tiles = ListPool<GimmickFrame>.Get();
        List<int> order = ListPool<int>.Get();

        try
        {
            BuildTiles(area, cell, tiles, order);

            for (int round = 0; round < _rounds; round++)
            {
                ResetTiles(tiles);
                Shuffle(order);

                await ShowOrderAsync(tiles, order, token);
                await UniTask.Delay(TimeSpan.FromSeconds(_memorizeDelay), cancellationToken: token);

                if (false == await DetonateAsync(context, area, cell, tiles, order, token))
                {
                    PunishTarget(context);
                    return false;
                }
            }

            GameManager.Feedback.PlayImpact(_successImpact, context.Target.GetAimPoint(Vector3.zero));

            return true;
        }
        finally
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].Despawn();
            }

            ListPool<GimmickFrame>.Release(tiles);
            ListPool<int>.Release(order);
        }
    }

    // 칸 번호는 왼쪽 아래부터 가로로 센다.
    private void BuildTiles(Rect area, Vector2 cell, List<GimmickFrame> tiles, List<int> order)
    {
        Vector2 size = cell - new Vector2(_tileGap, _tileGap);

        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                GimmickFrame tile = new GimmickFrame(_tilePrefab, size, _edgeWidth, _edgeColor, _showColor);

                tile.Place(new Vector3(area.xMin + cell.x * (column + 0.5f), area.yMin + cell.y * (row + 0.5f), 0f));
                order.Add(tiles.Count);
                tiles.Add(tile);
            }
        }
    }

    private static void ResetTiles(List<GimmickFrame> tiles)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].SetFill(0f);
        }
    }

    private static void Shuffle(List<int> order)
    {
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
    }

    private async UniTask ShowOrderAsync(List<GimmickFrame> tiles, List<int> order, CancellationToken token)
    {
        for (int i = 0; i < order.Count; i++)
        {
            GimmickFrame tile = tiles[order[i]];

            tile.SetFillColor(_showColor);
            tile.SetFill(1f);

            await UniTask.Delay(TimeSpan.FromSeconds(_showDuration), cancellationToken: token);

            tile.SetFill(0f);

            await UniTask.Delay(TimeSpan.FromSeconds(_showGap), cancellationToken: token);
        }
    }

    // 끝까지 한 번도 맞지 않았으면 true다.
    private async UniTask<bool> DetonateAsync(AttackContext context, Rect area, Vector2 cell, List<GimmickFrame> tiles, List<int> order,
        CancellationToken token)
    {
        AttackTarget target = context.Target;

        for (int i = 0; i < order.Count; i++)
        {
            GimmickFrame tile = tiles[order[i]];

            tile.SetFillColor(_warnColor);
            await FillAsync(tile, token);

            if (target.IsDefeated)
            {
                return false;
            }

            GameManager.Feedback.PlayImpact(_detonateImpact, tile.Center);

            if (order[i] == GetCellIndex(area, cell, target.FollowTransform.position))
            {
                return false;
            }

            tile.SetFillColor(_clearedColor);

            await UniTask.Delay(TimeSpan.FromSeconds(_detonateGap), cancellationToken: token);
        }

        return true;
    }

    private async UniTask FillAsync(GimmickFrame tile, CancellationToken token)
    {
        float elapsed = 0f;

        while (elapsed < _flashDuration)
        {
            tile.SetFill(elapsed / _flashDuration);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }

        tile.SetFill(1f);
    }

    // 칸 사이 틈에 서도 피할 수 없게, 그림이 아니라 판을 나눈 칸으로 본다.
    private int GetCellIndex(Rect area, Vector2 cell, Vector3 position)
    {
        int column = Mathf.Clamp(Mathf.FloorToInt((position.x - area.xMin) / cell.x), 0, _columns - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt((position.y - area.yMin) / cell.y), 0, _rows - 1);

        return row * _columns + column;
    }
}
