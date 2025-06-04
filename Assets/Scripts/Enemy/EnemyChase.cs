using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;
using System.Collections.Generic;

public class EnemyChase : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth health;
    public bool isBoss = false;

    [Header("Attacks")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;
    public float attackDamage = 10f;

    [Header("Movement Dynamics")]
    public float repathThreshold = 0.1f;
    public float offsetRadius = 1.5f;
    private Vector3 destinationOffset;

    [Header("Jump Params")]
    public float jumpHeight = 2.0f;
    public float jumpDuration = 1f;

    [Header("NavMesh Links (Nearby Only)")]
    public List<NavMeshLink> nearbyNavLinks = new List<NavMeshLink>();
    public float navLinkSearchRadius = 3.0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();
        agent.autoTraverseOffMeshLink = false;
    }

    void Update()
    {
        if (agent == null || player == null || (health != null && health.isDead))
        {
            if (isBoss)
            {
                GameState.Instance.NotifyBossDefeatedAndAdvanceLevel();
            }
            return;
        }
        if (agent.isOnOffMeshLink)
        {
            StartCoroutine(ClimbAcrossLink());
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Example anti-stuck check when vertical gap is too high
        if (distanceToPlayer < 0.1f && (player.position.y - transform.position.y) > 2f)
        {
            Vector3 direction = (transform.position - player.position).normalized;
            Vector3 escape = transform.position + direction * 2f;
            agent.SetDestination(escape);
            return;
        }

        // --- Custom NavMeshLink traversal ---
        if (!agent.pathPending && (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete))
        {
            NavMeshLink link = FindNearbyNavMeshLink();
            if (link != null)
            {
                Vector3 start = link.startPoint + link.transform.position;
                Vector3 end = link.endPoint + link.transform.position;

                Vector3 target = (Vector3.Distance(transform.position, start) < Vector3.Distance(transform.position, end)) ? end : start;
                agent.SetDestination(target);
                return;
            }
        }

        // --- Chase logic ---
        if (distanceToPlayer > attackRange)
        {
            agent.SetDestination(player.position);
            animator?.SetFloat("Speed", agent.velocity.magnitude);
            return;
        }


        if (Time.time - lastAttackTime >= attackCooldown)
        {
            agent.ResetPath();
            animator?.SetFloat("Speed", 0f);
            animator?.SetTrigger("Attack");
            lastAttackTime = Time.time;
        }
    }

    IEnumerator ClimbAcrossLink()
{
    OffMeshLinkData data = agent.currentOffMeshLinkData;
    Vector3 startPos = agent.transform.position;
    Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

    float jumpSpeed = 3f; // You can expose this as a public variable if needed
    float distance = Vector3.Distance(startPos, endPos);
    float duration = distance / jumpSpeed;
    float elapsed = 0f;

    // Optional: trigger climbing animation
    animator?.SetBool("Climbing", true);

    while (elapsed < duration)
    {
        agent.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    agent.transform.position = endPos;
    agent.CompleteOffMeshLink();

    animator?.SetBool("Climbing", false);
}


    NavMeshLink FindNearbyNavMeshLink()
    {
        NavMeshLink bestLink = null;
        float minDist = Mathf.Infinity;

        foreach (var link in nearbyNavLinks)
        {
            if (link == null) continue;

            Vector3 start = link.startPoint + link.transform.position;
            Vector3 end = link.endPoint + link.transform.position;

            float distToStart = Vector3.Distance(transform.position, start);
            float distToEnd = Vector3.Distance(transform.position, end);

            if (distToStart < navLinkSearchRadius || distToEnd < navLinkSearchRadius)
            {
                float closest = Mathf.Min(distToStart, distToEnd);
                if (closest < minDist)
                {
                    minDist = closest;
                    bestLink = link;
                }
            }
        }

        return bestLink;
    }

    public void AttackHit()
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"Enemy attacked player for {attackDamage} damage at {Time.time}");
        }
    }
}
