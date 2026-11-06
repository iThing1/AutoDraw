using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 폐관 시간. 플레이어 둘레를 천장 조명 칸으로 나누고, 차례마다 정해진 무늬대로 조명을 깜빡이다 꺼서 어두워진 칸을 친다.
// 비상구 한 칸만 남기고 모두 끄는 정전 차례는 빨간 예고 규칙대로 막을 수 없다.
[CreateAssetMenu(fileName = "ClosingTimeAttackStyle", menuName = "Scriptable Objects/AttackStyle/ClosingTime")]
public class ClosingTimeAttackStyle : AttackStyle
{
    private enum LightsOut
    {
        Checker,
        CheckerFlipped,
        AllButExit,
    }

    [Header("구역")]
    [SerializeField] private Vector2 _fieldSize = new Vector2(20f, 12f);
    [SerializeField, Min(1)] private int _columns = 4;
    [SerializeField, Min(1)] private int _rows = 3;
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField, Min(0f)] private float _tileGap = 0.3f;

    [Header("조명")]
    [SerializeField] private LightsOut[] _rounds = { LightsOut.Checker, LightsOut.CheckerFlipped, LightsOut.AllButExit };
    [SerializeField] private Color _litColor = new Color(1f, 0.92f, 0.6f, 0.14f);
    [SerializeField] private Color _flickerColor = new Color(0.05f, 0.05f, 0.08f, 0.4f);
    [SerializeField] private Color _blackoutFlickerColor = new Color(0.85f, 0.1f, 0.1f, 0.4f);
    [SerializeField] private Color _darkColor = new Color(0.03f, 0.03f, 0.05f, 0.75f);
    [SerializeField] private Color _exitColor = new Color(0.3f, 0.95f, 0.45f, 0.4f);
    [SerializeField, Min(0.05f)] private float _flickerDuration = 0.8f;
    // 비상구까지 달려갈 수 있게 정전 차례는 더 길게 깜빡인다.
    [SerializeField, Min(0.05f)] private float _blackoutFlickerDuration = 1.4f;
    [SerializeField, Min(0.02f)] private float _flickerPeriod = 0.12f;
    // 비상구는 플레이어가 선 칸에서 이 칸 수 안쪽(자기 칸 제외)에서 고른다.
    [SerializeField, Min(1)] private int _exitReach = 1;
    [SerializeField, Min(0f)] private float _darkHold = 0.35f;
    [SerializeField, Min(0f)] private float _roundGap = 0.2f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;

    [Header("터짐")]
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();
    [SerializeField] private HitSettings _blackoutHit = new();
    [SerializeField] private ImpactSettings _blackoutImpact = new();

    public override void Prewarm()
    {
        if (null != _tilePrefab)
        {
            GameManager.Pool.Prewarm(_tilePrefab, _columns * _rows);
        }
    }

    public override async UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null == _tilePrefab || null == _rounds || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            return;
        }

        Rect field = Geometry2D.FitRect(area, context.Target.FollowTransform.position, _fieldSize);
        List<SpriteRenderer> tiles = ListPool<SpriteRenderer>.Get();
        List<bool> dark = ListPool<bool>.Get();

        try
        {
            BuildTiles(field, tiles, dark);

            for (int i = 0; i < _rounds.Length; i++)
            {
                bool isBlackout = LightsOut.AllButExit == _rounds[i];
                int playerCell = Geometry2D.GetCellIndex(field, _columns, _rows, context.Target.FollowTransform.position);
                int exit = isBlackout ? PickExit(playerCell) : -1;

                MarkDark(_rounds[i], exit, dark);

                if (0 <= exit)
                {
                    tiles[exit].color = _exitColor;
                }

                Color flickerColor = isBlackout ? _blackoutFlickerColor : _flickerColor;
                float flickerDuration = isBlackout ? _blackoutFlickerDuration : _flickerDuration;

                await FlickerAsync(tiles, dark, flickerColor, flickerDuration, token);

                SetDarkColors(tiles, dark);
                Strike(context, field, dark, isBlackout, gaugeGain);

                await UniTask.Delay(TimeSpan.FromSeconds(_darkHold), cancellationToken: token);

                SetAllColors(tiles, _litColor);

                await UniTask.Delay(TimeSpan.FromSeconds(_roundGap), cancellationToken: token);
            }

            await FadeOutAsync(tiles, token);
        }
        finally
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                GameManager.Pool.DespawnSprite(tiles[i]);
            }

            ListPool<SpriteRenderer>.Release(tiles);
            ListPool<bool>.Release(dark);
        }
    }

    // 칸 번호는 왼쪽 아래부터 가로로 센다. Geometry2D.GetCellIndex와 같은 순서다.
    private void BuildTiles(Rect field, List<SpriteRenderer> tiles, List<bool> dark)
    {
        Vector2 cell = new Vector2(field.width / _columns, field.height / _rows);
        Vector2 size = cell - new Vector2(_tileGap, _tileGap);

        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                Vector3 center = new Vector3(field.xMin + cell.x * (column + 0.5f), field.yMin + cell.y * (row + 0.5f), 0f);

                tiles.Add(GameManager.Pool.SpawnSprite(_tilePrefab, center, 0f, size, _litColor));
                dark.Add(false);
            }
        }
    }

    private int PickExit(int playerCell)
    {
        int playerColumn = playerCell % _columns;
        int playerRow = playerCell / _columns;
        int minColumn = Mathf.Max(0, playerColumn - _exitReach);
        int maxColumn = Mathf.Min(_columns - 1, playerColumn + _exitReach);
        int minRow = Mathf.Max(0, playerRow - _exitReach);
        int maxRow = Mathf.Min(_rows - 1, playerRow + _exitReach);
        int candidates = (maxColumn - minColumn + 1) * (maxRow - minRow + 1) - 1;

        // 판이 한 칸뿐이면 자기 칸 말고는 고를 곳이 없다.
        if (0 >= candidates)
        {
            return playerCell;
        }

        int pick = UnityEngine.Random.Range(0, candidates);

        for (int row = minRow; row <= maxRow; row++)
        {
            for (int column = minColumn; column <= maxColumn; column++)
            {
                int cell = row * _columns + column;

                if (cell == playerCell)
                {
                    continue;
                }

                if (0 == pick)
                {
                    return cell;
                }

                pick--;
            }
        }

        return playerCell;
    }

    private void MarkDark(LightsOut pattern, int exit, List<bool> dark)
    {
        for (int i = 0; i < dark.Count; i++)
        {
            bool isEven = 0 == (i % _columns + i / _columns) % 2;

            switch (pattern)
            {
                case LightsOut.Checker:
                    dark[i] = isEven;
                    break;
                case LightsOut.CheckerFlipped:
                    dark[i] = false == isEven;
                    break;
                default:
                    dark[i] = i != exit;
                    break;
            }
        }
    }

    private async UniTask FlickerAsync(List<SpriteRenderer> tiles, List<bool> dark, Color flickerColor, float duration, CancellationToken token)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            bool isOff = 0 == Mathf.FloorToInt(elapsed / _flickerPeriod) % 2;

            for (int i = 0; i < tiles.Count; i++)
            {
                if (dark[i])
                {
                    tiles[i].color = isOff ? flickerColor : _litColor;
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsed += Time.deltaTime;
        }
    }

    private void SetDarkColors(List<SpriteRenderer> tiles, List<bool> dark)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (dark[i])
            {
                tiles[i].color = _darkColor;
            }
        }
    }

    private static void SetAllColors(List<SpriteRenderer> tiles, Color color)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].color = color;
        }
    }

    private void Strike(AttackContext context, Rect field, List<bool> dark, bool isBlackout, float gaugeGain)
    {
        AttackTarget target = context.Target;
        Vector3 hitPoint = target.GetAimPoint(Vector3.zero);

        GameManager.Feedback.PlayImpact(isBlackout ? _blackoutImpact : _impact, isBlackout ? hitPoint : (Vector3)field.center);

        if (target.IsDefeated || false == IsTouchingDark(target, field, dark))
        {
            return;
        }

        if (isBlackout)
        {
            context.Damage.StrikeThroughGuard(_blackoutHit, hitPoint);
        }
        else
        {
            context.Damage.Strike(_hit, hitPoint, gaugeGain);
        }
    }

    // 칸 사이 틈에 서도 피할 수 없게, 그림이 아니라 판을 나눈 칸 크기로 본다.
    private bool IsTouchingDark(AttackTarget target, Rect field, List<bool> dark)
    {
        Vector2 cell = new Vector2(field.width / _columns, field.height / _rows);

        for (int i = 0; i < dark.Count; i++)
        {
            Vector3 center = new Vector3(field.xMin + cell.x * (i % _columns + 0.5f), field.yMin + cell.y * (i / _columns + 0.5f), 0f);

            if (dark[i] && target.IsTouchingRect(center, cell))
            {
                return true;
            }
        }

        return false;
    }

    private UniTask FadeOutAsync(List<SpriteRenderer> tiles, CancellationToken token)
    {
        Sequence fades = DOTween.Sequence();

        for (int i = 0; i < tiles.Count; i++)
        {
            fades.Join(tiles[i].DOFade(0f, _fadeDuration));
        }

        return fades.ToUniTaskKillOnCancel(token);
    }
}
