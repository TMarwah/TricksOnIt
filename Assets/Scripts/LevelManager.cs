using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LevelManager script to handle specific level progression and enemy spawning coordination.
/// Assign this script to an empty GameObject in each distinct "level area" within your scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level Identification")]
    [Tooltip("The unique index for this level manager. Corresponds to the spawn point index in LevelDebug.")]
    public int levelIndex;

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
    private bool _isLevelActive = false; // NEW: Flag to control if this LevelManager is currently active

    void Awake()
    {
        // Subscribe to level change event from GameState
        if (GameState.Instance != null)
        {
            GameState.Instance.OnLevelIndexChanged += OnLevelIndexChanged;
        }
        else
        {
            Debug.LogError("LevelManager: GameState instance not found in Awake! GameState must be initialized first.", this);
            enabled = false; // Disable if GameState isn't ready
        }
    }

    void Start()
    {
        // Basic validation for essential references
        if (enemySpawner == null)
        {
            Debug.LogError($"LevelManager (Index {levelIndex}): Enemy Spawner is not assigned! Please assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        if (bossSpawnPoint == null)
        {
            Debug.LogError($"LevelManager (Index {levelIndex}): Boss Spawn Point is not assigned! Please assign a Transform for the boss spawn point.", this);
            enabled = false;
            return;
        }

        // Immediately check if this level manager should be active based on the current GameState level
        OnLevelIndexChanged(GameState.Instance.CurrentLevelIndex);
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (GameState.Instance != null)
        {
            GameState.Instance.OnLevelIndexChanged -= OnLevelIndexChanged;
        }
    }

    /// <summary>
    /// Callback when the current level index changes in GameState.
    /// </summary>
    /// <param name="newLevelIndex">The new level index from GameState.</param>
    private void OnLevelIndexChanged(int newLevelIndex)
    {
        if (newLevelIndex == levelIndex)
        {
            // This LevelManager is now active for the current level
            if (!_isLevelActive) // Only initialize if it wasn't active before
            {
                Debug.Log($"LevelManager (Index {levelIndex}) activated.");
                _isLevelActive = true;
                InitializeLevel();
            }
        }
        else
        {
            // This LevelManager is not for the current level, deactivate it
            if (_isLevelActive) // Only deactivate if it was active before
            {
                Debug.Log($"LevelManager (Index {levelIndex}) deactivated.");
                _isLevelActive = false;
                // Optional: Stop any active coroutines or reset state of this LevelManager if needed
                enemySpawner.StopAllCoroutines(); // Stop spawning if waves are ongoing
            }
        }
    }

    void Update()
    {
        if (!_isLevelActive) return; // Only run Update logic if this level is active

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
    /// This method is called when this LevelManager becomes active.
    /// </summary>
    public void InitializeLevel()
    {
        // Set total enemies in GameState for the *current active level*
        GameState.Instance.SetTotalEnemiesForLevel(totalEnemiesPerLevel);
        _bossSpawnedForLevel = false;
        Debug.Log($"LevelManager (Index {levelIndex}): Initialized with {totalEnemiesPerLevel} enemies.");
        enemySpawner.StartSpawningWaves(totalEnemiesPerLevel); // Tell the spawner to start spawning
    }

    /// <summary>
    /// Call this method from an enemy's death script to decrement the total enemies remaining.
    /// This now calls the GameState to update the count.
    /// </summary>
    public void EnemyDefeated()
    {
        if (!_isLevelActive) return; // Only process if this is the active level manager
        GameState.Instance.DecrementEnemiesRemaining(); // Update count in GameState
    }

    /// <summary>
    /// Spawns the boss at the designated boss spawn point.
    /// </summary>
    private void SpawnBoss()
    {
        enemySpawner.SpawnBoss(bossSpawnPoint.position);
    }
}