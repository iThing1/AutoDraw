using UnityEngine;

[CreateAssetMenu(fileName = "PaintPalette", menuName = "Scriptable Objects/PaintPalette")]
public class PaintPalette : ScriptableObject
{
    [SerializeField] private Color[] _colors = { Color.white };
    [SerializeField] private bool _pickRandomly;
    [SerializeField] private bool _isInk;

    // 검은 잉크 팔레트로 칠한 자국은 엔딩에서 지워진다.
    public bool IsInk { get { return _isInk; } }

    // 몇 번째 색을 쓸지는 에셋에 저장되지 않도록 호출하는 쪽이 세어서 넘겨준다.
    // 무작위 선택이면 순서 번호는 쓰지 않는다.
    public Color Pick(int sequenceIndex)
    {
        if (null == _colors || 0 == _colors.Length)
        {
            return Color.white;
        }

        if (_pickRandomly)
        {
            return _colors[Random.Range(0, _colors.Length)];
        }

        return _colors[sequenceIndex % _colors.Length];
    }
}
