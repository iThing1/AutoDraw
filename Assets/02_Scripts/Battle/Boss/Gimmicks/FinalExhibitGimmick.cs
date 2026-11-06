using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// 마지막 전시. 액자가 단계마다 좁아지고 바깥 어둠은 대시로만 피할 수 있는 피해를 준다.
// 마지막에 비상구가 열리고 액자가 닫힌다. 닫히기 전에 비상구에 닿으면 그로기, 못 닿으면 큰 피해다.
[CreateAssetMenu(fileName = "FinalExhibitGimmick", menuName = "Scriptable Objects/Boss/Gimmick/FinalExhibit")]
public class FinalExhibitGimmick : PunishingBossGimmick
{
    private const int DARK_SPRITE_COUNT = 4;

    // 액자 테두리와 어둠. 어둠은 액자를 뺀 바깥을 네 조각으로 덮는다.
    private sealed class ClosingRoom
    {
        private readonly GimmickFrame _frame;
        private readonly SpriteRenderer[] _dark = new SpriteRenderer[DARK_SPRITE_COUNT];
        private readonly Rect _outer;

        public ClosingRoom(GameObject prefab, Rect outer, float edgeWidth, Color edgeColor, Color darkColor)
        {
            _outer = outer;
            _frame = new GimmickFrame(prefab, outer.size, edgeWidth, edgeColor, Color.clear);

            for (int i = 0; i < DARK_SPRITE_COUNT; i++)
            {
                _dark[i] = GameManager.Pool.SpawnSprite(prefab, Vector3.zero, 0f, Vector2.zero, darkColor);
            }

            Fit(outer);
        }

        public Rect Current { get; private set; }
        public int DarkSortingOrder { get { return _dark[0].sortingOrder; } }

        public void Fit(Rect rect)
        {
            Current = rect;
            _frame.Fit(rect);

            PlaceDark(_dark[0], Rect.MinMaxRect(_outer.xMin, rect.yMax, _outer.xMax, _outer.yMax));
            PlaceDark(_dark[1], Rect.MinMaxRect(_outer.xMin, _outer.yMin, _outer.xMax, rect.yMin));
            PlaceDark(_dark[2], Rect.MinMaxRect(_outer.xMin, rect.yMin, rect.xMin, rect.yMax));
            PlaceDark(_dark[3], Rect.MinMaxRect(rect.xMax, rect.yMin, _outer.xMax, rect.yMax));
        }

        public void Despawn()
        {
            _frame.Despawn();

            for (int i = 0; i < DARK_SPRITE_COUNT; i++)
            {
                GameManager.Pool.DespawnSprite(_dark[i]);
            }
        }

        private static void PlaceDark(SpriteRenderer dark, Rect rect)
        {
            dark.transform.position = rect.center;
            dark.transform.localScale = new Vector3(rect.width, rect.height, 1f);
        }
    }

    [Header("액자")]
    [SerializeField] private GameObject _framePrefab;
    // 큰 것부터 적는다. 다음 액자는 지금 액자 안에서 무작위로 고른다.
    [SerializeField] private Vector2[] _stageSizes =
    {
        new Vector2(48f, 27f),
        new Vector2(34f, 19f),
        new Vector2(22f, 12f),
        new Vector2(12f, 7f),
    };
    [SerializeField, Min(0.01f)] private float _edgeWidth = 0.35f;
    [SerializeField] private Color _edgeColor = new Color(0.95f, 0.78f, 0.25f, 1f);
    [SerializeField] private Color _previewColor = new Color(0.85f, 0.1f, 0.1f, 0.7f);
    [SerializeField, Min(0f)] private float _warnDuration = 1f;
    [SerializeField, Min(0.05f)] private float _closeDuration = 2f;

    [Header("어둠")]
    [SerializeField] private Color _darkColor = new Color(0.05f, 0.02f, 0.03f, 0.8f);
    // 처음 액자와 어둠이 화면 밖에서 들어오도록 판보다 이만큼 넓게 잡는다.
    [SerializeField, Min(0f)] private float _outerMargin = 12f;
    [SerializeField] private HitSettings _outsideHit = new();
    [SerializeField] private ImpactSettings _outsideImpact = new();
    [SerializeField, Min(0.05f)] private float _outsideInterval = 0.5f;

