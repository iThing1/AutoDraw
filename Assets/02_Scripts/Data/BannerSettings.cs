using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public class BannerSettings
{
    [SerializeField, Min(0f)] private float _inDuration = 0.3f;
    [SerializeField, Min(0f)] private float _holdDuration = 1.4f;
    [SerializeField, Min(0f)] private float _outDuration = 0.3f;
    [SerializeField, Min(0f)] private float _startScale = 1.2f;
    [SerializeField] private float _exitMoveY = 40f;
    [SerializeField] private Ease _inEase = Ease.OutBack;
    [SerializeField] private Ease _outEase = Ease.InQuad;
    [SerializeField] private AudioClip _sound;

    public float InDuration { get { return _inDuration; } }
    public float HoldDuration { get { return _holdDuration; } }
    public float OutDuration { get { return _outDuration; } }
    public float StartScale { get { return _startScale; } }
    public float ExitMoveY { get { return _exitMoveY; } }
    public Ease InEase { get { return _inEase; } }
    public Ease OutEase { get { return _outEase; } }
    public AudioClip Sound { get { return _sound; } }
}
