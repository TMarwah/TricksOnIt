using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Required for TextMeshProUGUI

[RequireComponent(typeof(PlayerHealth))]
public class ThirdPersonMovement : MonoBehaviour
{
    private CharacterController controller;
    private Transform camTransform;
    private Unity.Cinemachine.CinemachineCamera cineCam;
    private PlayerHealth playerHealth;
    private Transform model;
    private Animator animator;
    private ComboMeter comboMeter;

    [Header("Movement")]
    public float speed = 6f;
    public float speedMod = 2f;
    public float airControlFactor = 0.5f;
    public float boostedAirControlFactor = 1.2f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    // NEW: Ground deceleration for landing momentum
    [Tooltip("How quickly horizontal air momentum is lost when grounded. Higher values mean faster stop.")]
    public float groundDeceleration = 15f;


    [Header("Jumping")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
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
    public AnimationCurve flipSpeedProfile = new AnimationCurve(new Keyframe(0, 1.5f), new Keyframe(0.25f, 1f), new Keyframe(0.5f, 0.8f), new Keyframe(1f, 0.75f));
    private float flipKeyHoldTimer = 0f;

    [Tooltip("Amount of combo points gained per 360-degree rotation.")]
    public float pointsPerDegree = 10/360f;
    [Tooltip("Maximum angle deviation from upright (in degrees) to successfully land a flip.")]
    [Range(0f, 90f)] public float maxLandingAngleDeviation = 45f;
    [Tooltip("Duration of stumble/stun animation on failed flip landing.")]
    public float stumbleDuration = 0.5f;

    [Header("Trick Settings")]
    public float minTrickHeight = 2.0f;

    [Header("Aiming")]
    public bool isAiming = false;
    public float aimSpeedMultiplier = 0.4f;

    [Header("Dashing")]
    public float dashForce = 15f;
    public float dashDuration = 0.25f;
    private bool isDashing = false;

    public GameObject wallJumpVFXPrefab;

    [Header("UI")]
    public TextMeshProUGUI bankedPointsText;
    private int potentialMidAirPoints = 0;


    Vector3 velocity;
    public bool isGrounded;
    public bool isSprinting;
    bool isTouchingWall;

    bool justWallJumped = false;
    float airControlMultiplier;

    Vector3 lastWallNormal = Vector3.zero;
    float wallNormalResetTime = 0.5f;
    float wallNormalTimer = 0f;
    bool inHitStop = false;

    private bool _isPerformingFlip = false;
    private Vector3 _currentFlipAxis = Vector3.zero;
    private float _accumulatedFlipAngle = 0f;
    private bool _justLanded = false;
    private Vector3 _initiatedFlipTypeAxis = Vector3.zero;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();

        Animator foundAnimator = GetComponent<Animator>();
        if (foundAnimator != null)
        {
            animator = foundAnimator;
            model = animator.transform;
        }
        else
        {
            Debug.LogError("ThirdPersonMovement: Animator component not found.", this);
        }

        cineCam = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        if (cineCam != null)
        {
            camTransform = cineCam.transform;
        }
        else
        {
            Debug.LogWarning("ThirdPersonMovement: CinemachineCamera not found. Falling back to main camera if available.", this);
            if (Camera.main != null) {
                camTransform = Camera.main.transform;
            } else {
                Debug.LogError("ThirdPersonMovement: No CinemachineCamera or Main Camera found. Camera-relative controls will fail.", this);
            }
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        isSprinting = false;
        airControlMultiplier = airControlFactor;
        comboMeter = FindObjectOfType<ComboMeter>();
        if (comboMeter == null)
        {
            Debug.LogWarning("ThirdPersonMovement: ComboMeter not found. Points will not be awarded.", this);
        }

        if (flipSpeedProfile == null || flipSpeedProfile.keys.Length == 0)
        {
            Debug.LogWarning("ThirdPersonMovement: flipSpeedProfile AnimationCurve is not set. Defaulting to constant speed.", this);
            flipSpeedProfile = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        }

        if (bankedPointsText != null)
        {
            bankedPointsText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ThirdPersonMovement: Banked Points Text UI is not assigned.", this);
        }
    }

    void Update()
    {
        bool previousFrameIsGrounded = isGrounded;
        isGrounded = controller.isGrounded;
        _justLanded = isGrounded && !previousFrameIsGrounded;

        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else // On ground
        {
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
            justWallJumped = false;

            // NEW: Dampen horizontal air momentum when grounded
            velocity.x = Mathf.MoveTowards(velocity.x, 0, groundDeceleration * Time.deltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, 0, groundDeceleration * Time.deltaTime);
        }

        if (playerHealth != null && playerHealth.IsDead())
        {
            velocity = Vector3.Lerp(velocity, new Vector3(0, velocity.y, 0), 0.2f);
            if(animator) {
                animator.SetBool("isWalking", false);
                animator.SetBool("isSprinting", false);
                animator.SetBool("isDashing", false);
            }
            controller.Move(velocity * Time.deltaTime);
            return;
        }

        inHitStop = HitStopManager.Instance != null && HitStopManager.Instance.IsHitStopActive;
        if(animator) {
            animator.SetFloat("airSpeed", velocity.y);
            animator.SetBool("isGrounded", isGrounded);
        }

        if (_justLanded)
        {
            if (_isPerformingFlip)
            {
                FinishFlip();
            }
            _isPerformingFlip = false;
            _accumulatedFlipAngle = 0f;
            _currentFlipAxis = Vector3.zero;
            _initiatedFlipTypeAxis = Vector3.zero;
            flipKeyHoldTimer = 0f;
            potentialMidAirPoints = 0;
        }

        if (isGrounded)
        {
            airControlMultiplier = airControlFactor;
            lastWallNormal = Vector3.zero;
            potentialMidAirPoints = 0;
        }

        isTouchingWall = Physics.CheckSphere(wallCheck.position, wallCheckRadius, wallMask);

        if (wallNormalTimer > 0f)
            wallNormalTimer -= Time.deltaTime;
        else
            lastWallNormal = Vector3.zero;

        if (animator != null)
        {
            bool calculateIsHanging = false;
            if (!isGrounded && isTouchingWall && !justWallJumped && velocity.y < 0.2f)
            {
                calculateIsHanging = true;
            }
            animator.SetBool("isHanging", calculateIsHanging);
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        float currentSpeed = isGrounded
            ? (isAiming ? speed * aimSpeedMultiplier : (isSprinting ? speed * speedMod : speed))
            : speed * airControlMultiplier;

        Vector3 moveDir = Vector3.zero;
        if (inputDirection.magnitude >= 0.1f && !inHitStop)
        {
            if (animator) {
                Vector3 localDir = transform.InverseTransformDirection(inputDirection);
                animator.SetFloat("Horizontal", localDir.x);
                animator.SetFloat("Vertical", localDir.z);
            }

            if (camTransform != null)
            {
                // MODIFIED: Player-controlled yaw rotation.
                // Allow if aiming OR if not aiming AND a flip is NOT initiated.
                // If a flip is initiated (_isPerformingFlip == true), flip mechanics control rotation.
                if (isAiming || !_isPerformingFlip)
                {
                    float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                }
                
                // moveDir is always calculated based on camera for consistent input response for movement force
                float moveAngleForMoveDir = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                moveDir = Quaternion.Euler(0f, moveAngleForMoveDir, 0f) * Vector3.forward;
            } else {
                 moveDir = transform.TransformDirection(new Vector3(inputDirection.x, 0, inputDirection.z));
            }

            if (isGrounded)
            {
                // Apply input-based movement. The 'velocity' vector's x/z is dampened above.
                controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
                if(animator) animator.SetBool("isWalking", true);
            }
            else // In Air
            {
                // Apply air control force.
                Vector3 airControl = moveDir.normalized * currentSpeed * Time.deltaTime;
                controller.Move(airControl);
                if(animator) animator.SetBool("isWalking", false);
            }
        }
        else
        {
            if(animator) {
                animator.SetFloat("Horizontal", 0f);
                animator.SetFloat("Vertical", 0f);
                animator.SetBool("isWalking", false);
            }
        }
        horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);

        if (Input.GetButtonDown("Jump") && isGrounded && !isAiming)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            // Preserve some horizontal momentum for the jump based on player's movement just before jumping
            // Note: 'horizontalVelocity' is from controller.velocity, so it reflects actual recent movement.
            // The 'velocity.x/z' from previous airborne actions is now being dampened when grounded.
            Vector3 currentInputMoveXZ = moveDir.normalized * currentSpeed; // Get XZ speed from current input if any
            if (currentInputMoveXZ.sqrMagnitude > 0.01f) { // If there's current ground input
                 velocity.x = currentInputMoveXZ.x * 0.5f; // Blend some of current input's direction/speed
                 velocity.z = currentInputMoveXZ.z * 0.5f;
            } else { // If no input, use a bit of the last frame's controller velocity
                 velocity.x = horizontalVelocity.x * 0.8f;
                 velocity.z = horizontalVelocity.z * 0.8f;
            }

            if(animator) animator.SetTrigger("JumpTrigger");
        }
        else if (Input.GetButtonDown("Jump") && isTouchingWall && CanWallJump())
        {
            PerformWallJump();
        }

        isSprinting = isDashing || (Input.GetKey(KeyCode.LeftShift) && isGrounded && !isAiming);
        if(animator) {
            animator.SetBool("isSprinting", isSprinting);
        }

        if (!isGrounded)
        {
            if (IsHighEnoughForTrick() && !_isPerformingFlip)
            {
                Vector3 axisToInitiate = Vector3.zero;
                if (Input.GetKeyDown(KeyCode.R)) axisToInitiate = Vector3.right;
                else if (Input.GetKeyDown(KeyCode.F)) axisToInitiate = Vector3.left;
                else if (Input.GetKeyDown(KeyCode.Q)) axisToInitiate = Vector3.up;
                else if (Input.GetKeyDown(KeyCode.E)) axisToInitiate = Vector3.down;

                if (axisToInitiate != Vector3.zero)
                {
                    _isPerformingFlip = true;
                    _initiatedFlipTypeAxis = axisToInitiate;
                    _currentFlipAxis = axisToInitiate;
                    _accumulatedFlipAngle = 0f;
                    flipKeyHoldTimer = 0f;
                    potentialMidAirPoints = 0;
                }
            }

            if (_isPerformingFlip)
            {
                Vector3 keyHeldNowAxis = Vector3.zero;
                if (Input.GetKey(KeyCode.R)) keyHeldNowAxis = Vector3.right;
                else if (Input.GetKey(KeyCode.F)) keyHeldNowAxis = Vector3.left;
                else if (Input.GetKey(KeyCode.Q)) keyHeldNowAxis = Vector3.up;
                else if (Input.GetKey(KeyCode.E)) keyHeldNowAxis = Vector3.down;

                _currentFlipAxis = keyHeldNowAxis;

                if (_currentFlipAxis != Vector3.zero)
                {
                    flipKeyHoldTimer += Time.deltaTime;
                    float currentSpeedMultiplier = (flipSpeedProfile != null && flipSpeedProfile.keys.Length > 0)
                                                   ? flipSpeedProfile.Evaluate(flipKeyHoldTimer) : 1f;
                    float rotationPerFrame = airFlipSpeed * currentSpeedMultiplier * Time.deltaTime;
                    transform.Rotate(_currentFlipAxis * rotationPerFrame, Space.Self);
                    _accumulatedFlipAngle += rotationPerFrame;
                    potentialMidAirPoints = Mathf.FloorToInt(_accumulatedFlipAngle * pointsPerDegree);
                }
            }
        }
        else
        {
             _isPerformingFlip = false;
             potentialMidAirPoints = 0;
        }

        // This applies gravity, jump/fall velocity, and any (now dampened on ground) x/z air momentum
        controller.Move(velocity * Time.deltaTime);

        UpdatePotentialPointsUI();
    }

