using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// 맞은 자리 주변에 물감 자국을 찍어 전장을 점점 칠한다. 자국은 텍스처 두 장에 구워 개수 상한 없이 남는다.
// 아래 장에는 플레이어 물감만, 위 장에는 잉크와 물감을 찍힌 순서대로 그려 나중에 칠한 물감이 잉크를 덮어 보인다.
// 위 장만 흐리거나 다시 그리면 물감은 남기고 잉크만 지울 수 있다. 판정은 텍스처를 읽지 않고 기록(PaintMark)으로 한다.
public class PaintCanvas : MonoBehaviour
{
    private const string BAKE_SHADER = "Sprites/Default";
    private const int PAINT_LAYER_ORDER = 0;
    private const int TOP_LAYER_ORDER = 1;
    // 커지는 중이거나 잠깐 다시 띄운 자국은 구운 두 장보다 위에 보인다.
    private const int LIVE_SPLAT_ORDER = 2;

    private static readonly System.Predicate<PaintMark> IS_REMOVED = IsMarkRemoved;

    [SerializeField] private GameObject _splatPrefab;

    [Header("굽기")]
    // 바닥 1단위를 몇 픽셀로 구울지. 96x54 바닥이면 20일 때 1920x1080 두 장이다.
    [SerializeField, Min(1)] private int _pixelsPerUnit = 20;

    [Header("자국")]
    [SerializeField, Min(0f)] private float _appearDuration = 0.12f;
    [SerializeField] private Ease _appearEase = Ease.OutBack;
    // 리무버에 지워진 잉크가 갑자기 사라지지 않고 이 시간 동안 흐려지며 사라진다.
    [SerializeField, Min(0f)] private float _removeFadeDuration = 0.4f;

    // 찍힌 순서대로 둔다. 위 장을 다시 그릴 때 이 순서로 그려야 덮인 모양이 그대로 나온다.
    private readonly List<PaintMark> _marks = new();
    private readonly List<PaintMark> _inkMarks = new();
    // 커지는 연출 중이라 아직 굽지 않은 자국. 찍은 순서대로라 앞에서부터 다 커진 것을 굽는다.
    private readonly List<AppearingSplat> _appearing = new();
    private readonly Dictionary<Sprite, Mesh> _spriteMeshes = new();
    private readonly List<Mesh> _stripeMeshes = new();
    private Material _bakeMaterial;
    private Mesh _quad;
    private PaintBaker _paintLayer;
    private PaintBaker _topLayer;
    private int _sortingLayerId;

    private readonly struct AppearingSplat
    {
        public readonly PaintSplat Splat;
        public readonly PaintMark Mark;
        public readonly float BakeTime;

        public AppearingSplat(PaintSplat splat, PaintMark mark, float bakeTime)
        {
            Splat = splat;
            Mark = mark;
            BakeTime = bakeTime;
        }
    }

    public IReadOnlyList<PaintMark> InkMarks { get { return _inkMarks; } }

    public bool HasRequiredReferences()
    {
        if (null == _splatPrefab)
        {
            Debug.LogError("물감 캔버스 : 자국 프리팹이 지정되지 않았습니다.");
            return false;
        }

        return true;
    }

    // 전투를 시작하는 쪽이 스테이지 바닥 영역을 넘겨 한 번 부른다. 그 전에 찍힌 자국은 굽지 못하고 기록만 남는다.
    public void Init(Bounds area)
    {
        Shader shader = Shader.Find(BAKE_SHADER);

        if (null == shader)
        {
            Debug.LogError($"물감 캔버스 : {BAKE_SHADER} 셰이더를 찾지 못해 자국을 굽지 않습니다.");
            return;
        }

        _sortingLayerId = _splatPrefab.GetComponent<SpriteRenderer>().sortingLayerID;
        _bakeMaterial = new Material(shader);
        _quad = CreateQuad();
        _paintLayer = new PaintBaker("PaintLayer", transform, area, _pixelsPerUnit, _bakeMaterial, _quad, _sortingLayerId, PAINT_LAYER_ORDER);
        _topLayer = new PaintBaker("TopLayer", transform, area, _pixelsPerUnit, _bakeMaterial, _quad, _sortingLayerId, TOP_LAYER_ORDER);
    }

