using System;
using UnityEngine;

[Serializable]
public class ImpactSettings
{
    private const float MIN_PITCH = 0.1f;

    [SerializeField, Min(0f)] private float _shakeForce = 1.5f;
    [SerializeField] private AudioClip[] _sounds;
    [SerializeField] private FloatRange _pitch = new(1f, 1f);
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField] private Color _effectColor = Color.black;
    [SerializeField, Min(0f)] private float _effectScale = 1f;
    [SerializeField, Min(0f)] private float _effectLifetime = 3f;

    public float ShakeForce { get { return _shakeForce; } }
    public GameObject EffectPrefab { get { return _effectPrefab; } }
    public Color EffectColor { get { return _effectColor; } }
    public float EffectScale { get { return _effectScale; } }
    public float EffectLifetime { get { return _effectLifetime; } }

    // 같은 소리가 반복돼 기계적으로 들리지 않도록, 여러 소리 중 하나를 고르고 피치를 조금씩 흔든다.
    public AudioClip PickSound()
    {
        if (null == _sounds || 0 == _sounds.Length)
        {
            return null;
        }

        return _sounds[UnityEngine.Random.Range(0, _sounds.Length)];
    }

    // 피치가 0이면 소리가 나지 않으므로 하한을 둔다.
    public float PickPitch()
    {
        return Mathf.Max(MIN_PITCH, _pitch.PickRandom());
    }
}
