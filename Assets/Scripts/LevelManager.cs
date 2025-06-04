using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LevelManager script to handle overall game state, level progression, and enemy spawning coordination.
/// Assign this script to an empty GameObject in each level/scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("The total number of enemies to spawn in this level.")]
    public int totalEnemiesPerLevel = 30;
    [Tooltip("The threshold of remaining enemies in the level at which the boss will spawn.")]
    public int bossSpawnThreshold = 5;

    [Header("Spawning References")]
    [Tooltip("Drag your EnemySpawner GameObject here.")]
    public EnemySpawner enemySpawner;
    [Tooltip("Drag the Transform where the boss should spawn.")]
    public Transform bossSpawnPoint;

    private bool _bossSpawnedForLevel = false;

    // We no longer need _currentEnemiesInLevel here, it's in GameState
    // public int CurrentEnemiesInLevel { get; private set; } // REMOVE THIS

    void Start()
    {
        // Basic validation
        if (enemySpawner == null)
        {
            Debug.LogError("LevelManager: Enemy Spawner is not assigned! Please assign the EnemySpawner in the Inspector.", this);
            enabled = false;
            return;
        }

        if (bossSpawnPoint == null)
        {
            Debug.LogError("LevelManager: Boss Spawn Point is not assigned! Please assign a Transform for the boss spawn point.", this);
            enabled = false;
            return;
        }

        if (GameState.Instance == null)
        {
            Debug.LogError("LevelManager: GameState instance not found! Make sure a GameState GameObject exists in your scene.", this);
            enabled = false;
            return;
        }

        InitializeLevel();
    }

    void Update()
    {
        // Check if boss needs to be spawned
        // Now getting the current enemy count from GameState
        if (!_bossSpawnedForLevel && GameState.Instance.CurrentEnemiesRemaining <= bossSpawnThreshold && enemySpawner.HasCompletedAllWaves())
        {
            GameState.Instance.NotifyBossAboutToSpawn(); // Notify GameState before spawning
            SpawnBoss();
            _bossSpawnedForLevel = true;
        }
    }

    /// <summary>
    /// Initializes a new level, resetting enemy counts and triggering the spawner.
    /// </summary>
    public void InitializeLevel()
    {
        // Set total enemies in GameState
        GameState.Instance.SetTotalEnemiesForLevel(totalEnemiesPerLevel);
        _bossSpawnedForLevel = false;
        Debug.Log($"Level initialized with {totalEnemiesPerLevel} enemies.");
        enemySpawner.StartSpawningWaves(totalEnemiesPerLevel); // Tell the spawner to start
    }

    /// <summary>
    /// Call this method from an enemy's death script to decrement the total enemies remaining.
    /// This now calls the GameState to update the count.
    /// </summary>
    public void EnemyDefeated()
    {
        GameState.Instance.DecrementEnemiesRemaining(); // Update count in GameState
        // Debug.Log is handled by GameState now, but you can keep it here if needed for LevelManager specifics.
    }

    /// <summary>
    /// Spawns the boss at the designated boss spawn point.
    /// </summary>
    private void SpawnBoss()
    {
        enemySpawner.SpawnBoss(bossSpawnPoint.position);
    }
}