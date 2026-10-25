using System;
using UnityEngine;

// 그림 한 장을 늘이고 누르고 기울여 움직임을 만드는 값. 크기는 1에 더하는 비율, 각도는 도 단위다.
[Serializable]
public class UnitAnimationSettings
{
    [SerializeField] private bool _useAnimation = true;

    [Header("숨쉬기")]
    [SerializeField, Min(0f)] private float _breatheAmount = 0.03f;
    // 1초에 몇 번 숨 쉬는지.
    [SerializeField, Min(0f)] private float _breatheSpeed = 0.6f;

    [Header("걸음")]
    // 이만큼 움직일 때마다 한 번 통통 튄다.
    [SerializeField, Min(0.1f)] private float _stepLength = 0.8f;
    [SerializeField, Min(0f)] private float _bobHeight = 0.06f;
    [SerializeField, Min(0f)] private float _landSquash = 0.08f;
    [SerializeField, Range(0f, 45f)] private float _leanAngle = 8f;
    // 이 속도에서 걸음·기울기가 가장 커진다.
    [SerializeField, Min(0.1f)] private float _fullSpeed = 7f;
    // 그림이 좌우 대칭이 아니면 가는 쪽을 보도록 뒤집는다.
    [SerializeField] private bool _flipByDirection;

    [Header("반응")]
    [SerializeField, Min(0f)] private float _attackSquash = 0.12f;
    [SerializeField, Min(0f)] private float _hitSquash = 0.2f;
    [SerializeField, Min(0f)] private float _guardSquash = 0.1f;
    [SerializeField, Min(0.01f)] private float _reactionDuration = 0.25f;
    [SerializeField, Min(0f)] private float _dashStretch = 0.35f;

    // 속도·기울기가 바뀌는 빠르기. 클수록 바로 따라간다.
    [SerializeField, Min(0.1f)] private float _followSharpness = 14f;

    public bool UseAnimation { get { return _useAnimation; } }
    public float BreatheAmount { get { return _breatheAmount; } }
    public float BreatheSpeed { get { return _breatheSpeed; } }
    public float StepLength { get { return _stepLength; } }
    public float BobHeight { get { return _bobHeight; } }
    public float LandSquash { get { return _landSquash; } }
    public float LeanAngle { get { return _leanAngle; } }
    public float FullSpeed { get { return _fullSpeed; } }
    public bool FlipByDirection { get { return _flipByDirection; } }
    public float AttackSquash { get { return _attackSquash; } }
    public float HitSquash { get { return _hitSquash; } }
    public float GuardSquash { get { return _guardSquash; } }
    public float ReactionDuration { get { return _reactionDuration; } }
    public float DashStretch { get { return _dashStretch; } }
    public float FollowSharpness { get { return _followSharpness; } }
}
