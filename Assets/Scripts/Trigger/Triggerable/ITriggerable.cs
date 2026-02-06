using UnityEngine;

/// <summary>
/// An abstract class for any triggerable object
/// </summary>
public abstract class ITriggerable : MonoBehaviour
{
    public abstract void Trigger();
    public abstract void Trigger(Vector3 position);
}
