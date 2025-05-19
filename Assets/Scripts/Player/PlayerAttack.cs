using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(ThirdPersonMovement))]
public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private CameraEffects camEffects;
    private ThirdPersonMovement playerController;

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
    public float handCooldown = 0.5f;

    private float leftCooldownTimer = 0f;
    private float rightCooldownTimer = 0f;
    public bool didPlungeAttack = false;
    private bool wasGroundedLastFrame = true;

    [Header("Plunge")]
    public float plungingAttackForce = 10f;

    [Header("VFX")]
    public GameObject hitSparkPrefab;
    public GameObject plungeAttackVFXPrefab;

    private bool isAiming = false;

    void Awake()
    {
        playerController = GetComponent<ThirdPersonMovement>();
        Animator foundAnimator = GetComponent<Animator>();
        if (foundAnimator != null)
        {
            animator = foundAnimator;
        }
        var cineCam = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        if (cineCam != null)
        {
            camEffects = cineCam.GetComponent<CameraEffects>();
        }
    }

    void Update()
    {
        if (GetComponent<PlayerHealth>().IsDead()) return;

        leftCooldownTimer -= Time.deltaTime;
        rightCooldownTimer -= Time.deltaTime;
        lightAttackTimer -= Time.deltaTime;

        // Maintain aiming while either Q or R is held down
        bool leftHeld = Input.GetKey(KeyCode.Q);
        bool rightHeld = Input.GetKey(KeyCode.E);

        // Update aiming state on player and local flag
        isAiming = playerController.isGrounded && (leftHeld || rightHeld);
        playerController.isAiming = isAiming;
        animator.SetBool("isAiming", isAiming);

        // Fire left hand shot if cooldown allows and key held
        if (leftHeld && leftCooldownTimer <= 0f && playerController.isGrounded)
        {
            StartCoroutine(PerformRangedAttack("Left"));
            leftCooldownTimer = handCooldown;
        }

        // Fire right hand shot if cooldown allows and key held
        if (rightHeld && rightCooldownTimer <= 0f && playerController.isGrounded)
        {
            StartCoroutine(PerformRangedAttack("Right"));
            rightCooldownTimer = handCooldown;
        }

        // Other attack input handling unchanged
        if (Input.GetMouseButtonDown(0) && playerController.isGrounded && lightAttackTimer <= 0f)
        {
            PerformLightAttack();
            lightAttackTimer = lightAttackCooldown;
        }
        else if (Input.GetMouseButtonDown(0) && !playerController.isGrounded)
        {
            StartCoroutine(PerformPlungingAttack());
        }

        if (didPlungeAttack && playerController.isGrounded && !wasGroundedLastFrame)
        {
            if (plungeAttackVFXPrefab != null)
            {
                Vector3 vfxPos = transform.position;
                vfxPos.y -= 1f;
                Instantiate(plungeAttackVFXPrefab, vfxPos, Quaternion.identity);
            }
            didPlungeAttack = false;
        }

        wasGroundedLastFrame = playerController.isGrounded;
    }

    IEnumerator PerformRangedAttack(string hand)
    {
        animator.SetTrigger(hand == "Left" ? "ShootLeft" : "ShootRight");

        for (int i = 0; i < pelletsPerShot; i++)
        {
            Transform enemy = FindEnemyInSprayCone(rangedAttackRange, rangedAttackAngle);
            if (enemy != null)
            {
                camEffects.Shake(0.01f);
                DealDamageToSingleEnemy(enemy, rangedKnockbackForce, 5f);
                DealSplashDamageAround(enemy.position, rangedSplashRadius, rangedKnockbackForce * 0.5f);
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

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

    void PerformLightAttack()
    {
        if (playerController.isSprinting && playerController.isGrounded)
        {
            StartCoroutine(playerController.DashForward());
            StartCoroutine(DealContactDamageDuringDash());
        }
        else if (playerController.isGrounded)
        {
            animator.SetTrigger("LightAttack");
        }
    }

    public void LightAttackHit()
    {
        DealDamageToEnemies(attackRange, attackAngle, knockbackForce);
    }

    IEnumerator PerformPlungingAttack()
    {
        StartCoroutine(playerController.PlungeDownward(40f));
        yield return new WaitUntil(() => playerController.isGrounded);
        DealDamageToEnemies(attackRange, 360f, plungingAttackForce);
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

    IEnumerator DealContactDamageDuringDash()
    {
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
}