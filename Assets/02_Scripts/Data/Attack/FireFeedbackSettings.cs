using System;
using UnityEngine;

// 한 발 쏠 때마다 나는 소리·흔들림과 쏜 쪽 몸의 움츠림. 꺼 두면 아무것도 하지 않아, 보스가 같은 공격 방식을 써도 그대로다.
[Serializable]
public class FireFeedbackSettings
{
    [SerializeField] private bool _enabled;
    [SerializeField] private bool _recoils = true;
    [SerializeField] private ImpactSettings _impact = new();

    public bool Enabled { get { return _enabled; } }
    public bool Recoils { get { return _recoils; } }
    public ImpactSettings Impact { get { return _impact; } }
}
