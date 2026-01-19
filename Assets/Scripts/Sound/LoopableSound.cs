using UnityEngine;

public class LoopableSound : ILoopableSound
{
    [SerializeField] AudioClip sound;

    public override void PlayLoopableSound()
    {
        base.PlayLoopableSound();
        source.clip = sound;
        source.Play();
    }

    public override void StopLoopableSound()
    {
        source.Stop();
        base.StopLoopableSound();
    }
}
