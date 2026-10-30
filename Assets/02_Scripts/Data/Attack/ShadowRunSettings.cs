using System;
using UnityEngine;

// 길을 따라 달리는 먹 그림자 하나의 값. 길을 정하고 예고하는 것은 패턴이 한다.
[Serializable]
public class ShadowRunSettings
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Color _color = new Color(0.02f, 0.02f, 0.03f, 1f);
    // 예고선 굵기도 이 값을 따라, 보이는 길과 맞는 범위를 같게 한다.
    [SerializeField, Min(0.1f)] private float _diameter = 2.2f;
    [SerializeField, Min(0.1f)] private float _speed = 45f;
    [SerializeField] private HitSettings _hit = new();

    public GameObject Prefab { get { return _prefab; } }
    public Color Color { get { return _color; } }
    public float Diameter { get { return _diameter; } }
    public float Speed { get { return _speed; } }
    public HitSettings Hit { get { return _hit; } }
}
