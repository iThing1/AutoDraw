using System;
using UnityEngine;

[Serializable]
public class SkillData : BaseData
{
    [SerializeField] private string _name;
    [SerializeField] private string _attackStyleKey;
    [SerializeField] private bool _isStarting;
    [SerializeField] private string _iconKey;
    [SerializeField] private string _description;
    [SerializeField] private string _previewKey;

    public string Name { get { return _name; } }
    public string AttackStyleKey { get { return _attackStyleKey; } }
    public bool IsStarting { get { return _isStarting; } }
    public string IconKey { get { return _iconKey; } }
    public string Description { get { return _description; } }
    // 비어 있으면 미리보기 영상이 없다.
    public string PreviewKey { get { return _previewKey; } }
}
