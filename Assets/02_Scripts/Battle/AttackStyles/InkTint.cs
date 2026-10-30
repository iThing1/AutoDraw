using UnityEngine;

// 패턴의 예고·먹 색에 덧입히는 색. 되감은 패턴을 원래 패턴과 구분해 보이게 한다.
// 기본값(default)은 세기 0이라 원래 색을 그대로 돌려준다.
public readonly struct InkTint
{
    private readonly Color _color;
    private readonly float _strength;

    public InkTint(Color color, float strength)
    {
        _color = color;
        _strength = Mathf.Clamp01(strength);
    }

    // 투명도는 원래 색 것을 그대로 둬, 옅은 예고는 옅은 채로 색만 바뀐다.
    public Color Apply(Color color)
    {
        Color tinted = Color.Lerp(color, _color, _strength);
        tinted.a = color.a;

        return tinted;
    }
}