    private void LateUpdate()
    {
        // 굽지 못하는 상태(Init 실패)면 커진 자국을 그대로 두어 적어도 보이게 한다.
        if (null == _paintLayer)
        {
            return;
        }

        float now = Time.unscaledTime;

        while (0 < _appearing.Count && _appearing[0].BakeTime <= now)
        {
            AppearingSplat appearing = _appearing[0];
            _appearing.RemoveAt(0);
            appearing.Splat.Despawn();
            Bake(appearing.Mark);
        }

        _paintLayer.Flush();
        _topLayer.Flush();
    }

    private void OnDestroy()
    {
        if (null != _paintLayer)
        {
            _paintLayer.Release();
            _topLayer.Release();
            Destroy(_bakeMaterial);
            Destroy(_quad);
        }

        foreach (Mesh mesh in _spriteMeshes.Values)
        {
            Destroy(mesh);
        }

        for (int i = 0; i < _stripeMeshes.Count; i++)
        {
            Destroy(_stripeMeshes[i]);
        }
    }

    public void LeaveSplats(Vector3 center, Color color, bool isInk, HitFeedbackPreset preset)
    {
        SplatStyle style = preset.SplatStyle;

        if (null == style || false == style.HasSprites)
        {
            return;
        }

        int count = preset.SplatCount.PickRandom();

        for (int i = 0; i < count; i++)
        {
            Vector3 position = center + (Vector3)(Random.insideUnitCircle * preset.SplatSpread);
            float scale = preset.SplatScale.PickRandom();

            SpawnSplat(position, style, color, isInk, scale);
        }
    }

    // 연출이 새로 찍은 잉크 자국 자리를 써야 할 때(주변에 잉크가 없어 새로 찍는 경우 등) 찍은 자국을 돌려준다.
    public PaintMark LeaveInkSplat(Vector3 position, SplatStyle style, Color color, float scale)
    {
        return LeaveSplat(position, style, color, true, scale);
    }

    // 맞은 자리가 아닌 곳에 자국을 하나 찍는다. 기믹이 플레이어 쪽 물감(안전한 자리)을 미리 깔아 둘 때도 쓴다.
    public PaintMark LeaveSplat(Vector3 position, SplatStyle style, Color color, bool isInk, float scale)
    {
        if (null == style || false == style.HasSprites)
        {
            return null;
        }

        return SpawnSplat(position, style, color, isInk, scale);
    }

    // 롤러처럼 이어서 깔리는 띠를 시작한다. 굽지 못하는 상태면 null이고, 부르는 쪽은 띠 없이 굴러간다.
    public PaintStripe BeginStripe(Vector3 start, Vector3 direction, float width, Texture texture, float tileLength, Color color, bool isInk)
    {
        if (null == _bakeMaterial || null == texture)
        {
            return null;
        }

        Mesh mesh = new Mesh { name = "PaintStripe" };
        _stripeMeshes.Add(mesh);

        // 띠 메시는 월드 좌표라 보이는 오브젝트는 원점에 그대로 둔다.
        GameObject display = new GameObject("PaintStripe");
        display.transform.SetParent(transform, true);
        display.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer stripeRenderer = display.AddComponent<MeshRenderer>();
        stripeRenderer.sharedMaterial = _bakeMaterial;
        stripeRenderer.sortingLayerID = _sortingLayerId;
        stripeRenderer.sortingOrder = LIVE_SPLAT_ORDER;
        stripeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        stripeRenderer.receiveShadows = false;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        PaintBaker.FillBlock(block, texture, color);
        stripeRenderer.SetPropertyBlock(block);

        return new PaintStripe(this, display, mesh, texture, start, direction, width, tileLength, color, isInk);
    }

    // PaintStripe.Finish가 부른다. 깔린 길이가 없으면 남기지 않는다.
    public void FinishStripe(PaintStripe stripe)
    {
        if (0f >= stripe.Length)
        {
            return;
        }

        PaintMark mark = PaintMark.ForStripe(stripe.Mesh, stripe.Texture, stripe.Start, stripe.End, stripe.Width, stripe.Color, stripe.IsInk);

        AddMark(mark);
        Bake(mark);
    }

