// 스킬 표의 수량·지속 시간·간격. 표가 비어 있으면(0) 에셋 값을 쓴다. 보스는 NONE이라 늘 에셋 값이다.
public readonly struct SkillModifier
{
    public static readonly SkillModifier NONE = default;

    private readonly int _count;
    private readonly float _duration;
    private readonly float _interval;

    public SkillModifier(int count, float duration, float interval)
    {
        _count = count;
        _duration = duration;
        _interval = interval;
    }

    public int ResolveCount(int assetValue)
    {
        return 0 < _count ? _count : assetValue;
    }

    public float ResolveDuration(float assetValue)
    {
        return 0f < _duration ? _duration : assetValue;
    }

    public float ResolveInterval(float assetValue)
    {
        return 0f < _interval ? _interval : assetValue;
    }
}
