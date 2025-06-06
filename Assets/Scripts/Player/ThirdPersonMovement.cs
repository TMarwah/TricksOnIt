using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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
    // NEW: Wall slide mechanic
    [Tooltip("How fast the player slides down a wall when hanging.")]
    public float wallSlideSpeed = 2f;


    [Header("Air Rotation")]
    public float airFlipSpeed = 360f;
    public AnimationCurve flipSpeedProfile = new AnimationCurve(new Keyframe(0, 1.5f), new Keyframe(0.25f, 1f), new Keyframe(0.5f, 0.8f), new Keyframe(1f, 0.75f));
    private float flipKeyHoldTimer = 0f;

    [Tooltip("Amount of combo points gained per degree of rotation.")]
    public float pointsPerDegree = 100/360f;
    [Tooltip("Maximum angle deviation from upright (in degrees) to successfully land a flip.")]
    [Range(0f, 90f)] public float maxLandingAngleDeviation = 45f;
    [Tooltip("Duration of stumble/stun animation on failed flip landing.")]
    public float stumbleDuration = 1f;
    public AudioClip trickFailSFX;
    public GameObject stumbleVFXPrefab;

    [Header("Trick Settings")]
    public float minTrickHeight = 2.0f;

    [Header("Aiming")]
    public bool isAiming = false;
    public float aimSpeedMultiplier = 0.4f;

    [Header("Dashing")]
    public float dashForce = 50f;
    public float dashDuration = 0.25f;
    public bool isDashing = false;

    public GameObject wallJumpVFXPrefab;
    public GameObject dashVFXPrefab;
    public AudioClip dashSFX;

    [Header("UI")]
    public TextMeshProUGUI bankedPointsText;
    private int potentialMidAirPoints = 0;


    Vector3 velocity;
    public bool isGrounded;
    public bool isSprinting;
    bool isTouchingWall;
    bool isWallSliding = false; // NEW: State for wall sliding

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

    // (Awake and Start methods remain the same...)
    #region Standard Methods
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
        comboMeter = GetComponent<ComboMeter>();
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
    #endregion

    void Update()
    {
        bool previousFrameIsGrounded = isGrounded;
        isGrounded = controller.isGrounded;
        _justLanded = isGrounded && !previousFrameIsGrounded;
        
        // Determine wall contact once per frame
        isTouchingWall = Physics.CheckSphere(wallCheck.position, wallCheckRadius, wallMask);

        // MODIFIED: Handle all air logic in one block
        if (!isGrounded)
        {
            // Check for wall sliding condition
            isWallSliding = isTouchingWall && velocity.y < 0 && !justWallJumped;

            if (isWallSliding)
            {
                // If sliding, clamp the downward velocity to the slide speed
                velocity.y = -wallSlideSpeed;
                // Cancel any flip when you start sliding
                _isPerformingFlip = false; 
                potentialMidAirPoints = 0;
            }
            else
            {
                // If not wall sliding, apply normal gravity
                velocity.y += gravity * Time.deltaTime;
            }
        }
        else // On ground
        {
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
            isWallSliding = false; // Cannot be wall sliding if grounded
            justWallJumped = false;

            velocity.x = Mathf.MoveTowards(velocity.x, 0, groundDeceleration * Time.deltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, 0, groundDeceleration * Time.deltaTime);
        }

        if (playerHealth != null && playerHealth.IsDead())
        {
            // ... (death logic remains the same)
            return;
        }

        inHitStop = HitStopManager.Instance != null && HitStopManager.Instance.IsHitStopActive;

        // MODIFIED: Animator logic is now cleaner
        if (animator)
        {
            animator.SetFloat("airSpeed", velocity.y);
            animator.SetBool("isGrounded", isGrounded);
            // Directly set the "isHanging" parameter based on our new wall slide state
            animator.SetBool("isHanging", isWallSliding);
            if (isWallSliding) Debug.Log("should be wallsliding");
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

        if (wallNormalTimer > 0f)
            wallNormalTimer -= Time.deltaTime;
        else
            lastWallNormal = Vector3.zero;

        // --- All movement, input, and flip logic remains largely the same below ---
        
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
                if (isAiming || !_isPerformingFlip)
                {
                    float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);
                }
                
                float moveAngleForMoveDir = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
                moveDir = Quaternion.Euler(0f, moveAngleForMoveDir, 0f) * Vector3.forward;
            } else {
                 moveDir = transform.TransformDirection(new Vector3(inputDirection.x, 0, inputDirection.z));
            }

            if (isGrounded)
            {
                controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
                if(animator && controller.enabled) animator.SetBool("isWalking", true);
            }
            else
            {
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
            Vector3 currentInputMoveXZ = moveDir.normalized * currentSpeed;
            if (currentInputMoveXZ.sqrMagnitude > 0.01f) {
                 velocity.x = currentInputMoveXZ.x * 0.5f;
                 velocity.z = currentInputMoveXZ.z * 0.5f;
            } else {
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

        if (!isGrounded && !isWallSliding) // Can't flip if you are sliding
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
        
        controller.Move(velocity * Time.deltaTime);
        UpdatePotentialPointsUI();
    }

    // (The rest of your methods: UpdatePotentialPointsUI, FinishFlip, PerformStumble, CanWallJump, etc. remain the same)
    #region Unchanged Methods
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
        "Whoa!", "Oops!", "Wobbly!", "Gah!", "Steady now...", "My ankles!", "Not again!", "Woah there!", "Oof!", "Eep!"
    };

    private IEnumerator PerformStumble()
    {
        Debug.Log("Player is stumbling!");
        animator.SetTrigger("Stumble");
        controller.enabled = false;
        transform.rotation = Quaternion.identity;
        cineCam.GetComponent<CameraEffects>().Shake(0.1f);
        int randomIndex = UnityEngine.Random.Range(0, stumbleTexts.Count);
        BlurbText.Instance.TypeText(stumbleTexts[randomIndex]);
        comboMeter.SpendComboPoint(UnityEngine.Random.Range(0, 6) + 5);
        if (stumbleVFXPrefab != null)
        {
            Instantiate(stumbleVFXPrefab, transform.position, Quaternion.identity);
        }
        AudioSource.PlayClipAtPoint(trickFailSFX, transform.position);

        float elapsedTime = 0f;
        while (elapsedTime < stumbleDuration)
        {
            if (Input.GetKey(KeyCode.LeftShift)) // Check if the player is sprinting
            {
                Debug.Log("Player sprinted, ending stumble early.");
                break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

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
        velocity = jumpDirection * wallJumpHorizontalBoost;
        velocity.y = wallJumpVerticalBoost;
        justWallJumped = true;
        isWallSliding = false; // You are jumping away, not sliding
        _isPerformingFlip = false;
        potentialMidAirPoints = 0;
        airControlMultiplier = boostedAirControlFactor;
        wallNormalTimer = wallNormalResetTime;

        if (wallJumpVFXPrefab != null)
        {
            Instantiate(wallJumpVFXPrefab, wallCheck.position, Quaternion.LookRotation(lastWallNormal));
        }
        if(animator) animator.SetTrigger("JumpTrigger");
        comboMeter.AddComboPoint(5);
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
        velocity.x = 0f;
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
        if (animator) animator.SetBool("isDashing", true);
        float timer = 0f;
        velocity.y = 0;

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);

        GameObject dashVFX = Instantiate(dashVFXPrefab, transform.position, Quaternion.identity);
        dashVFX.transform.SetParent(transform);
        dashVFX.transform.localPosition = Vector3.zero;

        if (dashSFX != null)
        {
            AudioSource.PlayClipAtPoint(dashSFX, transform.position);
        }

        while (timer < dashDuration)
        {
            controller.Move(dashDirection * dashForce * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(WaitUntilNotInsideEnemy());
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
        isDashing = false;
        if (animator) animator.SetBool("isDashing", false);

        Destroy(dashVFX, 1f);
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
    #endregion
}