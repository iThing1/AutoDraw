using System;
using UnityEngine;

// 들어온 것을 느리게 하는 원형 구역 하나의 값. 시간 왜곡은 탄까지, 먹물 웅덩이는 이동만 느리게 한다.
[Serializable]
public class SlowZoneSettings
{
    [SerializeField, Min(0.1f)] private float _radius = 3.5f;
    // 안에서의 속도 배율. 1이면 느려지지 않는다.
    [SerializeField, Range(0f, 1f)] private float _moveScale = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _projectileScale = 0.12f;

    [Header("시간")]
    [SerializeField, Min(0f)] private float _growDuration = 0.3f;
    [SerializeField, Min(0f)] private float _duration = 5f;
    // 끝날 때 깨지듯 사라지는 시간. 이 순간 느려졌던 것들이 원래 속도로 돌아온다.
    [SerializeField, Min(0f)] private float _endDuration = 0.25f;

    [Header("겉모습")]
    [SerializeField] private Color _color = new Color(0.86f, 0.85f, 0.8f, 0.4f);
    [SerializeField, Min(0f)] private float _endPunch = 0.15f;

    public float Radius { get { return _radius; } }
    public float MoveScale { get { return _moveScale; } }
    public float ProjectileScale { get { return _projectileScale; } }

    public float GrowDuration { get { return _growDuration; } }
    public float Duration { get { return _duration; } }
    public float EndDuration { get { return _endDuration; } }

    public Color Color { get { return _color; } }
    public float EndPunch { get { return _endPunch; } }
}