    // 잉크가 위 장에 물감과 함께 구워져 있어 위 장을 통째로 흐린다. 그 아래 장의 물감이 그대로 드러난다.
    // 한 장이라 자국마다 시차를 줄 수 없어, 지연은 가장 짧은 값 하나만 쓴다.
    public async UniTask FadeOutInkAsync(float duration, FloatRange startDelay, Ease ease, CancellationToken token)
    {
        for (int i = _appearing.Count - 1; i >= 0; i--)
        {
            if (_appearing[i].Mark.IsInk)
            {
                _appearing[i].Splat.FadeOutAndDespawn(duration);
                _appearing.RemoveAt(i);
            }
        }

        if (null != _topLayer)
        {
            await DOVirtual.Float(1f, 0f, duration, _topLayer.SetAlpha)
                .SetDelay(startDelay.Min)
                .SetEase(ease)
                .SetUpdate(true)
                .ToUniTaskKillOnCancel(token);
        }

        for (int i = 0; i < _inkMarks.Count; i++)
        {
            _inkMarks[i].MarkRemoved();
        }

        _marks.RemoveAll(IS_REMOVED);
        _inkMarks.Clear();
        RedrawTopLayer();

        if (null != _topLayer)
        {
            _topLayer.SetAlpha(1f);
        }
    }

    // 리무버처럼 한 자리의 잉크만 지운다. 위 장을 지운 잉크 없이 다시 그리고, 지운 자국은 잠깐 띄워 흐려지게 한다.
    public void RemoveInkAround(Vector3 center, float radius)
    {
        bool hasRemoved = false;

        for (int i = _inkMarks.Count - 1; i >= 0; i--)
        {
            PaintMark ink = _inkMarks[i];

            if (Vector2.Distance(center, ink.Position) > radius)
            {
                continue;
            }

            // 물감에 덮여 안 보이던 잉크는 흐려지는 모습을 띄우면 물감 위로 튀어 보이므로 그냥 지운다.
            bool wasVisible = IsInkExposed(ink);

            _inkMarks.RemoveAt(i);
            _marks.Remove(ink);
            ink.MarkRemoved();
            hasRemoved = true;

            if (false == TryStopAppearing(ink, _removeFadeDuration) && wasVisible && null != ink.Sprite)
            {
                SpawnLiveSplat(ink).FadeOutAndDespawn(_removeFadeDuration);
            }
        }

        if (hasRemoved)
        {
            RedrawTopLayer();
        }
    }

    // 잉크 웅덩이가 꿈틀하는 연출. 구운 그림은 움직일 수 없어 같은 모양을 위에 잠깐 띄워 흔든다.
    // 물감에 덮인 잉크는 띄우면 물감 위로 튀어 보이므로 흔들지 않는다.
    public void Twitch(PaintMark mark, float strength, float duration, bool isRealtime)
    {
        if (null == mark.Sprite || false == IsInkExposed(mark))
        {
            return;
        }

        SpawnLiveSplat(mark).PunchAndDespawn(Vector3.one * (mark.Scale * strength), duration, isRealtime);
    }

    // 플레이어 쪽 물감(잉크가 아닌 자국)이 이 자리에 칠해져 있는지.
    // 기믹 때 한 번씩만 불려, 자국 수만큼 도는 비용은 문제가 되지 않는다.
    public bool IsPaintedAt(Vector3 position, float radius)
    {
        for (int i = 0; i < _marks.Count; i++)
        {
            if (false == _marks[i].IsInk && _marks[i].Overlaps(position, radius))
            {
                return true;
            }
        }

        return false;
    }

    // 아직 캔버스에 남은 잉크이고, 그 뒤에 찍힌 플레이어 물감에 가운데가 덮이지 않았는지.
    public bool IsInkExposed(PaintMark ink)
    {
        if (null == ink || false == ink.IsInk || ink.IsRemoved)
        {
            return false;
        }

        Vector3 center = ink.Position;

        for (int i = _marks.IndexOf(ink) + 1; i < _marks.Count; i++)
        {
            if (false == _marks[i].IsInk && _marks[i].Overlaps(center, 0f))
            {
                return false;
            }
        }

        return true;
    }

