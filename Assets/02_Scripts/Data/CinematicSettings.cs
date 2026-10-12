using System;
using UnityEngine;

[Serializable]
public class CinematicSettings
{
    [SerializeField, Range(0f, 0.3f)] private float _letterboxHeightRatio = 0.12f;
    [SerializeField, Min(0f)] private float _letterboxInDuration = 0.35f;
    [SerializeField, Min(0f)] private float _letterboxOutDuration = 0.35f;
    [SerializeField, Min(0f)] private float _enterBlendDuration = 0.8f;
    [SerializeField, Min(0f)] private float _exitBlendDuration = 0.6f;
    [SerializeField] private AudioClip _enterSound;
    [SerializeField] private AudioClip _exitSound;

    public float LetterboxHeightRatio { get { return _letterboxHeightRatio; } }
    public float LetterboxInDuration { get { return _letterboxInDuration; } }
    public float LetterboxOutDuration { get { return _letterboxOutDuration; } }
    public float EnterBlendDuration { get { return _enterBlendDuration; } }
    public float ExitBlendDuration { get { return _exitBlendDuration; } }
    public AudioClip EnterSound { get { return _enterSound; } }
    public AudioClip ExitSound { get { return _exitSound; } }
}
