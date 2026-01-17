using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Anchor : MonoBehaviour
{
    // Comparison tolerances
    const float positionEPS = 0.1f;
    const float rotationEPS = 0.1f;

    // See anchor within editor
    #if UNITY_EDITOR
    public const float tipsSize = 0.2f;

    private void OnDrawGizmosSelected()
    {
        Handles.color = Color.magenta;
        Vector3 position = transform.position;
        float size = transform.lossyScale.x;

        if (size >= 0)
        {
            Handles.ArrowHandleCap(
                0,
                position,
                Quaternion.LookRotation(transform.forward),
                size,
                EventType.Repaint
            );

            Vector3 leftLimit = position - size*transform.right;
            Vector3 rightLimit = position + size*transform.right;
            Handles.DrawLine(leftLimit, rightLimit);

            Handles.DrawLine(
                leftLimit - tipsSize*size*transform.forward,
                leftLimit + tipsSize*size*transform.forward
            );
            Handles.DrawLine(
                rightLimit - tipsSize*size*transform.forward,
                rightLimit + tipsSize*size*transform.forward
            );
        }
    }
    #endif

    /// <summary>
    /// Test whether this anchor is connected to another anchor
    /// </summary>
    /// <param name="other">The other anchor to compare to</param>
    /// <param name="matchScales">Whether to test for scale match</param>
    /// <returns>The anchoring state of this anchor and the given anchor</returns>
    public bool IsAnchoredTo(Anchor other, bool matchScales=true)
    {
        if (matchScales && (Mathf.Abs(transform.lossyScale.x - other.transform.lossyScale.x) > positionEPS))
        {
            return false;
        }
        return Quaternion.Angle(transform.rotation*Quaternion.AngleAxis(180f, Vector3.up), other.transform.rotation) < rotationEPS
            && Vector3.Distance(transform.position, other.transform.position) < positionEPS;
    }
}
