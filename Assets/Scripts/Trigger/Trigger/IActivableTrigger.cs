using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An abstract class for anything that needs to trigger activable events (on/off state)
/// </summary>
public abstract class IActivableTrigger : MonoBehaviour
{
    [SerializeField] List<IActivableTriggerable> activableTriggers;

    protected void OnTriggerActivate()
    {
        foreach (IActivableTriggerable activableTrigger in activableTriggers)
        {
            activableTrigger.OnTriggerActivate();
        }
    }

    protected void OnTriggerDeactivate()
    {
        foreach (IActivableTriggerable activableTrigger in activableTriggers)
        {
            activableTrigger.OnTriggerDeactivate();
        }
    }
}
