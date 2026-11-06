using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 바닥 경보. 작은 구역에 플레이어를 가두고, 정해진 순서의 패턴대로 칸들을 예고한 뒤 터뜨린다.
// 순서가 늘 같아 외울 수 있고, 판마다 빨라진다. 맞아도 끝까지 이어 가며, 한 번도 맞지 않아야 그로기다.
[CreateAssetMenu(fileName = "FloorAlarmGimmick", menuName = "Scriptable Objects/Boss/Gimmick/FloorAlarm")]
public class FloorAlarmGimmick : PunishingBossGimmick
{
    private const char BOMB_MARK = 'X';

    // 터질 칸을 그림처럼 적은 한 번의 폭발. 첫 줄이 맨 위이고, X가 터지는 칸이다.
    [Serializable]
    private class Wave
    {
        [SerializeField] private string[] _rows;

        public bool IsBomb(int row, int column, int rowCount)
        {
            int line = rowCount - 1 - row;

            return line < _rows.Length && column < _rows[line].Length && BOMB_MARK == _rows[line][column];
        }
    }

    [Header("구역")]
    [SerializeField, Min(1)] private int _columns = 6;
    [SerializeField, Min(1)] private int _rows = 4;
    [SerializeField, Min(1f)] private float _cellSize = 2.5f;
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField, Min(0f)] private float _tileGap = 0.15f;
    [SerializeField, Min(0.01f)] private float _tileEdgeWidth = 0.08f;
    [SerializeField] private Color _tileEdgeColor = new Color(0.62f, 0.48f, 0.2f, 0.5f);
    [SerializeField, Min(0.01f)] private float _boundaryWidth = 0.3f;
    [SerializeField] private Color _boundaryColor = new Color(0.55f, 0.25f, 0.85f, 1f);

    [Header("패턴")]
    [SerializeField] private Wave[] _waves;
    [SerializeField, Min(1)] private int _rounds = 2;
    [SerializeField, Min(0f)] private float _startDelay = 1.2f;
    [SerializeField, Min(0.05f)] private float _warnDuration = 0.8f;
    [SerializeField, Min(0f)] private float _restDuration = 0.2f;
    // 판이 넘어갈 때마다 예고와 쉬는 시간에 곱한다.
    [SerializeField, Range(0.1f, 1f)] private float _roundSpeedScale = 0.75f;

    [Header("터짐")]
    [SerializeField] private Color _warnColor = new Color(0.55f, 0.25f, 0.85f, 0.5f);
    [SerializeField] private Color _blastColor = new Color(0.9f, 0.8f, 1f, 0.85f);
    [SerializeField, Min(0f)] private float _blastDuration = 0.12f;
    // 소리와 흔들림은 폭발마다 한 번만 낸다.
    [SerializeField] private ImpactSettings _waveImpact = new();
    [SerializeField] private ImpactSettings _tileImpact = new();

    [Header("결과")]
    [SerializeField] private ImpactSettings _successImpact = new();

    [Header("카메라")]
    // 구역 아래쪽이 하단 슬롯에 가리지 않을 만큼 둔다.
    [SerializeField, Min(0f)] private float _cameraPadding = 2.2f;
    [SerializeField, Min(0f)] private float _cameraBlendDuration = 0.5f;

    public override void Prewarm()
    {
        if (null != _tilePrefab)
        {
            GameManager.Pool.Prewarm(_tilePrefab, (_columns * _rows + 1) * GimmickFrame.SPRITE_COUNT);
        }
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        AttackTarget target = context.Target;
        UnitMover mover = target.Unit.Mover;

        if (null == _tilePrefab || null == _waves || 0 == _waves.Length || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            Debug.LogError($"바닥 경보 : {name}에 칸 그림이나 패턴이 없거나 플레이어 이동 범위를 알 수 없습니다.");
            return true;
        }

        Rect field = Geometry2D.FitRect(area, target.FollowTransform.position, new Vector2(_columns, _rows) * _cellSize);
        Vector2 cell = new Vector2(field.width / _columns, field.height / _rows);
        Vector2 bodyExtents = target.Unit.GetBodyExtents();
        GimmickFrame boundary = new GimmickFrame(_tilePrefab, field.size, _boundaryWidth, _boundaryColor, _warnColor);
        List<GimmickFrame> tiles = ListPool<GimmickFrame>.Get();

        try
        {
            GameManager.CameraRig.ShowArea(new Bounds(field.center, field.size), _cameraPadding, _cameraBlendDuration);
            // 몸이 경계선 밖으로 나가 보이지 않게 몸 크기만큼 안쪽으로 가둔다.
            mover.SetMoveArea(new Rect(field.position + bodyExtents, field.size - bodyExtents * 2f));

            boundary.Place(field.center);
            boundary.SetFill(0f);
            BuildTiles(field, cell, tiles);

            await UniTask.Delay(TimeSpan.FromSeconds(_startDelay), cancellationToken: token);

            bool isClean = true;
            float speedScale = 1f;

            for (int round = 0; round < _rounds && false == target.IsDefeated; round++)
            {
                for (int i = 0; i < _waves.Length && false == target.IsDefeated; i++)
                {
                    isClean &= await PlayWaveAsync(context, _waves[i], cell, tiles, speedScale, token);
                }

                speedScale *= _roundSpeedScale;
            }

            if (false == isClean || target.IsDefeated)
            {
                return false;
            }

            GameManager.Feedback.PlayImpact(_successImpact, target.GetAimPoint(Vector3.zero));

            return true;
        }
        finally
        {
            GameManager.CameraRig.EndShowArea(_cameraBlendDuration);
            mover.SetMoveArea(area);
            boundary.Despawn();

            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].Despawn();
            }

            ListPool<GimmickFrame>.Release(tiles);
        }
    }

    // 칸 번호는 왼쪽 아래부터 가로로 센다.
    private void BuildTiles(Rect field, Vector2 cell, List<GimmickFrame> tiles)
    {
        Vector2 size = cell - new Vector2(_tileGap, _tileGap);

        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                GimmickFrame tile = new GimmickFrame(_tilePrefab, size, _tileEdgeWidth, _tileEdgeColor, _warnColor);

                tile.Place(new Vector3(field.xMin + cell.x * (column + 0.5f), field.yMin + cell.y * (row + 0.5f), 0f));
                tile.SetFill(0f);
                tiles.Add(tile);
            }
        }
    }

    // 맞지 않았으면 true다.
    private async UniTask<bool> PlayWaveAsync(AttackContext context, Wave wave, Vector2 cell, List<GimmickFrame> tiles, float speedScale, CancellationToken token)
    {
        await WarnAsync(wave, tiles, _warnDuration * speedScale, token);

        if (context.Target.IsDefeated)
        {
            return false;
        }

        // 대시로는 피할 수 있다.
        bool isHit = Detonate(context, wave, cell, tiles) && false == context.Target.TryDodge();

        if (isHit)
        {
            PunishTarget(context);
        }

        await UniTask.Delay(TimeSpan.FromSeconds(_blastDuration), cancellationToken: token);

        SetBombFill(wave, tiles, 0f);

        await UniTask.Delay(TimeSpan.FromSeconds(_restDuration * speedScale), cancellationToken: token);

        return false == isHit;
    }

    // 터질 칸이 아래부터 차올라, 다 차는 순간 터진다.
    private async UniTask WarnAsync(Wave wave, List<GimmickFrame> tiles, float duration, CancellationToken token)
    {
        SetBombColor(wave, tiles, _warnColor);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            SetBombFill(wave, tiles, elapsed / duration);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    // 터뜨리고, 플레이어가 터진 칸에 닿았으면 true를 돌려준다. 칸 사이 틈에 서도 피할 수 없게 그림이 아니라 칸 크기로 본다.
    private bool Detonate(AttackContext context, Wave wave, Vector2 cell, List<GimmickFrame> tiles)
    {
        bool isHit = false;

        SetBombColor(wave, tiles, _blastColor);
        SetBombFill(wave, tiles, 1f);

        GameManager.Feedback.PlayImpact(_waveImpact, context.Target.GetAimPoint(Vector3.zero));

        for (int i = 0; i < tiles.Count; i++)
        {
            if (false == IsBomb(wave, i))
            {
                continue;
            }

            GameManager.Feedback.PlayImpact(_tileImpact, tiles[i].Center);
            isHit |= context.Target.IsTouchingRect(tiles[i].Center, cell);
        }

        return isHit;
    }

    private void SetBombColor(Wave wave, List<GimmickFrame> tiles, Color color)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (IsBomb(wave, i))
            {
                tiles[i].SetFillColor(color);
            }
        }
    }

    private void SetBombFill(Wave wave, List<GimmickFrame> tiles, float progress)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (IsBomb(wave, i))
            {
                tiles[i].SetFill(progress);
            }
        }
    }

    private bool IsBomb(Wave wave, int tileIndex)
    {
        return wave.IsBomb(tileIndex / _columns, tileIndex % _columns, _rows);
    }
}
