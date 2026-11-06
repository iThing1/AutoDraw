using System;
using UnityEngine;

[Serializable]
public class SkillData : BaseData
{
    [SerializeField] private string _name;
    [SerializeField] private string _attackStyleKey;
    // 스킬마다 쓰는 값만 채운다. 비어 있으면(0) 에셋 값을 쓴다. 수량은 파편·일제사격·웅덩이 수이고, 간격은 그 파편·사격·팔 뻗기의 간격이다.
    [SerializeField] private int _count;
    [SerializeField] private float _duration;
    [SerializeField] private float _interval;
    [SerializeField] private bool _isStarting;
    [SerializeField] private string _iconKey;
    [SerializeField] private string _description;
    [SerializeField] private string _previewKey;

    public string Name { get { return _name; } }
    public string AttackStyleKey { get { return _attackStyleKey; } }
    public int Count { get { return _count; } }
    public float Duration { get { return _duration; } }
    public float Interval { get { return _interval; } }
    public bool IsStarting { get { return _isStarting; } }
    public string IconKey { get { return _iconKey; } }
    public string Description { get { return _description; } }
    // 비어 있으면 미리보기 영상이 없다.
    public string PreviewKey { get { return _previewKey; } }
}
