using UnityEngine;

public class LoopableSound : ILoopableSound
{
    [SerializeField] AudioClip sound;

    protected override void PlayLoopableSound()
    {
        base.PlayLoopableSound();
        source.clip = sound;
        source.Play();
    }

    protected override void StopLoopableSound()
    {
        source.Stop();
        base.StopLoopableSound();
    }
}
