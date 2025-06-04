using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public GameObject deathVFX;
    private Animator animator;
    public bool isDead = false;

    public LevelManager levelManager; // Add this public reference

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
            animator.SetTrigger("Flinch");
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
            animator.SetTrigger("Die");
        }

        // Disable navmesh and collider (or destroy after delay)
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        Collider col = GetComponent<Collider>();
        // Notify LevelManager if assigned, otherwise try to find it
        LevelManager managerToNotify = levelManager != null ? levelManager : FindObjectOfType<LevelManager>();
        if (managerToNotify != null)
        {
            managerToNotify.EnemyDefeated();
        }
        else
        {
            Debug.LogWarning("EnemyHealth: LevelManager not found. Cannot notify of enemy death.");
        }

        Destroy(gameObject, 2f); // give time for death animation to play
    }
}
