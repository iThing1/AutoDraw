using UnityEngine;

// 캔버스에 남은 자국 하나의 기록. 그림은 텍스처에 구워 두고, 판정(칠해졌는지·덮였는지)과 다시 굽기는 이 기록으로 한다.
// 점 자국은 원, 롤러 띠는 굵은 선분으로 판정한다.
public class PaintMark
{
    private readonly Mesh _mesh;
    private readonly Texture _texture;
    private readonly Matrix4x4 _matrix;
    private readonly Color _color;
    private readonly bool _isInk;
    private readonly Vector3 _start;
    private readonly Vector3 _end;
    private readonly float _radius;
    // 점 자국만 있다. 꿈틀·흐려지기처럼 잠깐 겉모습을 다시 띄울 때 쓴다.
    private readonly Sprite _sprite;
    private readonly Quaternion _rotation;
    private readonly float _scale;
    private bool _isBaked;
    private bool _isRemoved;

    public Mesh Mesh { get { return _mesh; } }
    public Texture Texture { get { return _texture; } }
    public Matrix4x4 Matrix { get { return _matrix; } }
    public Color Color { get { return _color; } }
    public bool IsInk { get { return _isInk; } }
    public Vector3 Position { get { return (_start + _end) * 0.5f; } }
    public Sprite Sprite { get { return _sprite; } }
    public Quaternion Rotation { get { return _rotation; } }
    public float Scale { get { return _scale; } }
    // 커지는 연출이 끝나 텍스처에 그려졌는지. 다시 그릴 때 아직 안 그려진 자국은 뺀다.
    public bool IsBaked { get { return _isBaked; } }
    public bool IsRemoved { get { return _isRemoved; } }

    private PaintMark(Mesh mesh, Texture texture, Matrix4x4 matrix, Color color, bool isInk, Vector3 start, Vector3 end, float radius, Sprite sprite, Quaternion rotation, float scale)
    {
        _mesh = mesh;
        _texture = texture;
        _matrix = matrix;
        _color = color;
        _isInk = isInk;
        _start = start;
        _end = end;
        _radius = radius;
        _sprite = sprite;
        _rotation = rotation;
        _scale = scale;
    }

    // 판정 반지름은 그림의 가로 절반으로 넉넉히 잡는다.
    public static PaintMark ForSplat(Sprite sprite, Mesh mesh, Vector3 position, Quaternion rotation, float scale, Color color, bool isInk)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, Vector3.one * scale);
        float radius = sprite.bounds.extents.x * scale;

        return new PaintMark(mesh, sprite.texture, matrix, color, isInk, position, position, radius, sprite, rotation, scale);
    }

    // 띠 메시는 월드 좌표로 만들어 두므로 변환이 없다.
    public static PaintMark ForStripe(Mesh mesh, Texture texture, Vector3 start, Vector3 end, float width, Color color, bool isInk)
    {
        return new PaintMark(mesh, texture, Matrix4x4.identity, color, isInk, start, end, width * 0.5f, null, Quaternion.identity, 1f);
    }

    // 이 원이 자국과 겹치는지.
    public bool Overlaps(Vector3 point, float radius)
    {
        return Vector2.Distance(point, Geometry2D.GetClosestPointOnSegment(_start, _end, point)) <= _radius + radius;
    }

    public void MarkBaked()
    {
        _isBaked = true;
    }

    public void MarkRemoved()
    {
        _isRemoved = true;
    }
}