    private PaintMark SpawnSplat(Vector3 position, SplatStyle style, Color color, bool isInk, float scale)
    {
        Sprite sprite = style.PickSprite();
        PaintMark mark = PaintMark.ForSplat(sprite, GetSpriteMesh(sprite), position, style.PickRotation(), scale, color, isInk);

        AddMark(mark);

        PaintSplat splat = SpawnLiveSplat(mark);
        Transform splatTransform = splat.transform;
        splatTransform.localScale = Vector3.zero;
        splatTransform.DOScale(scale, _appearDuration).SetEase(_appearEase).SetUpdate(true);

        _appearing.Add(new AppearingSplat(splat, mark, Time.unscaledTime + _appearDuration));

        return mark;
    }

    private void AddMark(PaintMark mark)
    {
        _marks.Add(mark);

        if (mark.IsInk)
        {
            _inkMarks.Add(mark);
        }
    }

    private PaintSplat SpawnLiveSplat(PaintMark mark)
    {
        GameObject instance = GameManager.Pool.Spawn(_splatPrefab, transform);
        instance.transform.SetPositionAndRotation(mark.Position, mark.Rotation);
        instance.transform.localScale = Vector3.one * mark.Scale;

        PaintSplat splat = instance.GetOrAddComponent<PaintSplat>();
        splat.Show(mark.Sprite, mark.Color, mark.IsInk, LIVE_SPLAT_ORDER);

        return splat;
    }

    // 아직 커지는 중인 자국이면 굽지 않고 흐려 보낸다.
    private bool TryStopAppearing(PaintMark mark, float fadeDuration)
    {
        for (int i = 0; i < _appearing.Count; i++)
        {
            if (mark == _appearing[i].Mark)
            {
                _appearing[i].Splat.FadeOutAndDespawn(fadeDuration);
                _appearing.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private void Bake(PaintMark mark)
    {
        if (mark.IsRemoved || null == _paintLayer)
        {
            return;
        }

        if (false == mark.IsInk)
        {
            _paintLayer.Draw(mark);
        }

        _topLayer.Draw(mark);
        mark.MarkBaked();
    }

    // 지운 잉크를 뺀 남은 자국을 찍힌 순서대로 다시 그린다. 아직 커지는 중인 자국은 다 커지면 따로 굽는다.
    private void RedrawTopLayer()
    {
        if (null == _topLayer)
        {
            return;
        }

        _topLayer.Clear();

        for (int i = 0; i < _marks.Count; i++)
        {
            if (_marks[i].IsBaked)
            {
                _topLayer.Draw(_marks[i]);
            }
        }
    }

    // 아틀라스 안 스프라이트 모양 그대로 그리도록 스프라이트의 꼭짓점과 UV로 메시를 만든다. 모양마다 한 번만 만든다.
    private Mesh GetSpriteMesh(Sprite sprite)
    {
        if (_spriteMeshes.TryGetValue(sprite, out Mesh mesh))
        {
            return mesh;
        }

        Vector2[] spriteVertices = sprite.vertices;
        ushort[] spriteTriangles = sprite.triangles;
        Vector3[] vertices = new Vector3[spriteVertices.Length];
        Color[] colors = new Color[spriteVertices.Length];
        int[] triangles = new int[spriteTriangles.Length];

        for (int i = 0; i < spriteVertices.Length; i++)
        {
            vertices[i] = spriteVertices[i];
            colors[i] = Color.white;
        }

        for (int i = 0; i < spriteTriangles.Length; i++)
        {
            triangles[i] = spriteTriangles[i];
        }

        mesh = new Mesh { name = sprite.name };
        mesh.vertices = vertices;
        mesh.uv = sprite.uv;
        mesh.colors = colors;
        mesh.triangles = triangles;

        _spriteMeshes.Add(sprite, mesh);

        return mesh;
    }

    private static bool IsMarkRemoved(PaintMark mark)
    {
        return mark.IsRemoved;
    }

    private static Mesh CreateQuad()
    {
        Mesh quad = new Mesh { name = "PaintLayerQuad" };
        quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
        quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };

        return quad;
    }
}
