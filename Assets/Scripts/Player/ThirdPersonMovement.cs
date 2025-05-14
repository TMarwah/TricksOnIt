using System;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerHealth))]

public class ThirdPersonMovement : MonoBehaviour
{


    public CharacterController controller;
    public Transform camTransform;

    [Header("Movement")] //player movement params
    public float speed = 6f;
    public float speedMod = 2f;
    public float airControlFactor = 0.5f;  // Reduced movement speed in the air
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    [Header("Jumping")] //jump params
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    private PlayerHealth playerHealth;
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Air Rotation")] //flip params
    public float airFlipSpeed = 360f; // degrees per second

    Vector3 velocity;
    bool isGrounded;
    bool isFlipping;
    bool isSprinting;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        isSprinting = false;
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead())
        {
            return;
        }
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        //get input from keys
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        //change in-air movement by modifier
        float currentSpeed = isGrounded ? (isSprinting ? speed * speedMod : speed) : speed * airControlFactor;

        //only rotate to cam if not tricking
        if (direction.magnitude >= 0.1f && !isFlipping)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        //char movement
        if (direction.magnitude >= 0.1f)
        {
            float moveAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0f, moveAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        //jump input check
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (Input.GetKey(KeyCode.LeftShift) && isGrounded)
        {
            isSprinting = true;
        }
        else {
            isSprinting = false;
        }

        //apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        //flip manager, only in air
        if (!isGrounded && !isFlipping)
        {
            if (Input.GetKeyDown(KeyCode.R))
                StartCoroutine(PerformFlip(Vector3.right));  //front flip

            else if (Input.GetKeyDown(KeyCode.F))
                StartCoroutine(PerformFlip(Vector3.left));   //back flip

            else if (Input.GetKeyDown(KeyCode.Q))
                StartCoroutine(PerformFlip(Vector3.up));     //left roll

            else if (Input.GetKeyDown(KeyCode.E))
                StartCoroutine(PerformFlip(Vector3.down));   //right roll
        }
    }

    IEnumerator PerformFlip(Vector3 localAxis)
    {
        isFlipping = true;
        float rotated = 0f;
        float rotationPerFrame;

        while (rotated < 360f)
        {
            rotationPerFrame = airFlipSpeed * Time.deltaTime;
            transform.Rotate(localAxis * rotationPerFrame, Space.Self);
            rotated += rotationPerFrame;
            yield return null;
        }

        //snap rot
        transform.Rotate(localAxis * (360f - rotated), Space.Self);
        isFlipping = false;
    }
}
