using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public class ShatterSettings
{
    [SerializeField, Min(1)] private int _columns = 4;
    [SerializeField, Min(1)] private int _rows = 4;
    [SerializeField] private FloatRange _spreadDistance = new(1.5f, 3.5f);
    [SerializeField, Min(0f)] private float _maxRotation = 360f;
    [SerializeField, Min(0f)] private float _flyDuration = 0.8f;
    [SerializeField] private Ease _flyEase = Ease.OutCubic;
    [SerializeField] private FloatRange _fadeDelay = new(0.3f, 1f);
    [SerializeField, Min(0f)] private float _fadeDuration = 0.6f;
    [SerializeField] private ImpactSettings _impact = new();

    public int Columns { get { return _columns; } }
    public int Rows { get { return _rows; } }
    public FloatRange SpreadDistance { get { return _spreadDistance; } }
    public float MaxRotation { get { return _maxRotation; } }
    public float FlyDuration { get { return _flyDuration; } }
    public Ease FlyEase { get { return _flyEase; } }
    public FloatRange FadeDelay { get { return _fadeDelay; } }
    public float FadeDuration { get { return _fadeDuration; } }
    public ImpactSettings Impact { get { return _impact; } }
}
