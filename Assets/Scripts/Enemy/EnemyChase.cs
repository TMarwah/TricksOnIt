using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public Transform player;
    private UnityEngine.AI.NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth health;

    [Header("Attacks")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;
    public float attackDamage = 10f;

    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();
    }

    void Update()
    {
        if (agent == null || player == null || (health != null && health.isDead))
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            agent.SetDestination(player.position);
            animator?.SetFloat("Speed", agent.velocity.magnitude);
            return;
        }

        agent.ResetPath();
        animator?.SetFloat("Speed", 0f);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            animator?.SetTrigger("Attack");
            lastAttackTime = Time.time;
        }
    }

    public void AttackHit()
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }
}
