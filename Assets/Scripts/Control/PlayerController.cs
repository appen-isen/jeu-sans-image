using UnityEngine;
using UnityEngine.EventSystems;

// Handles everything related to player movement
public class PlayerController : MonoBehaviour
{
    [SerializeField] private EarsController ears = null;
    [Header("Vertical rotation")]
    [SerializeField] float minRotation = -90f;
    [SerializeField] float maxRotation = 90f;

    [Header("Player speed")]
    [SerializeField] private float speed = 15;
    [SerializeField] private float sensitivity = 50;
    [SerializeField] private float maxSpeed = 10;

    [Header("Slope handling")]
    [SerializeField] private float maxSlopeAngle = 20f;
    private RaycastHit slopeHit;

    private float playerHeight = 2f;

    private PlayerControlsMap playerControls;
    private Rigidbody rb;

    private Vector3 currentRotation = Vector3.zero;


    // Awake is always called before Start. Often used to initialize variables
    void Awake()
    {
        playerHeight = 2f * transform.localScale.y;
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

    // Rotate player according to input
    void RotatePlayer() {
        // Update player direction. Very basic
        // First, rotate player on y axis
        Vector2 lookInput = playerControls.Player.Look.ReadValue<Vector2>();
        lookInput *= sensitivity * Time.deltaTime;
        currentRotation += new Vector3(-lookInput.y, lookInput.x, 0);
        currentRotation.x = Mathf.Clamp(currentRotation.x, minRotation, maxRotation);

        transform.localEulerAngles = new Vector3(0, currentRotation.y, 0);

        // Then, rotate player's ears on both x and z axes (only available with certain controllers such as AR/VR)
        ears.RotateHead(new Vector3(currentRotation.x, 0, currentRotation.z));
    }

    // Move player according to input
    void MovePlayer() {
        // Update player acceleration, using a force
        Vector2 mvtInput = playerControls.Player.Move.ReadValue<Vector2>();
        Vector3 moveDirection = GetMoveDirection(transform.rotation*new Vector3(mvtInput.x, 0, mvtInput.y));
        rb.AddForce(moveDirection * speed * Time.deltaTime, ForceMode.VelocityChange);

        // Limit max speed.
        // maxSpeed affects only max speed, while rigidbody's linear damping affects both max speed and deceleration
        if (rb.linearVelocity.magnitude > maxSpeed) {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    // Check if player is on a slope
    bool OnSlope() {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.2f)) {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    // Get move direction, whether there is a slope or not
    private Vector3 GetMoveDirection(Vector3 mvtInput) {
        if (OnSlope()) {
            Vector3 mvtDir = Vector3.ProjectOnPlane(mvtInput, slopeHit.normal);
            
            // Limit norm of direction to 1, while allowing lower
            if (mvtDir.magnitude > 1) {
                return mvtDir.normalized;
            }
            return mvtDir;
        }
        return mvtInput;
    }
}
