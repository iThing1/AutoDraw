using UnityEngine;

[CreateAssetMenu(fileName = "PaintPalette", menuName = "Scriptable Objects/PaintPalette")]
public class PaintPalette : ScriptableObject
{
    [SerializeField] private Color[] _colors = { Color.white };
    [SerializeField] private bool _pickRandomly;
    [SerializeField] private bool _isInk;

    // 검은 잉크 팔레트로 칠한 자국은 엔딩에서 지워진다.
    public bool IsInk { get { return _isInk; } }

    // 순서 선택의 진행 위치는 에셋에 저장되지 않도록 호출하는 쪽이 들고 있는다.
    public Color Pick(ref int sequenceIndex)
    {
        if (null == _colors || 0 == _colors.Length)
        {
            return Color.white;
        }

        if (_pickRandomly)
        {
            return _colors[Random.Range(0, _colors.Length)];
        }

        Color color = _colors[sequenceIndex % _colors.Length];
        sequenceIndex = (sequenceIndex + 1) % _colors.Length;

        return color;
    }
}
