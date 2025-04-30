using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour {
    public float attackRange = 3f;
    public float attackAngle = 90f; // half-angle of cone
    public float knockbackForce = 10f;

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            PerformAttack();
        }
    }

    void PerformAttack() {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        Vector3 forward = transform.forward;

        foreach (Collider hit in hits) {
            if (hit.CompareTag("Enemy")) {
                Debug.Log($"Hit enemy!");
                Vector3 toTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(forward, toTarget);

                if (angle <= attackAngle) {
                    Rigidbody rb = hit.GetComponent<Rigidbody>();
                    UnityEngine.AI.NavMeshAgent agent = hit.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null && rb != null) {
                        rb.AddForce(toTarget * knockbackForce, ForceMode.Impulse);
                        StartCoroutine(KnockbackAgent(agent, rb, toTarget * knockbackForce, 1f));
                    }
                }
            }
        }
    }

    IEnumerator KnockbackAgent(UnityEngine.AI.NavMeshAgent agent, Rigidbody rb, Vector3 force, float duration) {
        // Rotate the enemy by 45 degrees instantly
        // transform.Rotate(0, 45, 0);

        // Flash red
        Renderer renderer = rb.GetComponent<Renderer>();
        Color originalColor = renderer.material.color;
        renderer.material.color = Color.red;

        agent.enabled = false;
        rb.linearVelocity = force;
        yield return new WaitForSeconds(duration);
        // transform.Rotate(0,-45,0);
        renderer.material.color = originalColor;
        agent.enabled = true;
    }
}
