using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerFootLift : ITrigger
{
    public void OnFootLift(Vector3 footLiftPosition)
    {
        OnTrigger(footLiftPosition);
    }
}
