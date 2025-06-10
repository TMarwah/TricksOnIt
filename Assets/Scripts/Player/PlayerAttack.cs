using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(ThirdPersonMovement))]
public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private CameraEffects camEffects;
    private ThirdPersonMovement playerController;
    private ComboMeter comboMeter;

    [Header("Light Attack")]
    public float attackRange = 1f;
    public float attackAngle = 90f;
    public float knockbackForce = 0f;
    public float lightAttackCooldown = 0.3f;
    private float lightAttackTimer = 0f;

    [Header("Ranged Attack")]
    public float rangedAttackRange = 3f;
    public float rangedAttackAngle = 180f;
    public float rangedKnockbackForce = 0.05f;
    public float rangedSplashRadius = 0.1f;
    public int pelletsPerShot = 3;
    public float rangedAttackCooldown = 0.5f;
    private float rangedAttackCooldownTimer = 0f;

    public bool didPlungeAttack = false;
    private bool wasGroundedLastFrame = true;
    public GameObject shootVFXPrefab;

    [Header("Plunge")]
    public float plungingAttackForce = 10f;
    // NEW: Variables to control Plunge Attack scaling
    [Tooltip("The base radius of the plunge attack at the lowest combo rank.")]
    public float plungeBaseRange = 2.0f;
    [Tooltip("How much radius is added to the plunge attack for each combo rank above 'F'.")]
    public float plungeBonusRangePerRank = 0.75f;

    [Header("VFX")]
    public GameObject hitSparkPrefab;
    public GameObject plungeAttackVFXPrefab;

    [Header("Audio VFX")]
    public AudioClip pelletShootSound;
    [Range(0f, 1f)]
    public float pelletShootVolume = 0.8f;
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float hitVolume = 0.7f;
    public AudioClip plungeImpactSound;
    [Range(0f, 1f)]
    public float plungeImpactVolume = 1.0f;

    private bool isAiming = false;

    void Awake()
    {
        playerController = GetComponent<ThirdPersonMovement>();
        animator = GetComponent<Animator>();

        var cineCam = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        if (cineCam != null)
        {
            camEffects = cineCam.GetComponent<CameraEffects>();
        }
        comboMeter = FindObjectOfType<ComboMeter>();
    }

    void Update()
    {
        if (GetComponent<PlayerHealth>().IsDead()) return;

        // Decrement timers
        rangedAttackCooldownTimer -= Time.deltaTime;
        lightAttackTimer -= Time.deltaTime;

        // --- RANGED ATTACK ON RIGHT MOUSE BUTTON ---
        bool isHoldingAim = Input.GetMouseButton(1);

        isAiming = playerController.isGrounded && isHoldingAim;
        playerController.isAiming = isAiming;
        animator.SetBool("isAiming", isAiming);

        if (isAiming && rangedAttackCooldownTimer <= 0f)
        {
            if (comboMeter != null && comboMeter.HasComboPoints())
            {
                comboMeter.SpendComboPoint();
                StartCoroutine(PerformRangedAttack());
                rangedAttackCooldownTimer = rangedAttackCooldown;
            }
        }

        // --- LIGHT ATTACK & MOVEMENT ATTACKS (Left Mouse) ---
        if (Input.GetMouseButtonDown(0) && lightAttackTimer <= 0f)
        {
            if (playerController.isGrounded)
            {
                PerformLightAttack(); // Handles both dash and standard melee
            }
            else
            {
                // MODIFIED: Plunge attack no longer costs combo points.
                StartCoroutine(PerformPlungingAttack());
            }
            lightAttackTimer = lightAttackCooldown;
        }


        // --- PLUNGE ATTACK LANDING ---
        if (didPlungeAttack && playerController.isGrounded && !wasGroundedLastFrame)
        {
            if (plungeAttackVFXPrefab != null)
            {
                Vector3 vfxPos = transform.position;
                vfxPos.y -= 1f;
                Instantiate(plungeAttackVFXPrefab, vfxPos, Quaternion.identity);
            }
            if (plungeImpactSound != null)
            {
                AudioSource.PlayClipAtPoint(plungeImpactSound, transform.position, plungeImpactVolume);
            }
            didPlungeAttack = false;
        }

        wasGroundedLastFrame = playerController.isGrounded;
    }

    IEnumerator PerformRangedAttack()
    {
        animator.SetTrigger("Shoot");

        for (int i = 0; i < pelletsPerShot; i++)
        {
            Transform enemy = FindEnemyInSprayCone(rangedAttackRange, rangedAttackAngle);
            if (enemy != null)
            {
                AudioSource.PlayClipAtPoint(pelletShootSound, transform.position, pelletShootVolume);
                Vector3 vfxPosition = transform.position + transform.forward * 0.5f;
                Instantiate(shootVFXPrefab, vfxPosition, Quaternion.identity);
                camEffects.Shake(0.01f);
                DealDamageToSingleEnemy(enemy, rangedKnockbackForce, 5f);
                DealSplashDamageAround(enemy.position, rangedSplashRadius, rangedKnockbackForce * 0.5f);
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Unchanged methods (FindEnemyInSprayCone, DealDamageToSingleEnemy, etc.) go here...
    #region Unchanged Helper Methods
    Transform FindEnemyInSprayCone(float range, float angle)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        List<Transform> validEnemies = new List<Transform>();

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Vector3 toEnemy = hit.transform.position - transform.position;
                float currentAngle = Vector3.Angle(transform.forward, toEnemy);

                if (currentAngle <= angle / 2f)
                {
                    validEnemies.Add(hit.transform);
                }
            }
        }

        if (validEnemies.Count > 0)
        {
            return validEnemies[Random.Range(0, validEnemies.Count)];
        }

        return null;
    }

    void DealDamageToSingleEnemy(Transform enemy, float force, float damage)
    {
        Vector3 toEnemy = (enemy.position - transform.position).normalized;

        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();

        if (health != null)
        {
            agent.enabled = false;
            health.TakeDamage(damage);

            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, enemy.position, hitVolume);
            }

            if (force >= 5f)
            {
                camEffects.Shake(force / 100f);
                HitStopManager.Instance.TriggerHitStop(animator, health.GetComponent<Animator>());
                StartCoroutine(DelayedKnockbackAfterHitstop(enemy, toEnemy, force));
            }
            else
            {
                Knockback(enemy, toEnemy, force);
            }
        }

        if (hitSparkPrefab != null)
        {
            Quaternion sparkRot = Quaternion.LookRotation(-toEnemy);
            Instantiate(hitSparkPrefab, enemy.position, sparkRot);
        }
    }

    void DealSplashDamageAround(Vector3 center, float radius, float force)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                DealDamageToSingleEnemy(hit.transform, force, 10f);
            }
        }
    }
    #endregion

    // MODIFIED: Logic now separates cost for melee vs. dash attacks.
    void PerformLightAttack()
    {
        if (playerController.isSprinting && playerController.isGrounded)
        {
            // --- DASH ATTACK (No Cost) ---
            StartCoroutine(playerController.DashForward(GetComboRankValue() > 600));
            StartCoroutine(DealContactDamageDuringDash());
        }
        else if (playerController.isGrounded)
        {
            // --- STANDARD MELEE (Has Cost) ---
            comboMeter?.SpendComboPoint();
            animator.SetTrigger("LightAttack");
        }
    }

    public void LightAttackHit()
    {
        DealDamageToEnemies(attackRange, attackAngle, knockbackForce);
    }

    // MODIFIED: Plunge attack now scales its damage radius based on combo rank.
    IEnumerator PerformPlungingAttack()
    {
        StartCoroutine(playerController.PlungeDownward(40f));
        yield return new WaitUntil(() => playerController.isGrounded);

        // Calculate the attack's range based on the current combo rank
        int rankValue = GetComboRankValue();
        float finalRange = plungeBaseRange + (rankValue * plungeBonusRangePerRank);

        Debug.Log($"Plunging with rank {rankValue}, final range: {finalRange}"); // For debugging
        DealDamageToEnemies(finalRange, 360f, plungingAttackForce);
        didPlungeAttack = true;
    }

    void DealDamageToEnemies(float range, float angle, float force)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        Vector3 forward = transform.forward;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Vector3 toTarget = (hit.transform.position - transform.position).normalized;
                float currentAngle = Vector3.Angle(forward, toTarget);

                if (currentAngle <= angle / 2f || angle == 360f)
                {
                    DealDamageToSingleEnemy(hit.transform, force, 20f);
                }
            }
        }
    }

    // MODIFIED: Dash attack now does no damage at the lowest combo rank.
    IEnumerator DealContactDamageDuringDash()
    {
        // At rank "F" (value 0), this attack does nothing.
        if (GetComboRankValue() == 0)
        {
            yield break;
        }

        float timer = 0f;
        float duration = playerController.dashDuration;
        HashSet<Collider> hitEnemies = new HashSet<Collider>();

        while (timer < duration)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 1f);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Enemy") && !hitEnemies.Contains(hit))
                {
                    hitEnemies.Add(hit);
                    DealDamageToSingleEnemy(hit.transform, 5f, 25f);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    // Unchanged knockback methods...
    #region Unchanged Knockback Methods
    IEnumerator DelayedKnockbackAfterHitstop(Transform enemy, Vector3 direction, float force)
    {
        yield return new WaitUntil(() => !HitStopManager.Instance.IsHitStopActive);
        Knockback(enemy, direction, force);
    }

    void Knockback(Transform enemy, Vector3 direction, float force)
    {
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode.Impulse);
            rb.linearVelocity = direction * force;
            agent.enabled = true;
        }
    }
    #endregion
    
    // NEW: Helper function to convert the combo letter into a numeric value for scaling.
    private int GetComboRankValue()
    {
        if (comboMeter == null) return 0;

        string rating = comboMeter.GetComboRating();
        switch (rating)
        {
            case "S": return 5;
            case "A": return 4;
            case "B": return 3;
            case "C": return 2;
            case "D": return 1;
            case "F":
            default: return 0;
        }
    }
}