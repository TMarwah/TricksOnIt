using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List

/// <summary>
/// Handles the spawning of enemies in waves and manages wave progression.
/// Assign this script to an empty GameObject (e.g., "EnemySpawner").
/// Its child GameObjects will be used as spawn points.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [Tooltip("The prefab for regular enemies.")]
    public GameObject enemyPrefab;
    [Tooltip("The prefab for the boss enemy.")]
    public GameObject bossPrefab;

    [Header("Spawn Settings")]
    // This array will now be populated automatically from child transforms
    [Tooltip("All possible spawn point Transforms for regular enemies. These will be automatically gathered from this GameObject's children.")]
    public Transform[] regularSpawnPoints;
    [Tooltip("The time delay between spawning each enemy within a wave.")]
    public float spawnDelayInWave = 0.5f;
    [Tooltip("The time delay between waves.")]
    public float timeBetweenWaves = 3f;
    [Tooltip("The number of enemies to spawn per wave.")]
    public int enemiesPerWave = 5;

    [Header("References")]
    [Tooltip("Drag your Player GameObject here, for enemies to target.")]
    public Transform player;
    [Tooltip("Drag your LevelManager GameObject here.")]
    private LevelManager levelManager;

    private int _enemiesRemainingInLevel; // Total enemies to spawn for the current level
    private int _enemiesSpawnedInCurrentWave;
    private bool _isSpawningWave = false;
    private int _wavesCompleted = 0;
    private int _totalWavesForLevel; // Calculated based on totalEnemiesPerLevel and enemiesPerWave
    private int _currentSpawnPointIndex = 0; // Tracks the current spawn point for sequential use

    void Awake()
    {
        // Automatically gather all child transforms to use as spawn points
        // GetComponentsInChildren(true) includes inactive children.
        // We filter out the spawner's own transform.
        List<Transform> childSpawnPoints = new List<Transform>();
        foreach (Transform child in transform)
        {
            childSpawnPoints.Add(child);
        }
        regularSpawnPoints = childSpawnPoints.ToArray();

        // Log the found spawn points for debugging
        if (regularSpawnPoints.Length > 0)
        {
            Debug.Log($"EnemySpawner: Found {regularSpawnPoints.Length} spawn points as children.");
            for (int i = 0; i < regularSpawnPoints.Length; i++)
            {
                Debug.Log($"  Spawn Point {i}: {regularSpawnPoints[i].name} at {regularSpawnPoints[i].position}");
            }
        }
    }

    void Start()
    {
        // Basic validation
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Enemy Prefab is not assigned!", this);
            enabled = false;
            return;
        }
        if (bossPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: Boss Prefab is not assigned! Boss will not spawn.", this);
        }
        // Check if spawn points were found from children
        if (regularSpawnPoints == null || regularSpawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawner: No Regular Spawn Points found as children! Please add empty GameObjects as children to this spawner.", this);
            enabled = false;
            return;
        }
        if (player == null)
        {
            Debug.LogError("EnemySpawner: Player Transform is not assigned! Enemies will not have a target.", this);
        }
        if (levelManager == null)
        {
            levelManager = GetComponentInParent<LevelManager>();
            if (levelManager == null)
            {
                Debug.LogError("EnemySpawner: LevelManager is not assigned and could not be found in parent!", this);
                enabled = false;
                return;
            }
        }
    }

    /// <summary>
    /// Called by the LevelManager to start the enemy spawning process for a new level.
    /// </summary>
    /// <param name="totalEnemiesToSpawn">The total number of enemies to spawn in this level.</param>
    public void StartSpawningWaves(int totalEnemiesToSpawn)
    {
        _enemiesRemainingInLevel = totalEnemiesToSpawn;
        _enemiesSpawnedInCurrentWave = 0;
        _wavesCompleted = 0;
        _isSpawningWave = false;
        _currentSpawnPointIndex = 0; // Reset spawn point index for new level

        // Calculate total waves needed. Use Mathf.CeilToInt to round up if enemies don't perfectly fit into waves.
        _totalWavesForLevel = Mathf.CeilToInt((float)totalEnemiesToSpawn / enemiesPerWave);
        Debug.Log($"Starting to spawn {totalEnemiesToSpawn} enemies in {_totalWavesForLevel} waves.");

        StartCoroutine(WaveSpawningCoroutine());
    }

    /// <summary>
    /// Coroutine to manage the wave-based spawning of enemies.
    /// </summary>
    private IEnumerator WaveSpawningCoroutine()
    {
        while (_wavesCompleted < _totalWavesForLevel)
        {
            // Wait for the time between waves, unless it's the very first wave
            if (_wavesCompleted > 0)
            {
                Debug.Log($"Waiting {timeBetweenWaves} seconds before next wave...");
                yield return new WaitForSeconds(timeBetweenWaves);
            }

            _isSpawningWave = true;
            Debug.Log($"Starting Wave {_wavesCompleted + 1} of {_totalWavesForLevel}.");

            // Determine how many enemies to spawn in this wave
            int enemiesToSpawnInThisWave = Mathf.Min(enemiesPerWave, _enemiesRemainingInLevel);
            _enemiesSpawnedInCurrentWave = 0;

            for (int i = 0; i < enemiesToSpawnInThisWave; i++)
            {
                SpawnRegularEnemy();
                _enemiesRemainingInLevel--;
                _enemiesSpawnedInCurrentWave++;
                yield return new WaitForSeconds(spawnDelayInWave);
            }

            _isSpawningWave = false;
            _wavesCompleted++;
            Debug.Log($"Wave {_wavesCompleted} completed. {_enemiesRemainingInLevel} enemies left for level.");
        }
        Debug.Log("All waves completed for this level.");
    }

    /// <summary>
    /// Spawns a single regular enemy at the next available spawn point in sequence.
    /// </summary>
    private void SpawnRegularEnemy()
    {
        if (regularSpawnPoints.Length == 0) return; // Should be caught in Awake/Start, but good to double check

        // Get the next spawn point in sequence and then increment the index,
        // wrapping around if we reach the end of the array.
        Transform spawnPoint = regularSpawnPoints[_currentSpawnPointIndex];
        _currentSpawnPointIndex = (_currentSpawnPointIndex + 1) % regularSpawnPoints.Length;

        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        // Assuming EnemyChase is the script that handles enemy movement/AI
        EnemyChase enemyChase = enemy.GetComponent<EnemyChase>();
        if (enemyChase != null)
        {
            enemyChase.player = player;
        }
        else
        {
            Debug.LogWarning("EnemySpawner: Spawned enemy does not have an 'EnemyChase' component. Player target will not be set.");
        }
    }

    /// <summary>
    /// Spawns the boss at the specified boss spawn point.
    /// Called by the LevelManager when the boss spawn condition is met.
    /// </summary>
    /// <param name="spawnPosition">The position where the boss should spawn.</param>
    public void SpawnBoss(Vector3 spawnPosition)
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("Boss Prefab is not assigned, cannot spawn boss.");
            return;
        }

        GameObject boss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        EnemyChase bossChase = boss.GetComponent<EnemyChase>();
        if (bossChase != null)
        {
            bossChase.player = player;
        }
        else
        {
            Debug.LogWarning("EnemySpawner: Spawned boss does not have an 'EnemyChase' component. Player target will not be set.");
        }

        Debug.Log("Boss spawned!");
    }

    /// <summary>
    /// Checks if all planned waves for the current level have been completed.
    /// This is used by LevelManager to determine when the boss can spawn.
    /// </summary>
    /// <returns>True if all waves are completed, false otherwise.</returns>
    public bool HasCompletedAllWaves()
    {
        return _wavesCompleted >= _totalWavesForLevel && !_isSpawningWave;
    }
}