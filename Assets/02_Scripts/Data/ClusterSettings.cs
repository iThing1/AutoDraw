using System;
using UnityEngine;

// 공중에서 터져 여러 조각으로 갈라지는 공격의 설정.
[Serializable]
public class ClusterSettings
{
    [SerializeField, Range(0f, 1f)] private float _splitRatio = 0.45f;
    [SerializeField, Min(0f)] private float _splitHeight = 5f;
    [SerializeField] private ImpactSettings _splitImpact = new();

    [SerializeField, Min(1)] private int _fragmentCount = 10;
    [SerializeField, Min(0f)] private float _fragmentInterval = 0.05f;
    [SerializeField, Min(0f)] private float _landingSpread = 1.5f;
    [SerializeField] private ShotSettings _fragment = new();

    public float SplitRatio { get { return _splitRatio; } }
    public float SplitHeight { get { return _splitHeight; } }
    public ImpactSettings SplitImpact { get { return _splitImpact; } }

    public int FragmentCount { get { return _fragmentCount; } }
    public float FragmentInterval { get { return _fragmentInterval; } }
    public float LandingSpread { get { return _landingSpread; } }
    public ShotSettings Fragment { get { return _fragment; } }
}
