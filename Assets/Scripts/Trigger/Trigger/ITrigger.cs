using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An abstract class for anything that needs to trigger one-shot events
/// </summary>
public abstract class ITrigger : MonoBehaviour
{
    [SerializeField] List<ITriggerable> triggerables;

    protected void OnTrigger(Vector3 position)
    {
        foreach (ITriggerable triggerable in triggerables)
        {
            triggerable.Trigger(position);
        }
    }

    protected void OnTrigger()
    {
        foreach (var triggerable in triggerables)
        {
            triggerable.Trigger();
        }
    }
}
