using DG.Tweening;
using System;
using UnityEngine;

[Serializable]
public class WanderSettings
{
    [SerializeField] private bool _useWander;
    [SerializeField] private Vector2 _areaHalfSize = new Vector2(4f, 2f);
    [SerializeField] private FloatRange _speed = new(1.5f, 3.5f);
    [SerializeField] private FloatRange _pause = new(0.4f, 1.2f);
    [SerializeField, Min(0f)] private float _minMoveDistance = 1.5f;
    [SerializeField] private Ease _moveEase = Ease.InOutSine;

    [Header("추격")]
    // 목표가 이보다 멀어지면 다가간다. 0이면 다가가지 않고 처음 자리 주변만 배회한다.
    [SerializeField, Min(0f)] private float _chaseDistance = 12f;
    [SerializeField, Min(0f)] private float _chaseStopDistance = 7f;

    public bool UseWander { get { return _useWander; } }
    public Vector2 AreaHalfSize { get { return _areaHalfSize; } }
    public FloatRange Speed { get { return _speed; } }
    public FloatRange Pause { get { return _pause; } }
    public float MinMoveDistance { get { return _minMoveDistance; } }
    public Ease MoveEase { get { return _moveEase; } }
    public float ChaseDistance { get { return _chaseDistance; } }
    public float ChaseStopDistance { get { return _chaseStopDistance; } }
}
