using UnityEngine;

/// <summary>
/// A collision trigger that triggers one-shot event when colliding with an object
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerCollision : ITrigger
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount > 0)
        {
            Vector3 contactPoint = collision.GetContact(0).point;
            OnTrigger(contactPoint);
        }
    }
}
