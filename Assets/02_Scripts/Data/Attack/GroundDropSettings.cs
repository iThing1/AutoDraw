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

    public GameObject Prefab { get { return _prefab; } }
    public float Radius { get { return _radius; } }
    public Color TelegraphColor { get { return _telegraphColor; } }
    public float WarnDuration { get { return _warnDuration; } }
    public HitSettings Hit { get { return _hit; } }
    public ImpactSettings Impact { get { return _impact; } }
}
