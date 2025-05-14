using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour {
    public float attackRange = 3f;
    public float attackAngle = 90f; // half-angle of cone
    public float knockbackForce = 10f;
    public float plungingAttackForce = 20f;
    public Animator animator;
    public GameObject hitSparkPrefab;
    public GameObject plungeAttackVFXPrefab;
    private ThirdPersonMovement playerController;

    private bool didPlungeAttack = false;
    private bool wasGroundedLastFrame = true;

    void Start() {
        playerController = GetComponent<ThirdPersonMovement>();
    }

    void Update() {
        // Detect plunge attack input
        if (Input.GetMouseButtonDown(0) && playerController.isGrounded) {
            PerformLightAttack();
        }
        else if (Input.GetMouseButtonDown(0) && !playerController.isGrounded) {
            PerformPlungingAttack();
        }

        // Detect landing after plunge
        if (didPlungeAttack && playerController.isGrounded && !wasGroundedLastFrame) {
            PlayPlungeVFX();
            didPlungeAttack = false;
        }

        wasGroundedLastFrame = playerController.isGrounded;
    }

    void PerformLightAttack() {
        animator.SetTrigger("LightAttack");
        DealDamageToEnemies(attackRange, attackAngle, knockbackForce);
    }

    void PerformPlungingAttack()
    {
        playerController.PlungeDownward(plungingAttackForce); // force player down
        DealDamageToEnemies(attackRange, 360f, plungingAttackForce); // full AoE
        didPlungeAttack = true;
    }

    void PlayPlungeVFX() {
        if (plungeAttackVFXPrefab != null) {
            Instantiate(plungeAttackVFXPrefab, transform.position, Quaternion.identity);
        }
    }

    void DealDamageToEnemies(float range, float angle, float force) {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        Vector3 forward = transform.forward;

        foreach (Collider hit in hits) {
            if (hit.CompareTag("Enemy")) {
                Vector3 toTarget = (hit.transform.position - transform.position).normalized;
                float currentAngle = Vector3.Angle(forward, toTarget);

                if (currentAngle <= angle || angle == 360f) {
                    Rigidbody rb = hit.GetComponent<Rigidbody>();
                    UnityEngine.AI.NavMeshAgent agent = hit.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && rb != null) {
                        rb.AddForce(toTarget * force, ForceMode.Impulse);
                        StartCoroutine(KnockbackAgent(agent, rb, toTarget * force, 1f));
                    }
                    // Play hit spark at enemy position
                    if (hitSparkPrefab != null) {
                        Vector3 hitDirection = -(hit.transform.position - transform.position).normalized;
                        Quaternion sparkRotation = Quaternion.LookRotation(hitDirection, Vector3.up);
                        Instantiate(hitSparkPrefab, hit.transform.position, sparkRotation);
                    }
                }
            }
        }
    }

    IEnumerator KnockbackAgent(UnityEngine.AI.NavMeshAgent agent, Rigidbody rb, Vector3 force, float duration) {
        agent.enabled = false;
        rb.linearVelocity = force;
        yield return new WaitForSeconds(duration);
        agent.enabled = true;
    }
}
