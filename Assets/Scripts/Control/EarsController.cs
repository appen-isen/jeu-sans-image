using UnityEngine;

public class EarsController : MonoBehaviour
{
    public void RotateHead(Vector3 rotation) {
        transform.localEulerAngles = new Vector3(rotation.x, 0, rotation.z);
        // More advanced rotation would require another rotation center
    }
}