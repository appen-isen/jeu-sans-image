using UnityEngine;

/// <summary>
/// A triggerable music. Trigger can activate and deactivate the music.
/// </summary>
public class TriggerableMusic : IActivableTriggerable
{
    [SerializeField] ILoopableSound music;

    public override void OnTriggerActivate()
    {
        if (music != null)
        {
            MusicManager.Instance.StartMusic(music);
        }
    }

    public override void OnTriggerDeactivate()
    {
        if (music != null)
        {
            MusicManager.Instance.StopMusic(music);
        }
    }
}
