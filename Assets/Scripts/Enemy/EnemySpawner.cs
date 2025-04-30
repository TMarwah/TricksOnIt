using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;
    public float spawnInterval = 2f;
    public int numEnemiesRemaining = 5;
    private float timer = 0;

    void Update() {
        timer += Time.deltaTime;
        if (timer >= spawnInterval && numEnemiesRemaining > 0) {
            SpawnEnemy();
            numEnemiesRemaining--;
            timer = 0;
        }
    }

    void SpawnEnemy() {
        GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        enemy.GetComponent<EnemyChase>().player = player;
    }
}
