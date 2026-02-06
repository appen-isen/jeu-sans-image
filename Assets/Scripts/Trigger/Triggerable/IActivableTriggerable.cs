using UnityEngine;

/// <summary>
/// An abstract class for any triggerable object that has an on/off state
/// </summary>
public abstract class IActivableTriggerable : MonoBehaviour
{
    public abstract void OnTriggerActivate();
    public abstract void OnTriggerDeactivate();
}
