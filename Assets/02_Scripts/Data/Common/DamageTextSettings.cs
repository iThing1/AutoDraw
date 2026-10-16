using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public class DamageTextSettings
{
    [SerializeField] private Color _normalColor = Color.black;
    [SerializeField] private Color _criticalColor = new Color(0.85f, 0.2f, 0.3f);
    [SerializeField] private string _guardText = "GUARD";
    [SerializeField] private Color _guardColor = new Color(0.3f, 0.55f, 0.95f);
    // 게이지가 모자라 막기가 안 나갔을 때 띄운다.
    [SerializeField] private string _noGaugeText = "NO INK";
    [SerializeField] private Color _noGaugeColor = new Color(0.45f, 0.45f, 0.45f);
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
    public string GuardText { get { return _guardText; } }
    public Color GuardColor { get { return _guardColor; } }
    public string NoGaugeText { get { return _noGaugeText; } }
    public Color NoGaugeColor { get { return _noGaugeColor; } }
    public float NormalScale { get { return _normalScale; } }
    public float CriticalScale { get { return _criticalScale; } }
    public float PopScale { get { return _popScale; } }
    public float PopDuration { get { return _popDuration; } }
    public Ease PopEase { get { return _popEase; } }
    public float RiseDistance { get { return _riseDistance; } }
    public Ease RiseEase { get { return _riseEase; } }
    public float Duration { get { return _duration; } }
    public float FadeDuration { get { return Mathf.Min(_fadeDuration, _duration); } }
}
