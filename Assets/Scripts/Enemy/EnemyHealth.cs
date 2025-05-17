using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public GameObject deathVFX; // Optional
    private Animator animator;
    public bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (animator != null)
        {
            animator.SetTrigger("Flinch"); // Optional short hurt animation
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }

        if (animator != null)
        {
            animator.SetTrigger("Die"); // Assumes there's a 'Die' animation state
        }

        // Disable navmesh and collider (or destroy after delay)
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 2f); // give time for death animation to play
    }
}
