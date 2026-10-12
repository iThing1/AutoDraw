using UnityEngine;

public class SoundManager
{
    private AudioSource _sfxSource;

    public void Init(GameObject owner)
    {
        _sfxSource = owner.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
    }

    public void PlaySfx(AudioClip clip)
    {
        if (null == clip || null == _sfxSource)
        {
            return;
        }

        _sfxSource.PlayOneShot(clip);
    }
}