    [Header("물감 방울")]
    [SerializeField, Min(0.1f)] private float _dropInterval = 1.5f;
    [SerializeField] private GroundDropSettings _drop = new();

    [Header("비상구")]
    [SerializeField] private Vector2 _exitSize = new Vector2(5f, 5f);
    // 액자 가장자리와 비상구 사이의 어둠 폭. 대시 거리(4.5)보다 좁아야 맞지 않고 건널 수 있다.
    [SerializeField, Min(0f)] private float _exitGap = 3f;
    [SerializeField] private Color _exitColor = new Color(0.3f, 0.95f, 0.45f, 0.75f);
    [SerializeField, Min(0.05f)] private float _collapseDuration = 2.5f;

    [Header("결과")]
    [SerializeField] private ImpactSettings _successImpact = new();
    [SerializeField] private ImpactSettings _failImpact = new();

    [Header("카메라")]
    [SerializeField, Min(0f)] private float _cameraPadding = 1f;
    [SerializeField, Min(0f)] private float _cameraBlendDuration = 0.5f;

    public override void Prewarm()
    {
        if (null != _framePrefab)
        {
            GameManager.Pool.Prewarm(_framePrefab, GimmickFrame.SPRITE_COUNT * 2 + DARK_SPRITE_COUNT + 1);
        }

        GroundDrop.Prewarm(GameManager.Pool, _drop, 3);
    }

    public override async UniTask<bool> PlayAsync(AttackContext context, CancellationToken token)
    {
        UnitMover mover = context.Target.Unit.Mover;

        if (null == _framePrefab || null == _stageSizes || 0 == _stageSizes.Length || null == mover || false == mover.TryGetMoveArea(out Rect area))
        {
            Debug.LogError($"마지막 전시 : {name}에 액자 그림이나 단계가 없거나 플레이어 이동 범위를 알 수 없습니다.");
            return true;
        }

        Rect outer = new Rect(area.position - Vector2.one * _outerMargin, area.size + Vector2.one * (_outerMargin * 2f));
        ClosingRoom room = new ClosingRoom(_framePrefab, outer, _edgeWidth, _edgeColor, _darkColor);
        GimmickFrame preview = new GimmickFrame(_framePrefab, area.size, _edgeWidth, Color.clear, Color.clear);

        using (CancellationTokenSource hazardSource = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            try
            {
                GameManager.CameraRig.ShowArea(new Bounds(area.center, area.size), _cameraPadding, _cameraBlendDuration);

                StrikeOutsideAsync(context, room, hazardSource.Token).Forget();
                DropAsync(context, hazardSource.Token).Forget();

                Rect last = await NarrowAsync(context, room, preview, area, token);

                preview.SetEdgeColor(Color.clear);

                bool isEscaped = await EscapeAsync(context, room, area, last, token);

                hazardSource.Cancel();

                if (context.Target.IsDefeated)
                {
                    return false;
                }

                Vector3 hitPoint = context.Target.GetAimPoint(Vector3.zero);

                if (isEscaped)
                {
                    GameManager.Feedback.PlayImpact(_successImpact, hitPoint);
                    return true;
                }

                GameManager.Feedback.PlayImpact(_failImpact, hitPoint);
                PunishTarget(context);

                return false;
            }
            finally
            {
                GameManager.CameraRig.EndShowArea(_cameraBlendDuration);
                hazardSource.Cancel();
                room.Despawn();
                preview.Despawn();
            }
        }
    }

    private async UniTask<Rect> NarrowAsync(AttackContext context, ClosingRoom room, GimmickFrame preview, Rect area, CancellationToken token)
    {
        Rect bounds = area;

        preview.SetEdgeColor(_previewColor);

        for (int i = 0; i < _stageSizes.Length && false == context.Target.IsDefeated; i++)
        {
            bounds = PickStage(bounds, _stageSizes[i]);
            preview.Fit(bounds);

            await UniTask.Delay(TimeSpan.FromSeconds(_warnDuration), cancellationToken: token);

            Rect from = room.Current;
            float elapsed = 0f;

            while (elapsed < _closeDuration)
            {
                room.Fit(LerpRect(from, bounds, elapsed / _closeDuration));

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }

            room.Fit(bounds);
        }

        return bounds;
    }

