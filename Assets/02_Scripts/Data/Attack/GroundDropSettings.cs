using System;
using UnityEngine;

// 기믹이 플레이어를 몰아붙일 때 바닥에 떨어뜨리는 원 하나의 값.
[Serializable]
public class GroundDropSettings
{
    [SerializeField] private GameObject _prefab;
    [SerializeField, Min(0.1f)] private float _radius = 1.4f;
    [SerializeField] private Color _telegraphColor = new Color(0.1f, 0.1f, 0.12f, 0.35f);
    [SerializeField, Min(0f)] private float _warnDuration = 0.8f;
    [SerializeField] private HitSettings _hit = new();
    [SerializeField] private ImpactSettings _impact = new();

    [Header("웅덩이")]
    // 0이면 터지고 바로 사라진다. 0보다 크면 그만큼 웅덩이로 남아 닿을 때마다 아프다. 한 자리 낙하에만 남긴다.
    [SerializeField, Min(0f)] private float _linger;
    [SerializeField] private Color _puddleColor = new Color(0.1f, 0.1f, 0.12f, 0.55f);
    // 웅덩이에 맞으면 이 색으로 번쩍였다가 돌아온다.
    [SerializeField] private Color _flashColor = new Color(0.85f, 0.1f, 0.1f, 0.65f);
    [SerializeField, Min(0.05f)] private float _touchInterval = 0.5f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.3f;
    [SerializeField] private HitSettings _touchHit = new();
    // 맞은 자리(플레이어)에서 터뜨려 무엇에 맞았는지 알린다.
    [SerializeField] private ImpactSettings _touchImpact = new();

    public GameObject Prefab { get { return _prefab; } }
    public float Radius { get { return _radius; } }
    public Color TelegraphColor { get { return _telegraphColor; } }
    public float WarnDuration { get { return _warnDuration; } }
    public HitSettings Hit { get { return _hit; } }
    public ImpactSettings Impact { get { return _impact; } }
    public float Linger { get { return _linger; } }
    public Color PuddleColor { get { return _puddleColor; } }
    public Color FlashColor { get { return _flashColor; } }
    public float TouchInterval { get { return _touchInterval; } }
    public float FadeDuration { get { return _fadeDuration; } }
    public HitSettings TouchHit { get { return _touchHit; } }
    public ImpactSettings TouchImpact { get { return _touchImpact; } }
}
