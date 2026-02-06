using UnityEngine;

/// <summary>
/// An area that triggers when colliding (triggers once when entering, once when exiting)
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerArea : IActivableTrigger
{
    private void OnTriggerEnter(Collider other)
    {
        OnTriggerActivate();
    }

    private void OnTriggerExit(Collider other)
    {
        OnTriggerDeactivate();
    }
}
