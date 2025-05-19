using System;
using UnityEngine;
using UnityEngine.VFX; // Import VFX Graph namespace
using System.Collections;

[RequireComponent(typeof(PlayerHealth))]

public class ThirdPersonMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform camTransform;
    public Transform model;
    public Animator animator;

    [Header("Movement")]
    public float speed = 6f;
    public float speedMod = 2f;
    public float airControlFactor = 0.5f;
    public float boostedAirControlFactor = 1.2f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    [Header("Jumping")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    private PlayerHealth playerHealth;
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;
    Vector3 horizontalVelocity;

    [Header("Wall Jump")]
    public Transform wallCheck;
    public float wallCheckRadius = 0.5f;
    public LayerMask wallMask;
    public float wallJumpVerticalBoost = 5f;
    public float wallJumpHorizontalBoost = 5f;

    [Header("Air Rotation")]
    public float airFlipSpeed = 360f;

    [Header("Aiming")]
    public bool isAiming = false;
    public float aimSpeedMultiplier = 0.4f;

    [Header("Dashing")]
    public float dashForce = 15f;
    public float dashDuration = 0.25f;
    private bool isDashing = false;

    // Reference for the VFX Graph
    public GameObject wallJumpVFXPrefab;

    Vector3 velocity;
    public bool isGrounded;
    bool isFlipping;
    public bool isSprinting;
    bool isTouchingWall;

    bool justWallJumped = false;
    float airControlMultiplier;

    Vector3 lastWallNormal = Vector3.zero;
    float wallNormalResetTime = 0.5f;
    float wallNormalTimer = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();

        Animator foundAnimator = GetComponentInChildren<Animator>();
        if (foundAnimator != null)
        {
            animator = foundAnimator;
            model = animator.transform;
        }

        var cineCam = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        if (cineCam != null)
        {
            camTransform = cineCam.transform;
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        isSprinting = false;
        airControlMultiplier = airControlFactor;
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead())
        {
            return;
        }
        isGrounded = controller.isGrounded;
        animator.SetFloat("airSpeed", velocity.y);
        animator.SetBool("isGrounded", isGrounded);

        if (isGrounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;

            velocity.x = 0f;
            velocity.z = 0f;

            justWallJumped = false;
            airControlMultiplier = airControlFactor;

            lastWallNormal = Vector3.zero; // allow re-jumping from same wall later
        }

        isTouchingWall = Physics.CheckSphere(wallCheck.position, wallCheckRadius, wallMask);

        if (wallNormalTimer > 0f)
            wallNormalTimer -= Time.deltaTime;
        else
            lastWallNormal = Vector3.zero;

        //get input from keys
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        float currentSpeed = isGrounded
            ? (isAiming ? speed * aimSpeedMultiplier : (isSprinting ? speed * speedMod : speed))
            : speed * airControlMultiplier;

        Vector3 moveDir = Vector3.zero;
        // Only apply movement values when moving
        if (direction.magnitude >= 0.1f)
        {
            // Convert input direction to local space (relative to character's forward)
            Vector3 localDir = transform.InverseTransformDirection(direction);

            animator.SetFloat("Horizontal", localDir.x);
            animator.SetFloat("Vertical", localDir.z);

            if (!isFlipping && !isAiming)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
            if (isGrounded)
            {
                float moveAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                moveDir = Quaternion.Euler(0f, moveAngle, 0f) * Vector3.forward;
                controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
                animator.SetBool("isWalking", true);
            }
            else
            {
                float moveAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                moveDir = Quaternion.Euler(0f, moveAngle, 0f) * Vector3.forward;
                Vector3 airControl = moveDir.normalized * currentSpeed * Time.deltaTime;
                controller.Move(airControl);
                animator.SetBool("isWalking", false);
            }
        }
        else
        {
            animator.SetFloat("Horizontal", 0f);
            animator.SetFloat("Vertical", 0f);
            animator.SetBool("isWalking", false);
        }
        horizontalVelocity = moveDir.normalized * currentSpeed;

        isTouchingWall = Physics.CheckSphere(wallCheck.position, wallCheckRadius, wallMask);

        if (wallNormalTimer > 0f)
            wallNormalTimer -= Time.deltaTime;
        else
            lastWallNormal = Vector3.zero;

        //jump input check
        if (Input.GetButtonDown("Jump") && isGrounded && !isAiming)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            velocity.x = horizontalVelocity.x * airControlFactor;
            velocity.z = horizontalVelocity.z * airControlFactor * 1.2f;
            animator.SetTrigger("JumpTrigger");
        }
        else if (Input.GetButtonDown("Jump") && isTouchingWall && CanWallJump())
        {
            PerformWallJump();
        }

        isSprinting = isDashing || (Input.GetKey(KeyCode.LeftShift) && isGrounded && !isAiming);
        animator.SetBool("isSprinting", isSprinting);
        animator.SetBool("isDashing", isDashing);

        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);

        if (!isGrounded && !isFlipping)
        {
            if (Input.GetKeyDown(KeyCode.R))
                StartCoroutine(PerformFlip(Vector3.right));
            else if (Input.GetKeyDown(KeyCode.F))
                StartCoroutine(PerformFlip(Vector3.left));
            else if (Input.GetKeyDown(KeyCode.Q))
                StartCoroutine(PerformFlip(Vector3.up));
            else if (Input.GetKeyDown(KeyCode.E))
                StartCoroutine(PerformFlip(Vector3.down));
        }
    }

    private bool CanWallJump()
    {
        Vector3 currentWallNormal = (transform.position - wallCheck.position).normalized;

        // Compare against previous wall normal
        return lastWallNormal == Vector3.zero ||
               Vector3.Angle(currentWallNormal, lastWallNormal) > 15f;
    }

    private void PerformWallJump()
    {
        Vector3 wallNormal = (transform.position - wallCheck.position).normalized;
        Vector3 jumpDirection = (wallNormal + Vector3.up).normalized;

        velocity = jumpDirection * wallJumpHorizontalBoost;
        velocity.y = wallJumpVerticalBoost;

        justWallJumped = true;
        airControlMultiplier = boostedAirControlFactor;

        lastWallNormal = wallNormal;
        wallNormalTimer = wallNormalResetTime;

        // Instantiate VFX Graph at the wall jump position
        if (wallJumpVFXPrefab != null)
        {
            Instantiate(wallJumpVFXPrefab, transform.position, Quaternion.identity);
        }

        Debug.Log("Wall Jump performed!");
    }

    IEnumerator PerformFlip(Vector3 localAxis)
    {
        isFlipping = true;
        float rotated = 0f;

        while (rotated < 360f)
        {
            float rotationPerFrame = airFlipSpeed * Time.deltaTime;
            transform.Rotate(localAxis * rotationPerFrame, Space.Self);
            rotated += rotationPerFrame;
            yield return null;
        }

        transform.Rotate(localAxis * (360f - rotated), Space.Self);
        isFlipping = false;
    }

    public void PlungeDownward(float force)
    {
        velocity.y = -Mathf.Abs(force);
    }

    public IEnumerator DashForward()
    {
        isDashing = true;
        float timer = 0f;
        Vector3 dashDirection = transform.forward;

        // Ignore collisions with enemies
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);

        while (timer < dashDuration)
        {
            isGrounded = true;

            // Move player forward
            controller.Move(dashDirection * dashForce * Time.deltaTime);

            // Smoothly interpolate model rotation and position
            float t = timer / dashDuration;

            timer += Time.deltaTime;
            yield return null;
        }

        // Wait until no longer overlapping enemies
        yield return StartCoroutine(WaitUntilNotInsideEnemy());

        // Re-enable collisions
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
        isDashing = false;
    }

    private IEnumerator WaitUntilNotInsideEnemy()
    {
        // Define a small overlap radius; tune if needed
        float checkRadius = 0.5f;
        LayerMask enemyMask = LayerMask.GetMask("Enemy");

        // Wait until we're no longer overlapping any enemy
        while (Physics.CheckSphere(transform.position, checkRadius, enemyMask))
        {
            yield return null;
        }
    }
}