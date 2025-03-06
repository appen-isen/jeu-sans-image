using UnityEngine;

// Handles everything related to player movement
public class PlayerController : MonoBehaviour
{
    [SerializeField] private EarsController ears = null;

    [SerializeField] private float speed = 15;
    [SerializeField] private float sensitivity = 50;
    [SerializeField] private float maxSpeed = 10;

    private PlayerControlsMap playerControls;
    private Rigidbody rb;

    private Vector3 currentRotation = Vector3.zero;

    // Awake is always called before Start. Often used to initialize variables
    void Awake()
    {
        playerControls = new PlayerControlsMap();
        rb = GetComponent<Rigidbody>();

        if (ears == null) {     // If not assigned within editor
            Debug.LogError("ears attribute of " + name + " should be assigned within editor. (ears object should be a child of " + name + ")");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // Often used to initialize variables which are dependent to other scripts/components variables
    void Start()
    {
        
    }

    void OnEnable()
    {
        playerControls.Enable();
    }

    void OnDisable()
    {
        playerControls.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        RotatePlayer();
        MovePlayer();
        
    }

    void RotatePlayer() {
        // Update player direction. Very basic
        // First, rotate player on y axis
        Vector2 lookInput = playerControls.Player.Look.ReadValue<Vector2>();
        lookInput *= sensitivity * Time.deltaTime;
        currentRotation += new Vector3(Mathf.Clamp(-lookInput.y, -90, 90), lookInput.x, 0);
        transform.localEulerAngles = new Vector3(0, currentRotation.y, 0);

        // Then, rotate player's ears on both x and z axes (only available with certain controllers such as AR/VR)
        ears.RotateHead(new Vector3(currentRotation.x, 0, currentRotation.z));
    }

    void MovePlayer() {
        // Update player acceleration, using a force
        Vector2 mvtInput = playerControls.Player.Move.ReadValue<Vector2>();
        mvtInput *= speed * Time.deltaTime;
        rb.AddForce(transform.rotation * new Vector3(mvtInput.x, 0, mvtInput.y), ForceMode.VelocityChange);

        // Limit max speed.
        // maxSpeed affects only max speed, while rigidbody's linear damping affects both max speed and deceleration
        if (rb.linearVelocity.magnitude > maxSpeed) {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
}
