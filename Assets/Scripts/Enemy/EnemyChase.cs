using UnityEngine;

public class EnemyChase : MonoBehaviour {
    public Transform player;
    private UnityEngine.AI.NavMeshAgent agent;

    void Start() {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    void Update() {
        agent.SetDestination(player.position);
    }
}
