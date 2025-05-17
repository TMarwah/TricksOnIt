using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public Transform player;
    private UnityEngine.AI.NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth health;

    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();
    }

    void Update()
    {
        if (agent != null && player != null && (health == null || !health.isDead))
        {
            agent.SetDestination(player.position);

            // Update animator with movement speed
            if (animator != null)
            {
                animator.SetFloat("Speed", agent.velocity.magnitude);
            }
        }
    }
}
