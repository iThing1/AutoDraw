using System;
using UnityEngine;

// 목표 둘레에 먹물 웅덩이를 흩뿌리고, 웅덩이에서 그림자 팔이 목표를 앞질러 노리며 뻗어 나오는 공격의 값.
// 보스의 잉크 팔 기믹과 보스 2를 깨고 얻는 잉크 손 스킬이 함께 쓴다.
[Serializable]
public class InkArmsSettings
{
    [Header("먹물 흩뿌리기")]
    // 목표 둘레 이 반경 안에 웅덩이를 흩뿌린다. 목표가 어디로 움직여도 근처에 웅덩이가 있도록 넉넉히 깐다.
    [SerializeField, Min(1)] private int _poolCount = 16;
    [SerializeField, Min(0f)] private float _poolScatterRadius = 18f;
    [SerializeField] private SplatStyle _poolStyle;
    [SerializeField] private FloatRange _poolScale = new FloatRange(1.6f, 2.6f);
    [SerializeField, Min(0f)] private float _poolInterval = 0.04f;
    [SerializeField, Min(0f)] private float _poolSettleDuration = 0.5f;

    [Header("시간")]
    [SerializeField, Min(0f)] private float _duration = 8f;
    [SerializeField, Min(0.05f)] private float _lungeInterval = 0.7f;
    [SerializeField, Min(0f)] private float _retractDuration = 0.35f;

    [Header("노리기")]
    [SerializeField] private FloatRange _sourceDistance = new FloatRange(3f, 12f);
    // 팔이 다 뻗을 때 목표가 있을 자리를 이 시간(초)만큼 앞질러 노린다.
    [SerializeField, Min(0f)] private float _leadTime = 0.35f;
    // 기어 오는 동안 붙잡을 자리가 목표를 따라가는 빠르기(초당 거리).
    [SerializeField, Min(0f)] private float _homingSpeed = 4.5f;

    [Header("팔")]
    [SerializeField] private IntRange _armsPerLunge = new IntRange(1, 2);
    // 팔의 모양·기어 오는 빠르기·꿈틀거림은 먹물 삼킴 연출의 값을 같이 쓴다.
    [SerializeField] private InkSwallowSettings _arms = new();
    [SerializeField] private HitSettings _grabHit = new();

    public int PoolCount { get { return _poolCount; } }
    public float PoolScatterRadius { get { return _poolScatterRadius; } }
    public SplatStyle PoolStyle { get { return _poolStyle; } }
    public FloatRange PoolScale { get { return _poolScale; } }
    public float PoolInterval { get { return _poolInterval; } }
    public float PoolSettleDuration { get { return _poolSettleDuration; } }

    public float Duration { get { return _duration; } }
    public float LungeInterval { get { return _lungeInterval; } }
    public float RetractDuration { get { return _retractDuration; } }

    public FloatRange SourceDistance { get { return _sourceDistance; } }
    public float LeadTime { get { return _leadTime; } }
    public float HomingSpeed { get { return _homingSpeed; } }

    public IntRange ArmsPerLunge { get { return _armsPerLunge; } }
    public InkSwallowSettings Arms { get { return _arms; } }
    public HitSettings GrabHit { get { return _grabHit; } }
}
