using UnityEngine;

// 공격 하나를 이루는 발사 패턴, 수치, 연출의 묶음. 키 전환은 이 에셋을 통째로 바꿔 끼운다.
[CreateAssetMenu(fileName = "AttackStyle", menuName = "Scriptable Objects/AttackStyle")]
public class AttackStyle : ScriptableObject
{
    [Header("표시")]
    [SerializeField] private string _displayName;
    [SerializeField] private Color _iconColor = Color.white;

    [Header("공격")]
    [SerializeField, Min(0.05f)] private float _attackInterval = 1f;
    [SerializeField] private AttackPattern _pattern = AttackPattern.Single;
    [SerializeField, Min(0f)] private float _gaugeGainPerHit = 5f;

    [Header("발사체")]
    [SerializeField] private ShotSettings _shot = new();

    [Header("연사")]
    [SerializeField, Min(1)] private int _burstCount = 3;
    [SerializeField, Min(0f)] private float _burstInterval = 0.08f;

    [Header("파편")]
    [SerializeField] private ClusterSettings _cluster = new();

    public string DisplayName { get { return _displayName; } }
    public Color IconColor { get { return _iconColor; } }

    public float AttackInterval { get { return _attackInterval; } }
    public AttackPattern Pattern { get { return _pattern; } }
    public float GaugeGainPerHit { get { return _gaugeGainPerHit; } }

    public ShotSettings Shot { get { return _shot; } }

    public int BurstCount { get { return _burstCount; } }
    public float BurstInterval { get { return _burstInterval; } }

    public ClusterSettings Cluster { get { return _cluster; } }
}
