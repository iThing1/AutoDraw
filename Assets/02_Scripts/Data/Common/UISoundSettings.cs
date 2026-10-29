using System;
using UnityEngine;

// 화면 조작·HUD 상태 변화에 붙는 소리. 소리 종류는 UISoundKind로 고른다.
[Serializable]
public class UISoundSettings
{
    [Serializable]
    public class Cue
    {
        [SerializeField] private UISoundKind _kind;
        [SerializeField] private AudioClip _clip;
        // 같은 소리를 여러 종류에 쓸 때 음높이로 구분한다.
        [SerializeField, Range(0.1f, 3f)] private float _pitch = 1f;

        public UISoundKind Kind { get { return _kind; } }
        public AudioClip Clip { get { return _clip; } }
        public float Pitch { get { return _pitch; } }
    }

    // 새 소리는 UISoundKind에 이름을 더하고 여기에 한 줄을 더하면 된다.
    [SerializeField] private Cue[] _cues;

    // 설정에 없는 종류면 false다.
    public bool TryGetCue(UISoundKind kind, out Cue cue)
    {
        if (null != _cues)
        {
            for (int i = 0; i < _cues.Length; i++)
            {
                if (kind == _cues[i].Kind)
                {
                    cue = _cues[i];
                    return true;
                }
            }
        }

        cue = null;
        return false;
    }
}
