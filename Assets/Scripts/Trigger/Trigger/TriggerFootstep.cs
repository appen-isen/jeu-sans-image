using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerFootstep : ITrigger
{
    public void OnFootstep(Vector3 footstepPosition)
    {
        OnTrigger(footstepPosition);
    }
}
