using UnityEngine;

public class SoundManager
{
    // 소리마다 피치를 다르게 주려면 AudioSource를 나눠야 해서, 몇 개를 돌려 가며 쓴다.
    private const int SFX_SOURCE_COUNT = 8;

    private AudioSource[] _sfxSources;
    private int _nextSourceIndex;

    public void Init(GameObject owner)
    {
        _sfxSources = new AudioSource[SFX_SOURCE_COUNT];

        for (int i = 0; i < SFX_SOURCE_COUNT; i++)
        {
            AudioSource source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;

            _sfxSources[i] = source;
        }
    }

    public void PlaySfx(AudioClip clip)
    {
        PlaySfx(clip, 1f);
    }

    // 재생 중인 소스의 피치를 바꾸면 이미 나던 소리의 음높이까지 바뀌므로, 한 소스에는 한 소리만 재생한다.
    public void PlaySfx(AudioClip clip, float pitch)
    {
        if (null == clip || null == _sfxSources)
        {
            return;
        }

        AudioSource source = GetIdleSource();

        source.clip = clip;
        source.pitch = pitch;
        source.Play();
    }

    // 쉬고 있는 소스를 먼저 쓰고, 모두 재생 중이면 다음 차례 소스의 소리를 끊고 쓴다.
    private AudioSource GetIdleSource()
    {
        for (int i = 0; i < _sfxSources.Length; i++)
        {
            int index = (_nextSourceIndex + i) % _sfxSources.Length;

            if (false == _sfxSources[index].isPlaying)
            {
                return TakeSource(index);
            }
        }

        return TakeSource(_nextSourceIndex);
    }

    private AudioSource TakeSource(int index)
    {
        _nextSourceIndex = (index + 1) % _sfxSources.Length;

        return _sfxSources[index];
    }
}