    void UpdatePotentialPointsUI()
    {
        if (bankedPointsText != null)
        {
            if (_isPerformingFlip && !isGrounded && potentialMidAirPoints > 0)
            {
                bankedPointsText.text = "+" + potentialMidAirPoints.ToString();
                bankedPointsText.gameObject.SetActive(true);
            }
            else
            {
                bankedPointsText.gameObject.SetActive(false);
            }
        }
    }

    private void FinishFlip()
    {
        bool isPitchOrRollFlip = _initiatedFlipTypeAxis == Vector3.right || _initiatedFlipTypeAxis == Vector3.left;

        if (_initiatedFlipTypeAxis == Vector3.zero && _accumulatedFlipAngle < 10f) {
             Debug.Log("Negligible rotation, no flip evaluated.");
             return;
        }
        int pointsGained = potentialMidAirPoints;

        if (isPitchOrRollFlip)
        {
            float uprightness = Vector3.Dot(transform.up, Vector3.up);
            float angleDeviation = Mathf.Acos(Mathf.Clamp(uprightness, -1f, 1f)) * Mathf.Rad2Deg;

            if (angleDeviation <= maxLandingAngleDeviation)
            {
                if (pointsGained > 0 && comboMeter != null)
                {
                    comboMeter.AddComboPoint(pointsGained);
                    Debug.Log($"Pitch/Roll Flip Success! Added {pointsGained} combo points. Total angle: {_accumulatedFlipAngle:F1}, Landing Angle Dev: {angleDeviation:F1}");
                }
                else if (_accumulatedFlipAngle > 10f)
                {
                    Debug.Log($"Pitch/Roll Flip landed okay but not enough rotation for points. Angle: {_accumulatedFlipAngle:F1}");
                }
            }
            else
            {
                Debug.LogWarning($"Pitch/Roll Flip Failed! Landed at {angleDeviation:F1} degrees deviation. Angle: {_accumulatedFlipAngle:F1}. No points awarded.");
                if (comboMeter != null) { /* Optional penalty */ }
                StartCoroutine(PerformStumble());
            }
        }
        else
        {
            if (pointsGained > 0 && comboMeter != null)
            {
                comboMeter.AddComboPoint(pointsGained);
                Debug.Log($"Yaw/Spin Flip Success! Added {pointsGained} combo points. Total angle: {_accumulatedFlipAngle:F1}");
            }
            else if (_accumulatedFlipAngle > 10f)
            {
                 Debug.Log($"Yaw/Spin Flip attempted but not enough rotation for points. Angle: {_accumulatedFlipAngle:F1}");
            }
        }
    }

