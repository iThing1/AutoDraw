using UnityEngine;

// 롤러처럼 이어서 깔리는 물감 띠 하나. 깔리는 동안은 메시만 늘리고, 끝나면 그 메시 그대로 캔버스에 기록해 굽는다.
public class PaintStripe
{
    private readonly PaintCanvas _canvas;
    private readonly GameObject _display;
    private readonly Mesh _mesh;
    private readonly Texture _texture;
    private readonly Vector3 _start;
    private readonly Vector3 _direction;
    private readonly Vector3 _side;
    private readonly Color _color;
    private readonly bool _isInk;
    private readonly float _width;
    private readonly float _tileLength;
    private readonly Vector3[] _vertices = new Vector3[4];
    private readonly Vector2[] _uvs = new Vector2[4];
    private float _length;
    private bool _isFinished;

    public Mesh Mesh { get { return _mesh; } }
    public Texture Texture { get { return _texture; } }
    public Vector3 Start { get { return _start; } }
    public Vector3 End { get { return _start + _direction * _length; } }
    public Color Color { get { return _color; } }
    public bool IsInk { get { return _isInk; } }
    public float Width { get { return _width; } }
    public float Length { get { return _length; } }

    public PaintStripe(PaintCanvas canvas, GameObject display, Mesh mesh, Texture texture, Vector3 start, Vector3 direction, float width, float tileLength, Color color, bool isInk)
    {
        _canvas = canvas;
        _display = display;
        _mesh = mesh;
        _texture = texture;
        _start = start;
        _direction = direction.normalized;
        _side = new Vector3(-_direction.y, _direction.x, 0f) * (width * 0.5f);
        _width = width;
        _tileLength = tileLength;
        _color = color;
        _isInk = isInk;

        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        _mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
    }

    public void SetLength(float length)
    {
        if (_isFinished)
        {
            return;
        }

        _length = Mathf.Max(0f, length);

        Vector3 end = End;
        float repeat = _length / _tileLength;

        _vertices[0] = _start - _side;
        _vertices[1] = end - _side;
        _vertices[2] = _start + _side;
        _vertices[3] = end + _side;
        _uvs[0] = new Vector2(0f, 0f);
        _uvs[1] = new Vector2(repeat, 0f);
        _uvs[2] = new Vector2(0f, 1f);
        _uvs[3] = new Vector2(repeat, 1f);

        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.RecalculateBounds();
    }

    // 깔린 만큼 굽고 깔리는 동안 보이던 띠는 지운다. 스테이지와 함께 캔버스가 사라졌으면 할 일이 없다.
    public void Finish()
    {
        if (_isFinished)
        {
            return;
        }

        _isFinished = true;

        if (null != _canvas)
        {
            _canvas.FinishStripe(this);
        }

        if (null != _display)
        {
            Object.Destroy(_display);
        }
    }
}
