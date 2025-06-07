using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public GameObject deathVFX;
    public AudioClip deathSFX;
    private Animator animator;
    public bool isDead = false;
    private EnemyChase chaseComponent;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        chaseComponent = GetComponent<EnemyChase>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (animator != null && !(chaseComponent.isBoss && amount < 10))
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

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // Disable navmesh and collider
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Notify LevelManager if assigned, otherwise try to find it
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DecrementEnemiesRemaining();
        }
        else
        {
            Debug.LogWarning($"Enemy '{gameObject.name}' died but GameManager instance is null. Enemy count not decremented.", this);
        }

        if (chaseComponent != null && chaseComponent.isBoss)
        {
            Debug.Log($"EnemyHealth: This was a boss ({gameObject.name}). Notifying GameManager.");
            GameManager.Instance.NotifyBossDefeated();
        }

        // Start coroutine to handle delayed destruction and VFX
        StartCoroutine(HandleDeath());
    }

    private IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(2f); // Wait for 2 seconds

        if (deathSFX != null)
        {
            AudioSource.PlayClipAtPoint(deathSFX, transform.position);
        }
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }


        Destroy(gameObject);
    }
}
