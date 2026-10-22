using UnityEngine;
using UnityEngine.Rendering;

// 자국을 그려 넣는 텍스처 한 장과, 그것을 바닥에 보여 주는 사각형 하나.
// 한 프레임에 생긴 자국은 모아 두었다가 Flush에서 한 번에 그린다. 텍스처를 CPU로 읽지 않아 굽는 동안 멈추지 않는다.
public class PaintBaker
{
    private static readonly int MAIN_TEX_ID = Shader.PropertyToID("_MainTex");
    private static readonly int COLOR_ID = Shader.PropertyToID("_Color");
    // Sprites/Default가 곱하는 값. SpriteRenderer가 아니면 아무도 채우지 않아 직접 흰색을 넣는다.
    private static readonly int RENDERER_COLOR_ID = Shader.PropertyToID("_RendererColor");
    private static readonly int FLIP_ID = Shader.PropertyToID("_Flip");

    private readonly RenderTexture _texture;
    private readonly Material _material;
    private readonly CommandBuffer _commands;
    private readonly Matrix4x4 _projection;
    private readonly MeshRenderer _display;
    private readonly MaterialPropertyBlock _drawBlock = new();
    private readonly MaterialPropertyBlock _displayBlock = new();
    private bool _hasPending;

    // area는 텍스처가 덮는 월드 영역(스테이지 바닥)이다. quad는 -0.5~0.5 크기의 사각형 메시다.
    public PaintBaker(string name, Transform parent, Bounds area, int pixelsPerUnit, Material material, Mesh quad, int sortingLayerId, int sortingOrder)
    {
        int width = Mathf.Max(1, Mathf.CeilToInt(area.size.x * pixelsPerUnit));
        int height = Mathf.Max(1, Mathf.CeilToInt(area.size.y * pixelsPerUnit));

        _texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { name = name };
        _texture.Create();
        _material = material;
        _projection = Matrix4x4.Ortho(area.min.x, area.max.x, area.min.y, area.max.y, -100f, 100f);
        _commands = new CommandBuffer { name = name };

        GameObject display = new GameObject(name);
        display.transform.SetParent(parent, false);
        display.transform.SetPositionAndRotation(new Vector3(area.center.x, area.center.y, 0f), Quaternion.identity);
        display.transform.localScale = new Vector3(area.size.x, area.size.y, 1f);
        display.AddComponent<MeshFilter>().sharedMesh = quad;

        _display = display.AddComponent<MeshRenderer>();
        _display.sharedMaterial = material;
        _display.sortingLayerID = sortingLayerId;
        _display.sortingOrder = sortingOrder;
        _display.shadowCastingMode = ShadowCastingMode.Off;
        _display.receiveShadows = false;

        FillBlock(_displayBlock, _texture, Color.white);
        _display.SetPropertyBlock(_displayBlock);

        SetWhiteDefaults(_drawBlock);
        Clear();
    }

    public void Draw(PaintMark mark)
    {
        if (false == _hasPending)
        {
            _commands.Clear();
            _commands.SetRenderTarget(_texture);
            _commands.SetViewProjectionMatrices(Matrix4x4.identity, _projection);
            _hasPending = true;
        }

        // 그리기 명령에는 이때의 값이 복사되어, 같은 블록을 다음 자국에 다시 써도 된다.
        _drawBlock.SetTexture(MAIN_TEX_ID, mark.Texture);
        _drawBlock.SetColor(COLOR_ID, mark.Color);
        _commands.DrawMesh(mark.Mesh, mark.Matrix, _material, 0, 0, _drawBlock);
    }

    public void Flush()
    {
        if (false == _hasPending)
        {
            return;
        }

        Graphics.ExecuteCommandBuffer(_commands);
        _commands.Clear();
        _hasPending = false;
    }

    // 모아 둔 그리기도 함께 버린다.
    public void Clear()
    {
        _commands.Clear();
        _commands.SetRenderTarget(_texture);
        _commands.ClearRenderTarget(false, true, Color.clear);
        Graphics.ExecuteCommandBuffer(_commands);
        _commands.Clear();
        _hasPending = false;
    }

    // 그림을 그대로 보이는 메시(구운 장, 깔리는 중인 띠)에 텍스처와 색을 넣는다.
    public static void FillBlock(MaterialPropertyBlock block, Texture texture, Color color)
    {
        block.SetTexture(MAIN_TEX_ID, texture);
        block.SetColor(COLOR_ID, color);
        SetWhiteDefaults(block);
    }

    public void SetAlpha(float alpha)
    {
        _displayBlock.SetColor(COLOR_ID, new Color(1f, 1f, 1f, alpha));
        _display.SetPropertyBlock(_displayBlock);
    }

    public void Release()
    {
        _commands.Release();
        _texture.Release();
        Object.Destroy(_texture);
    }

    private static void SetWhiteDefaults(MaterialPropertyBlock block)
    {
        block.SetColor(RENDERER_COLOR_ID, Color.white);
        block.SetVector(FLIP_ID, Vector4.one);
    }
}
