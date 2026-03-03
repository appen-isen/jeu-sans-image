using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class FeetController : MonoBehaviour
{
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private float acceptableRestingDistance = 0.3f;
    [SerializeField] private float footLiftDelay = 0.1f;
    [SerializeField] private float footBackToGroundDelay = 0.5f;
    [SerializeField] private float feetDistanceToCenter = 0.2f;
    [SerializeField] private float stepDistanceMultiplier = 1f;
    [SerializeField] private float maxStepHeight = 1f;
    [SerializeField] private float stepMinDistanceToWalls = 0.1f;
    [SerializeField] private float distanceUnderFootFloorDetection = 0.1f;

    private Vector3 leftFootPosition = Vector3.zero;
    private Vector3 rightFootPosition = Vector3.zero;

    private bool isRightFootNextToLift = true;  // Updated when lifting a foot
    private bool isAtRest = true;
    private bool isLiftingFoot = false;

#if UNITY_EDITOR
    public const float feetRadius = 0.15f;
    [SerializeField] private GameObject leftfoot_debug;
    [SerializeField] private GameObject rightfoot_debug;

    private void OnDrawGizmosSelected()
    {
        Handles.color = Color.magenta;
        Handles.DrawSolidDisc(leftFootPosition, Vector3.up, feetRadius);
        Handles.DrawSolidDisc(rightFootPosition, Vector3.up, feetRadius);
    }
#endif

    private void Awake()
    {
        ResetFeetPositions();
    }

    /// <summary>
    /// Reset the feet position without playing sounds.
    /// Should be used for init or reset purposes only, as otherwise footsteps might sound odd.
    /// </summary>
    private void ResetFeetPositions()
    {
        (leftFootPosition, rightFootPosition) = GetFeetRestPosition();
    }

    /// <summary>
    /// Get the expected feet location if we were at rest
    /// </summary>
    /// <returns>Tuple or expected feet location: (expectedLeft, expectedRight)</returns>
    private (Vector3, Vector3) GetFeetRestPosition()
    {
        Vector3 feetCenter = transform.position;
        Vector3 towardsRight = transform.right;
        Vector3 expectedLeft = feetCenter - feetDistanceToCenter*towardsRight;
        Vector3 expectedRight = feetCenter + feetDistanceToCenter*towardsRight;
        return (expectedLeft, expectedRight);
    }

    /// <summary>
    /// Know whether feet are in rest position or not
    /// </summary>
    /// <returns>Whether feet are in rest position or not</returns>
    private bool AreFeetInRestPosition()
    {
        var (expectedLeft, expectedRight) = GetFeetRestPosition();
        return (
            Vector3.Distance(expectedLeft, leftFootPosition) < acceptableRestingDistance
            || Vector3.Distance(expectedRight, rightFootPosition) < acceptableRestingDistance
        );
    }

    /// <summary>
    /// Lift the foot. Says it all.
    /// </summary>
    private void LiftFoot()
    {
        if (isLiftingFoot) return;  // Can't lift two feet at the same time
        isLiftingFoot = true;
        
        Vector3 liftSoundPosition = isRightFootNextToLift ? rightFootPosition : leftFootPosition;
        isRightFootNextToLift = ! isRightFootNextToLift;
        
        // Trigger sound at position
        GameObject floor = getFloorUnderPosition(liftSoundPosition);
        if (floor == null) return;  // No floor under foot

        TriggerFootLift footLift = floor.GetComponent<TriggerFootLift>();
        if (footLift == null) return;   // No foot lifting behavior

        footLift.OnFootLift(liftSoundPosition);
    }

    /// <summary>
    /// Predict where the next foot to place should be placed
    /// Returns a Vector3: predicted position, or rest position when no suitable position found
    /// Returns a Collider: collider the foot is placed on, or null if no suitable position found
    /// </summary>
    /// <returns>Whether the foot can be placed forward ; where it should be placed ; the collider under the foot</returns>
    private (Vector3, Collider) PredictFootPosition()
    {
        // Get position at rest
        var (restLeft, restRight) = GetFeetRestPosition();
        // We want the foot opposite to the next to lift (we want the next to place on ground)
        Vector3 restFoot = isRightFootNextToLift ? restLeft : restRight;
        
        // Project position forward, in moving direction
        Vector3 stepMovement = playerRb.linearVelocity * stepDistanceMultiplier;
        
        Vector3 projectedPosition;
        // Test for walls: feet can't go through walls
        if (Physics.Raycast(restFoot, stepMovement, out RaycastHit hit, stepMovement.magnitude))    // TODO wall mask
        {
            // There is a wall => foot cannot go through. Must place it in front of the wall.
            projectedPosition = restFoot + stepMovement.normalized * (hit.distance - stepMinDistanceToWalls);
        }
        else
        {
            // No wall => can take a step forward
            projectedPosition = restFoot + stepMovement;
        }

        // Raycast downwards to take slopes into account
        Vector3 rayStart = projectedPosition + Vector3.up * maxStepHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, maxStepHeight*2))      // TODO floor mask
        {
            // Found ground to place foot on at raycast hit point
            return (hit.point, hit.collider);
        }
        else
        {
            // No ground to place foot on => use rest position as a backup
            return (restFoot, null);
        }
    }

    /// <summary>
    /// Place a foot on ground. Says it all.
    /// </summary>
    private void PlaceFootOnGround()
    {
        if (!isLiftingFoot) return;     // Can't place foot on ground if none are lifted
        isLiftingFoot = false;

        var (predictedFootPosition, underFootCollider) = PredictFootPosition();
        if (isRightFootNextToLift)
        {
            leftFootPosition = predictedFootPosition;
        }
        else
        {
            rightFootPosition = predictedFootPosition;
        }

        // Activate colliders at new foot location
        if (underFootCollider != null)
        {
            // Walking on a collider
            TriggerFootstep trigger = underFootCollider.GetComponent<TriggerFootstep>();
            if (trigger != null)
            {
                trigger.OnFootstep(predictedFootPosition);
            }
            // Otherwise, collider is not supposed to be walked on (edge case)
        }
        // Otherwise, no collider found under foot, we should probably do nothing (edge case)
    }

    /// <summary>
    /// Get the floor gameobject that is under the given position
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <returns>The floor object that is under the given position, or null if foot is not on a floor</returns>
    private GameObject getFloorUnderPosition(Vector3 position) {
        Vector3 rayStart = position + distanceUnderFootFloorDetection*Vector3.down;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, distanceUnderFootFloorDetection*2)) {  // TODO floor mask
            return hit.collider.gameObject;
        }
        return null;
    }

    /// <summary>
    /// Walk until at rest again
    /// </summary>
    /// <returns></returns>
    IEnumerator WalkCoroutine()
    {
        isAtRest = false;
        bool isWalking = true;
        while (isWalking)
        {
            LiftFoot();
            yield return new WaitForSeconds(footBackToGroundDelay);     // Wait after lifting to foot

            PlaceFootOnGround();
            yield return new WaitForSeconds(footLiftDelay);     // Wait before lifting the foot again
            if (AreFeetInRestPosition())
            {
                isWalking = false;
            }
        }
        isAtRest = true;
    }

    /// <summary>
    /// Update performed every physics frame
    /// </summary>
    private void FixedUpdate()
    {
        // When at rest, start moving feet when leaving acceptable resting distance
        if (isAtRest)
        {
            if (! AreFeetInRestPosition())
            {
                StartCoroutine(WalkCoroutine());
            }
        }
        leftfoot_debug.transform.position = leftFootPosition;
        rightfoot_debug.transform.position = rightFootPosition;
    }
}
