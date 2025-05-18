using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(ThirdPersonMovement))]
public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 3f;
    public float attackAngle = 90f;
    public float knockbackForce = 5f;
    public float plungingAttackForce = 20f;
    public Animator animator;

    [Header("Ranged Attack")]
    public float rangedAttackRange = 8f;
    public float rangedAttackAngle = 60f;
    public float rangedKnockbackForce = 1f;
    public float rangedSplashRadius = 0.5f;
    public int pelletsPerShot = 3;
    public float handCooldown = 0.5f;

    private float leftCooldownTimer = 0f;
    private float rightCooldownTimer = 0f;

    private ThirdPersonMovement playerController;
    private bool didPlungeAttack = false;
    private bool wasGroundedLastFrame = true;

    [Header("VFX")]
    public GameObject hitSparkPrefab;
    public GameObject plungeAttackVFXPrefab;

    private bool isAiming = false;

    void Start()
    {
        playerController = GetComponent<ThirdPersonMovement>();
    }

    void Update()
    {
        leftCooldownTimer -= Time.deltaTime;
        rightCooldownTimer -= Time.deltaTime;

        // Maintain aiming while either Q or R is held down
        bool leftHeld = Input.GetKey(KeyCode.Q);
        bool rightHeld = Input.GetKey(KeyCode.E);

        // Update aiming state on player and local flag
        isAiming = playerController.isGrounded && (leftHeld || rightHeld);
        playerController.isAiming = isAiming;

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
        if (Input.GetMouseButtonDown(0) && playerController.isGrounded)
        {
            PerformLightAttack();
        }
        else if (Input.GetMouseButtonDown(0) && !playerController.isGrounded)
        {
            PerformPlungingAttack();
        }

        if (didPlungeAttack && playerController.isGrounded && !wasGroundedLastFrame)
        {
            PlayPlungeVFX();
            didPlungeAttack = false;
        }

        wasGroundedLastFrame = playerController.isGrounded;
    }

    IEnumerator PerformRangedAttack(string hand)
    {
        isAiming = true;
        playerController.isAiming = true;

        animator.SetTrigger(hand == "Left" ? "ShootLeft" : "ShootRight");

        for (int i = 0; i < pelletsPerShot; i++)
        {
            Transform enemy = FindEnemyInSprayCone(rangedAttackRange, rangedAttackAngle);
            if (enemy != null)
            {
                DealDamageToSingleEnemy(enemy, rangedKnockbackForce);
                DealSplashDamageAround(enemy.position, rangedSplashRadius, rangedKnockbackForce * 0.5f);
            }
            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(0.15f);
        isAiming = false;
        playerController.isAiming = false;
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

    void DealDamageToSingleEnemy(Transform enemy, float force)
    {
        Vector3 toEnemy = (enemy.position - transform.position).normalized;

        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (agent != null && rb != null)
        {
            rb.AddForce(toEnemy * force, ForceMode.Impulse);
            StartCoroutine(KnockbackAgent(agent, rb, toEnemy * force, 0.3f));
        }

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(20f); // adjust value as needed
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
                Transform enemy = hit.transform;
                Vector3 toEnemy = (enemy.position - center).normalized;

                Rigidbody rb = enemy.GetComponent<Rigidbody>();
                UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();

                if (agent != null && rb != null)
                {
                    rb.AddForce(toEnemy * force, ForceMode.Impulse);
                    StartCoroutine(KnockbackAgent(agent, rb, toEnemy * force, 0.2f));
                }

                // 💥 NEW: Apply splash damage
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(10f); // splash damage is lighter
                }

                if (hitSparkPrefab != null)
                {
                    Quaternion sparkRot = Quaternion.LookRotation(-toEnemy);
                    Instantiate(hitSparkPrefab, enemy.position, sparkRot);
                }
            }
        }
    }


    void PerformLightAttack()
    {
        animator.SetTrigger("LightAttack");
        DealDamageToEnemies(attackRange, attackAngle, knockbackForce);
    }

    void PerformPlungingAttack()
    {
        playerController.PlungeDownward(plungingAttackForce);
        DealDamageToEnemies(attackRange, 360f, plungingAttackForce);
        didPlungeAttack = true;
    }

    void PlayPlungeVFX()
    {
        if (plungeAttackVFXPrefab != null)
        {
            Instantiate(plungeAttackVFXPrefab, transform.position, Quaternion.identity);
        }
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
                    Rigidbody rb = hit.GetComponent<Rigidbody>();
                    UnityEngine.AI.NavMeshAgent agent = hit.GetComponent<UnityEngine.AI.NavMeshAgent>();

                    if (agent != null && rb != null)
                    {
                        rb.AddForce(toTarget * force, ForceMode.Impulse);
                        StartCoroutine(KnockbackAgent(agent, rb, toTarget * force, 1f));
                    }

                    // 💥 NEW: Apply melee damage
                    EnemyHealth health = hit.GetComponent<EnemyHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(30f); // heavy melee damage
                    }

                    if (hitSparkPrefab != null)
                    {
                        Quaternion sparkRot = Quaternion.LookRotation(-toTarget);
                        Instantiate(hitSparkPrefab, hit.transform.position, sparkRot);
                    }
                }
            }
        }
    }


    IEnumerator KnockbackAgent(UnityEngine.AI.NavMeshAgent agent, Rigidbody rb, Vector3 force, float duration)
    {
        agent.enabled = false;
        rb.linearVelocity = force;
        yield return new WaitForSeconds(duration);
        agent.enabled = true;
    }
}