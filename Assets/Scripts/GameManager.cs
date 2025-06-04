using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GameManager script to handle overall game state, level progression, and enemy spawning coordination.
/// Assign this script to an empty GameObject in your scene (e.g., "GameManager").
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

    private int _currentEnemiesInLevel;
    private bool _bossSpawnedForLevel = false;

    // Public property to allow EnemySpawner to decrement the count
    public int CurrentEnemiesInLevel
    {
        get { return _currentEnemiesInLevel; }
        private set { _currentEnemiesInLevel = value; }
    }

    void Start()
    {
        // Basic validation
        if (enemySpawner == null)
        {
            Debug.LogError("GameManager: Enemy Spawner is not assigned! Please assign the EnemySpawner in the Inspector.", this);
            enabled = false;
            return;
        }

        if (bossSpawnPoint == null)
        {
            Debug.LogError("GameManager: Boss Spawn Point is not assigned! Please assign a Transform for the boss spawn point.", this);
            enabled = false;
            return;
        }

        InitializeLevel();
    }

    void Update()
    {
        // Check if boss needs to be spawned
        if (!_bossSpawnedForLevel && CurrentEnemiesInLevel <= bossSpawnThreshold && enemySpawner.HasCompletedAllWaves())
        {
            SpawnBoss();
            _bossSpawnedForLevel = true;
        }

        // You might add logic here for level completion, game over, etc.
        // For example, if all enemies (including boss) are defeated:
        // if (CurrentEnemiesInLevel <= 0 && _bossSpawnedForLevel && IsBossDefeated())
        // {
        //     Debug.Log("Level Complete!");
        //     // Trigger next level, show victory screen, etc.
        // }
    }

    /// <summary>
    /// Initializes a new level, resetting enemy counts and triggering the spawner.
    /// </summary>
    public void InitializeLevel()
    {
        _currentEnemiesInLevel = totalEnemiesPerLevel;
        _bossSpawnedForLevel = false;
        Debug.Log($"Level initialized with {totalEnemiesPerLevel} enemies.");
        enemySpawner.StartSpawningWaves(totalEnemiesPerLevel); // Tell the spawner to start
    }

    /// <summary>
    /// Call this method from an enemy's death script to decrement the total enemies remaining.
    /// </summary>
    public void EnemyDefeated()
    {
        CurrentEnemiesInLevel--;
        Debug.Log($"Enemy defeated. {CurrentEnemiesInLevel} enemies remaining in level.");
    }

    /// <summary>
    /// Spawns the boss at the designated boss spawn point.
    /// </summary>
    private void SpawnBoss()
    {
        enemySpawner.SpawnBoss(bossSpawnPoint.position);
    }

    // You might add a method here to check if the boss is defeated, e.g.,
    // private bool IsBossDefeated()
    // {
    //     // Implement logic to check if the boss GameObject is destroyed or its health is zero
    //     // This would typically involve the boss having a health component that notifies GameManager on death.
    //     return true; // Placeholder
    // }
}