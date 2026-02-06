using UnityEngine;

/// <summary>
/// A triggerable sound. Trigger plays the sound.
/// </summary>
public class TriggerableSound : ITriggerable
{
    [SerializeField] AudioClip audioSource;

    public override void Trigger()
    {
        if (audioSource != null)
        {
            SoundManager.Instance.PlaySoundAt(transform.position, audioSource);
        }
    }

    public override void Trigger(Vector3 position)
    {
        if (audioSource != null)
        {
            SoundManager.Instance.PlaySoundAt(position, audioSource);
        }
    }
}