    private async UniTask<bool> EscapeAsync(AttackContext context, ClosingRoom room, Rect area, Rect last, CancellationToken token)
    {
        AttackTarget target = context.Target;
        Rect exit = PickExit(area, last);
        Rect closed = new Rect(last.center, Vector2.zero);
        SpriteRenderer exitSprite = GameManager.Pool.SpawnSprite(_framePrefab, exit.center, 0f, exit.size, _exitColor);
        int exitOrder = exitSprite.sortingOrder;
        float elapsed = 0f;

        // 비상구는 어둠 위에 보여야 한다. 풀에 돌려줄 때 원래대로 되돌린다.
        exitSprite.sortingOrder = room.DarkSortingOrder + 1;

        try
        {
            while (elapsed < _collapseDuration && false == target.IsDefeated)
            {
                if (target.IsTouchingRect(exit.center, exit.size))
                {
                    return true;
                }

                room.Fit(LerpRect(last, closed, elapsed / _collapseDuration));

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.deltaTime;
            }

            return false;
        }
        finally
        {
            exitSprite.sortingOrder = exitOrder;
            GameManager.Pool.DespawnSprite(exitSprite);
        }
    }

    private async UniTask StrikeOutsideAsync(AttackContext context, ClosingRoom room, CancellationToken token)
    {
        AttackTarget target = context.Target;

        while (false == target.IsDefeated)
        {
            Rect current = room.Current;

            if (target.IsInsideRect(current.center, current.size))
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                continue;
            }

            Vector3 hitPoint = target.GetAimPoint(Vector3.zero);

            GameManager.Feedback.PlayImpact(_outsideImpact, hitPoint);
            context.Damage.StrikeThroughGuard(_outsideHit, hitPoint);

            await UniTask.Delay(TimeSpan.FromSeconds(_outsideInterval), cancellationToken: token);
        }
    }

    private async UniTask DropAsync(AttackContext context, CancellationToken token)
    {
        AttackTarget target = context.Target;

        while (false == target.IsDefeated)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_dropInterval), cancellationToken: token);

            GroundDrop.PlayAsync(GameManager.Pool, GameManager.Feedback, context, _drop, target.GetAimPoint(Vector3.zero), token).Forget();
        }
    }

    private static Rect PickStage(Rect bounds, Vector2 size)
    {
        size = Vector2.Min(size, bounds.size);

        float x = UnityEngine.Random.Range(bounds.xMin, bounds.xMax - size.x);
        float y = UnityEngine.Random.Range(bounds.yMin, bounds.yMax - size.y);

        return new Rect(x, y, size.x, size.y);
    }

    // 판 가운데 쪽이 항상 더 넓으므로, 그쪽 변 너머에 둬야 판 밖으로 나가지 않는다.
    private Rect PickExit(Rect area, Rect last)
    {
        Vector2 center = last.center;

        if (0.5f > UnityEngine.Random.value)
        {
            float side = last.center.x < area.center.x ? 1f : -1f;

            center.x += side * (last.width * 0.5f + _exitGap + _exitSize.x * 0.5f);
        }
        else
        {
            float side = last.center.y < area.center.y ? 1f : -1f;

            center.y += side * (last.height * 0.5f + _exitGap + _exitSize.y * 0.5f);
        }

        return Geometry2D.FitRect(area, center, _exitSize);
    }

    private static Rect LerpRect(Rect from, Rect to, float t)
    {
        return Rect.MinMaxRect(
            Mathf.Lerp(from.xMin, to.xMin, t),
            Mathf.Lerp(from.yMin, to.yMin, t),
            Mathf.Lerp(from.xMax, to.xMax, t),
            Mathf.Lerp(from.yMax, to.yMax, t));
    }
}
