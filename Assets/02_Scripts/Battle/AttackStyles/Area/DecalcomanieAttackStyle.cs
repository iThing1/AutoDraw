using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

// 플레이어 옆에 세로로 접는 선을 긋고, 플레이어 반대편에 먹 방울을 떨어뜨린 뒤 종이를 접어 같은 모양을 플레이어 쪽에 찍는다.
// 반대편에 떨어진 모양을 보고 비어 있던 자리의 거울 자리로 피한다.
[CreateAssetMenu(fileName = "DecalcomanieAttackStyle", menuName = "Scriptable Objects/AttackStyle/Decalcomanie")]
public class DecalcomanieAttackStyle : AttackStyle
{
    private const float VERTICAL_ANGLE = 90f;

    // 되감기용 기록. 접는 선 가운데와 처음 떨어뜨린 방울 자리를 쥐고 있다.
    private sealed class Record : RewindRecord
    {
        private readonly DecalcomanieAttackStyle _style;
        private readonly Vector3 _foldCenter;
        private readonly List<Vector3> _drops;

        public Record(DecalcomanieAttackStyle style, Vector3 foldCenter, List<Vector3> drops)
        {
            _style = style;
            _foldCenter = foldCenter;
            _drops = drops;
        }

        public override UniTask ReplayAsync(AttackContext context, InkTint tint, float gaugeGain, CancellationToken token)
        {
            return _style.RewindAsync(context, _foldCenter, _drops, tint, gaugeGain, token);
        }
    }

    [Header("접는 선")]
    // 목표에서 옆으로 이만큼 떨어진 곳에 세로로 긋는다. 왼쪽·오른쪽은 무작위다.
    [SerializeField] private FloatRange _foldOffset = new FloatRange(2f, 4f);
    [SerializeField, Min(1f)] private float _foldLength = 40f;
    [SerializeField, Min(0.01f)] private float _foldWidth = 0.25f;

    [Header("먹 방울")]
    [SerializeField, Min(1)] private int _dropCount = 7;
    [SerializeField, Min(0f)] private float _dropRadius = 1.6f;
    // 접는 선에서 바깥쪽으로 흩뿌리는 거리. 최소는 방울이 선에 걸쳐 거울 자리와 겹치지 않게 둔다.
    [SerializeField] private FloatRange _spreadDistance = new FloatRange(0.8f, 8f);
    // 목표 높이를 가운데 두고 위아래로 흩뿌리는 폭.
    [SerializeField, Min(0f)] private float _spreadHeight = 12f;

    [Header("예고")]
    [SerializeField] private GameObject _dropTelegraphPrefab;
    [SerializeField] private GameObject _foldTelegraphPrefab;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.35f);
    [SerializeField, Min(0f)] private float _dropWarnDuration = 0.6f;
    [SerializeField, Min(0f)] private float _foldDuration = 0.6f;
    // 접은 뒤 거울 자리에 잠깐 비치는 윤곽. 짧게 둬서 앞에서 본 모양을 기억해 피하게 한다.
    [SerializeField, Min(0f)] private float _stampWarnDuration = 0.3f;

    [Header("낙하")]
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();
    // 떨어진 자리에 남는 먹물 자국. 비워 두면 남기지 않는다.
    [SerializeField] private HitFeedbackPreset _groundSplat;

    public override void Prewarm()
    {
        if (null != _dropTelegraphPrefab)
        {
            GameManager.Pool.Prewarm(_dropTelegraphPrefab, _dropCount * 2);
        }

        if (null != _foldTelegraphPrefab)
        {
            GameManager.Pool.Prewarm(_foldTelegraphPrefab, 2);
        }
    }

    public override UniTask FireAsync(AttackContext context, float gaugeGain, CancellationToken token)
    {
        Vector3 target = context.Target.GetAimPoint(Vector3.zero);
        float side = 0.5f > Random.value ? -1f : 1f;
        float foldX = target.x + side * _foldOffset.PickRandom();

        List<Vector3> points = new List<Vector3>(_dropCount);

        for (int i = 0; i < _dropCount; i++)
        {
            float x = foldX + side * _spreadDistance.PickRandom();
            float y = target.y + Random.Range(-0.5f, 0.5f) * _spreadHeight;

            points.Add(new Vector3(x, y, target.z));
        }

        Vector3 foldCenter = new Vector3(foldX, target.y, target.z);

        context.RewindLog.Add(new Record(this, foldCenter, points));

        return PlayAsync(context, foldCenter, points, default, gaugeGain, token);
    }

    // 되감으면 접어서 찍혔던 자리에 먼저 떨어지고, 종이를 편 뒤 처음 방울 자리에 찍힌다.
    private async UniTask RewindAsync(AttackContext context, Vector3 foldCenter, List<Vector3> drops, InkTint tint, float gaugeGain,
        CancellationToken token)
    {
        List<Vector3> mirrored = ListPool<Vector3>.Get();

        try
        {
            Mirror(foldCenter.x, drops, mirrored);

            await PlayAsync(context, foldCenter, mirrored, tint, gaugeGain, token);
        }
        finally
        {
            ListPool<Vector3>.Release(mirrored);
        }
    }

    private async UniTask PlayAsync(AttackContext context, Vector3 foldCenter, List<Vector3> drops, InkTint tint, float gaugeGain,
        CancellationToken token)
    {
        await DropAllAsync(context, drops, _dropWarnDuration, tint, gaugeGain, token);

        await Telegraph.PlayLineAsync(_foldTelegraphPrefab, foldCenter, VERTICAL_ANGLE, _foldLength, _foldWidth, tint.Apply(_telegraphColor), _foldDuration,
            token);

        List<Vector3> mirrored = ListPool<Vector3>.Get();

        try
        {
            Mirror(foldCenter.x, drops, mirrored);

            await DropAllAsync(context, mirrored, _stampWarnDuration, tint, gaugeGain, token);
        }
        finally
        {
            ListPool<Vector3>.Release(mirrored);
        }
    }

    // 넘긴 목록은 기록에도 쓰이므로 고치지 않고 mirrored에 담는다.
    private static void Mirror(float foldX, List<Vector3> points, List<Vector3> mirrored)
    {
        for (int i = 0; i < points.Count; i++)
        {
            mirrored.Add(new Vector3(2f * foldX - points[i].x, points[i].y, points[i].z));
        }
    }

    // 한 묶음은 같은 순간에 떨어져, 방울이 겹쳐도 한 번만 맞고 막기 한 번으로 막는다.
    // 흔들림·소리도 묶음 가운데에서 한 번만 내, 방울 수만큼 겹쳐 터지지 않게 한다.
    private async UniTask DropAllAsync(AttackContext context, List<Vector3> points, float warnDuration, InkTint tint, float gaugeGain,
        CancellationToken token)
    {
        Vector3 center = Vector3.zero;

        for (int i = 0; i < points.Count; i++)
        {
            center += points[i];
        }

        await Telegraph.PlayCirclesAsync(_dropTelegraphPrefab, points, _dropRadius, tint.Apply(_telegraphColor), warnDuration, token);

        ImpactPlayer.Play(_impact, center / points.Count, tint.Apply(_impact.EffectColor));

        if (context.Target.IsInAnyBlast(points, _dropRadius))
        {
            context.Damage.Strike(_hit, context.Target.GetAimPoint(Vector3.zero), gaugeGain);
        }

        if (null == _groundSplat || null == context.PaintCanvas)
        {
            return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            context.PaintCanvas.LeaveSplats(points[i], context.Damage.PickColor(), true, _groundSplat);
        }
    }
}
