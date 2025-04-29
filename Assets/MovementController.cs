using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float trickRotationSpeed = 360f;

    private Rigidbody rb;
    private bool isGrounded;
    private bool isTricking = false;
    private bool isRecovering = false; // To track if the bean is recovering
    private Vector3 flipAxis; // Store the axis of rotation for the current flip
    private bool isFlipInitialized = false; // Track if the flip has been initialized
    private Vector3 jumpCameraForward; // Store the camera's forward direction at the time of the jump

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Hide the cursor and lock it to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Exit the game
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in the editor
            #else
            Application.Quit(); // Quit the application in builds
            #endif
        }

        if (!isRecovering) // Allow movement and tricks unless recovering
        {
            HandleMovement(); // Always allow movement
            HandleJump();

            if (!isTricking && isGrounded) // Align with the camera only when grounded and not flipping
            {
                AlignWithCamera();
            }
            else if (!isGrounded) // Handle tricks only when in the air
            {
                HandleTrick();
            }
        }
    }

    private void AlignWithCamera()
    {
        // Get the camera's forward direction
        Camera currentCamera = Camera.main; // Always fetch the current camera
        Vector3 cameraForward = currentCamera.transform.forward;
        cameraForward.y = 0; // Flatten the forward direction
        cameraForward.Normalize();

        // Align the bean to face the camera's forward direction
        Quaternion cameraAlignment = Quaternion.LookRotation(cameraForward, Vector3.up);
        rb.MoveRotation(cameraAlignment);
    }

    private void HandleMovement()
    {
        // Get input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Get the camera's forward and right directions
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // Flatten the directions to ignore vertical movement
        cameraForward.y = 0;
        cameraRight.y = 0;

        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate movement direction relative to the camera
        Vector3 move = (cameraForward * moveZ + cameraRight * moveX).normalized * moveSpeed * Time.deltaTime;

        // Apply movement using Rigidbody
        Vector3 newPosition = rb.position + move;
        rb.MovePosition(newPosition);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // Capture the camera's forward direction at the time of the jump
            Camera currentCamera = Camera.main;
            jumpCameraForward = currentCamera.transform.forward;

            // Flatten the forward direction to ignore vertical movement
            jumpCameraForward.y = 0;
            jumpCameraForward.Normalize();

            // Apply the jump force
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    private void HandleTrick()
    {
        if (!isGrounded)
        {
            // Get the camera's forward direction dynamically
            Camera currentCamera = Camera.main;
            Vector3 cameraForward = currentCamera.transform.forward;

            // Flatten the forward direction to ignore vertical movement
            cameraForward.y = 0;
            cameraForward.Normalize();

            // Perform the flip only while the key is held down
            if (Input.GetKey(KeyCode.R)) // Front flip
            {
                // Set the flip axis for a front flip (away from the camera)
                flipAxis = Vector3.Cross(Vector3.up, cameraForward); // Rotate forward relative to the camera's right axis

                // Perform the flip
                Quaternion flipRotation = Quaternion.AngleAxis(trickRotationSpeed * Time.deltaTime, flipAxis);
                rb.MoveRotation(rb.rotation * flipRotation);
            }
            else if (Input.GetKey(KeyCode.F)) // Back flip
            {
                // Set the flip axis for a back flip (towards the camera)
                flipAxis = Vector3.Cross(cameraForward, Vector3.up); // Rotate backward relative to the camera's left axis

                // Perform the flip
                Quaternion flipRotation = Quaternion.AngleAxis(trickRotationSpeed * Time.deltaTime, flipAxis);
                rb.MoveRotation(rb.rotation * flipRotation);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;

            // Check if the bean is off-angle
            float angle = Vector3.Angle(Vector3.up, transform.up);
            if (angle > 30f) // If the bean is tilted more than 30 degrees
            {
                StartCoroutine(RecoverFromFall());
            }
            else
            {
                AlignWithCamera(); // Align the bean with the camera's forward direction

                // Lock the bean upright
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                isTricking = false; // Trick is complete
                isFlipInitialized = false; // Reset flip initialization
            }
        }
    }

    private IEnumerator RecoverFromFall()
    {
        isRecovering = true; // Disable user control
        rb.constraints = RigidbodyConstraints.None; // Allow full physics simulation

        yield return new WaitForSeconds(2f); // Wait for 2 seconds

        AlignWithCamera(); // Align the bean with the camera's forward direction

        // Lock the bean upright and allow movement
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        isRecovering = false; // Return control to the player
        isFlipInitialized = false; // Reset flip initialization
    }
}