    private List<string> stumbleTexts = new List<string>() {
        "Whoa!",
        "Oops!",
        "Wobbly!",
        "Gah!",
        "Tripped!",
        "Steady now...",
        "My ankles!",
        "Not again!",
        "Close one!",
        "Woah there!",
        "Oof!",
        "Eep!"
    };

    private IEnumerator PerformStumble()
    {
        Debug.Log("Player is stumbling!");
        controller.enabled = false;
        transform.rotation = Quaternion.identity;
        cineCam.GetComponent<CameraEffects>().Shake(0.1f);
        int randomIndex = UnityEngine.Random.Range(0, stumbleTexts.Count);
        BlurbText.Instance.TypeText(stumbleTexts[randomIndex]);
        comboMeter.SpendComboPoint(UnityEngine.Random.Range(0, 6) + 5);
        yield return new WaitForSeconds(stumbleDuration);
        controller.enabled = true;
        Debug.Log("Player recovered from stumble.");
    }

    private bool CanWallJump()
    {
        Vector3 directionToWallCheck = (wallCheck.position - controller.bounds.center).normalized;
        if (directionToWallCheck.sqrMagnitude < 0.01f) directionToWallCheck = model.forward; // Fallback if wallCheck is at center

        if (Physics.Raycast(controller.bounds.center, directionToWallCheck, out RaycastHit hit, controller.radius + wallCheckRadius + 0.1f, wallMask))
        {
            Vector3 currentWallNormal = hit.normal;
             if (Vector3.Angle(currentWallNormal, lastWallNormal) > 15f || lastWallNormal == Vector3.zero) {
                lastWallNormal = currentWallNormal;
                return true;
            }
            return false;
        }
        return false;
    }

