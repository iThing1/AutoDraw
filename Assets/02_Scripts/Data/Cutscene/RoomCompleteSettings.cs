using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public class RoomCompleteSettings
{
    [SerializeField] private CinematicSettings _cinematic = new();
    [SerializeField, Min(0f)] private float _moveDuration = 1.6f;
    [SerializeField, Min(0f)] private float _holdDuration = 1.4f;
    [SerializeField, Min(0f)] private float _homeDuration = 1.2f;
    [SerializeField] private Ease _moveEase = Ease.InOutSine;
    [SerializeField] private BannerSettings _banner = new();

    public CinematicSettings Cinematic { get { return _cinematic; } }
    public float MoveDuration { get { return _moveDuration; } }
    public float HoldDuration { get { return _holdDuration; } }
    public float HomeDuration { get { return _homeDuration; } }
    public Ease MoveEase { get { return _moveEase; } }
    public BannerSettings Banner { get { return _banner; } }
}
