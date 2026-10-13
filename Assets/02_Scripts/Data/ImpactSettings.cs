using System;
using UnityEngine;

[Serializable]
public class ImpactSettings
{
    [SerializeField, Min(0f)] private float _shakeForce = 1.5f;
    [SerializeField] private AudioClip _sound;
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField] private Color _effectColor = Color.black;
    [SerializeField, Min(0f)] private float _effectScale = 1f;
    [SerializeField, Min(0f)] private float _effectLifetime = 3f;

    public float ShakeForce { get { return _shakeForce; } }
    public AudioClip Sound { get { return _sound; } }
    public GameObject EffectPrefab { get { return _effectPrefab; } }
    public Color EffectColor { get { return _effectColor; } }
    public float EffectScale { get { return _effectScale; } }
    public float EffectLifetime { get { return _effectLifetime; } }
}
