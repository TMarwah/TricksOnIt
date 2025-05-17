using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject bossPrefab;
    public Transform player;
    public float spawnInterval = 2f;
    public int numEnemiesRemaining = 30;
    public int enemiesPerWave = 5;

    private float timer = 0;
    private int enemiesToSpawnThisWave = 0;
    private bool spawningWave = false;
    private bool bossSpawned = false;

    void Update() {
        if (numEnemiesRemaining <= 0) {
            if (!bossSpawned && bossPrefab != null) {
                SpawnBoss();
                bossSpawned = true;
            }
            return;
        }

        timer += Time.deltaTime;

        if (!spawningWave && timer >= spawnInterval) {
            // Start a new wave
            enemiesToSpawnThisWave = Mathf.Min(enemiesPerWave, numEnemiesRemaining);
            spawningWave = true;
            timer = 0;
        }

        if (spawningWave && enemiesToSpawnThisWave > 0) {
            SpawnEnemy();
            enemiesToSpawnThisWave--;
            numEnemiesRemaining--;
        }

        if (spawningWave && enemiesToSpawnThisWave <= 0) {
            spawningWave = false;
        }
    }

    void SpawnEnemy() {
        GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        enemy.GetComponent<EnemyChase>().player = player;
    }

    void SpawnBoss()
    {
        // Boss spawn logic goes here
        GameObject boss = Instantiate(bossPrefab, transform.position, Quaternion.identity);
        boss.GetComponent<EnemyChase>().player = player;
        Debug.Log("Boss spawned!");
    }
}