    private void PerformWallJump()
    {
        Vector3 jumpDirection = (lastWallNormal + Vector3.up * 1.2f).normalized;
        velocity = jumpDirection * wallJumpHorizontalBoost; // This sets velocity.x and velocity.z
        velocity.y = wallJumpVerticalBoost;
        justWallJumped = true;
        _isPerformingFlip = false; // Cancel any flip attempt on wall jump
        potentialMidAirPoints = 0; // Clear potential points
        airControlMultiplier = boostedAirControlFactor;
        wallNormalTimer = wallNormalResetTime;

        if (wallJumpVFXPrefab != null)
        {
            Instantiate(wallJumpVFXPrefab, wallCheck.position, Quaternion.LookRotation(lastWallNormal));
        }
        if(animator) animator.SetTrigger("JumpTrigger");
        Debug.Log("Wall Jump performed! Normal: " + lastWallNormal);
    }

    private bool IsHighEnoughForTrick()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, minTrickHeight + 0.5f, groundMask))
        {
            return hit.distance > minTrickHeight;
        }
        return true;
    }

    public IEnumerator PlungeDownward(float force)
    {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
        velocity.y = -Mathf.Abs(force);
        velocity.x = 0f; // Stop horizontal movement during plunge
        velocity.z = 0f;
        _isPerformingFlip = false;
        flipKeyHoldTimer = 0f;
        potentialMidAirPoints = 0;

        PlayerAttack playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            yield return new WaitUntil(() => playerAttack.didPlungeAttack);
        }
        else
        {
            yield return new WaitUntil(() => isGrounded);
        }
        
        yield return new WaitForSeconds(0.2f); 
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
    }

    public IEnumerator DashForward()
    {
        Vector3 dashDirection = model.forward;
        isDashing = true;
        if(animator) animator.SetBool("isDashing", true);
        float timer = 0f;
        velocity.y = 0;

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);

        while (timer < dashDuration)
        {
            controller.Move(dashDirection * dashForce * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
        
        yield return StartCoroutine(WaitUntilNotInsideEnemy());
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
        isDashing = false;
        if(animator) animator.SetBool("isDashing", false);
    }

    private IEnumerator WaitUntilNotInsideEnemy()
    {
        float checkRadius = controller.radius + 0.1f;
        float maxWaitTime = 0.5f;
        LayerMask enemyMask = LayerMask.GetMask("Enemy");
        float timer = 0f;
        Vector3 exitDir = model.forward;

        while (Physics.CheckSphere(transform.position, checkRadius, enemyMask))
        {
            if (timer > maxWaitTime)
            {
                Debug.LogWarning("WaitUntilNotInsideEnemy timed out.");
                yield break;
            }
            controller.Move(exitDir * 0.5f * Time.deltaTime);
            yield return null;
            timer += Time.deltaTime;
        }
    }
}