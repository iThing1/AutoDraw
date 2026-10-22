using DG.Tweening;
using System;
using UnityEngine;

// 떠오르는 글자의 색·크기·문구와 연출 시간. 문구 종류는 FloatingTextKind로 고른다.
[Serializable]
public class FloatingTextSettings
{
    [Serializable]
    public struct Phrase
    {
        [SerializeField] private FloatingTextKind _kind;
        [SerializeField] private string _text;
        [SerializeField] private Color _color;

        public Phrase(FloatingTextKind kind, string text, Color color)
        {
            _kind = kind;
            _text = text;
            _color = color;
        }

        public FloatingTextKind Kind { get { return _kind; } }
        public string Text { get { return _text; } }
        public Color Color { get { return _color; } }
    }

    [SerializeField] private Color _normalColor = Color.black;
    [SerializeField] private Color _criticalColor = new Color(0.85f, 0.2f, 0.3f);
    // 새 문구는 FloatingTextKind에 이름을 더하고 여기에 한 줄을 더하면 된다.
    [SerializeField] private Phrase[] _phrases =
    {
        new Phrase(FloatingTextKind.Guard, "GUARD", new Color(0.3f, 0.55f, 0.95f)),
        new Phrase(FloatingTextKind.NoGauge, "NO INK", new Color(0.45f, 0.45f, 0.45f)),
        new Phrase(FloatingTextKind.GimmickClear, "BREAK", new Color(0.2f, 0.6f, 0.95f)),
        new Phrase(FloatingTextKind.GimmickFail, "FAILED", new Color(0.85f, 0.2f, 0.3f)),
        new Phrase(FloatingTextKind.Heal, "+{0}", new Color(0.25f, 0.75f, 0.35f)),
        new Phrase(FloatingTextKind.Dodge, "DODGE", new Color(0.3f, 0.7f, 0.45f)),
    };
    [SerializeField, Min(0f)] private float _normalScale = 1f;
    [SerializeField, Min(0f)] private float _criticalScale = 1.6f;
    [SerializeField, Min(1f)] private float _popScale = 1.4f;
    [SerializeField, Min(0f)] private float _popDuration = 0.15f;
    [SerializeField] private Ease _popEase = Ease.OutBack;
    [SerializeField] private float _riseDistance = 80f;
    [SerializeField] private Ease _riseEase = Ease.OutCubic;
    [SerializeField, Min(0.01f)] private float _duration = 0.7f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;

    public Color NormalColor { get { return _normalColor; } }
    public Color CriticalColor { get { return _criticalColor; } }
    public float NormalScale { get { return _normalScale; } }
    public float CriticalScale { get { return _criticalScale; } }
    public float PopScale { get { return _popScale; } }
    public float PopDuration { get { return _popDuration; } }
    public Ease PopEase { get { return _popEase; } }
    public float RiseDistance { get { return _riseDistance; } }
    public Ease RiseEase { get { return _riseEase; } }
    public float Duration { get { return _duration; } }
    public float FadeDuration { get { return Mathf.Min(_fadeDuration, _duration); } }

    // 설정에 없는 종류면 문구가 빈 값이다.
    public Phrase GetPhrase(FloatingTextKind kind)
    {
        if (null != _phrases)
        {
            for (int i = 0; i < _phrases.Length; i++)
            {
                if (kind == _phrases[i].Kind)
                {
                    return _phrases[i];
                }
            }
        }

        return default;
    }
}
