using System;
using UnityEngine;
using System.Collections.Generic; // Only needed if you switch to a List

/// <summary>
/// A centralized manager for game state, level progression, and debug controls.
/// This script combines the functionality of GameState, LevelManager, and LevelDebug.
/// It acts as a Singleton.
/// </summary>
public class GameManager : MonoBehaviour
{
    // --- Singleton Instance ---
    public static GameManager Instance { get; private set; }

    // --- Events for External Scripts (like UI) ---
    public event Action<int> OnEnemiesRemainingChanged;
    public event Action OnBossAboutToSpawn;

    // --- Inspector References ---
    [Header("Core References")]
    [Tooltip("Drag the Player GameObject here.")]
    public GameObject player;
    [SerializeField]
    [Tooltip("UI Text element to display the timer.")]
    private TMPro.TextMeshProUGUI timerText;
    [SerializeField]
    [Tooltip("Sound effect for the boss spawn warning.")]
    private AudioClip alarmSfx;

    [Header("Level Configuration")]
    [Tooltip("Define the settings for each level in order. The array index is the Level Index.")]
    public LevelData[] levels;

    // --- Public Properties (Read-only from outside) ---
    public int CurrentLevelIndex { get; private set; } = -1; // Start at -1 to ensure clean load of level 0
    public int CurrentEnemiesRemaining { get; private set; }
    public float Timer { get; private set; }
    public bool IsBossAboutToSpawn { get; private set; }

    // --- Private State ---
    private bool bossHasBeenSpawnedForCurrentLevel = false;

    /// <summary>
    /// Holds all configuration data for a single level.
    /// </summary>
    [System.Serializable]
    public class LevelData
    {
        [Tooltip("Just for clarity in the Inspector.")]
        public string levelName;
        [Tooltip("The spawn point for the player in this level.")]
        public Transform playerSpawnPoint;
        [Tooltip("The spawner responsible for creating enemies in this level.")]
        public EnemySpawner enemySpawner;
        [Tooltip("The transform where the boss for this level will be spawned.")]
        public Transform bossSpawnPoint;
        [Tooltip("Total number of regular enemies to defeat in this level.")]
        public int totalEnemies;
        [Tooltip("The number of remaining enemies at which the boss will spawn.")]
        public int bossSpawnThreshold;
    }

    #region Unity Lifecycle Methods

    void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if this manager should persist across scene loads
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Validate core references
        if (player == null)
        {
            Debug.LogError("GameManager: Player GameObject is not assigned!", this);
            enabled = false;
            return;
        }
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("GameManager: No Levels have been configured in the 'Levels' array!", this);
            enabled = false;
            return;
        }

        // Start the game at the first level
        ChangeLevel(0);
    }

    void Update()
    {
        Timer += Time.deltaTime;
        if (timerText != null)
        {
            timerText.text = $"{Timer:F2}";
        }

        HandleDebugInput();
        RunCurrentLevelLogic();
    }

    #endregion

    #region Public Methods (Called by other scripts)

    /// <summary>
    /// Call this from an enemy's health/death script when it is defeated.
    /// </summary>
    public void DecrementEnemiesRemaining()
    {
        if (CurrentEnemiesRemaining > 0)
        {
            CurrentEnemiesRemaining--;
            OnEnemiesRemainingChanged?.Invoke(CurrentEnemiesRemaining);
        }
    }

    /// <summary>
    /// Call this from a boss's health/death script when it is defeated.
    /// This is the sole trigger for advancing to the next level.
    /// </summary>
    public void NotifyBossDefeated()
    {
        Debug.Log($"GameManager: Boss defeated on level {CurrentLevelIndex}. Advancing to next level.");
        ChangeLevel(CurrentLevelIndex + 1);
    }

    #endregion

    #region Core Game Logic

    /// <summary>
    /// This is the main game loop check that runs every frame.
    /// </summary>
    private void RunCurrentLevelLogic()
    {
        // Do nothing if the current level is invalid or has no data
        if (CurrentLevelIndex < 0 || CurrentLevelIndex >= levels.Length) return;

        LevelData currentLevelData = levels[CurrentLevelIndex];

        // Check if the boss needs to be spawned for the current level
        if (!bossHasBeenSpawnedForCurrentLevel &&
            CurrentEnemiesRemaining <= currentLevelData.bossSpawnThreshold &&
            currentLevelData.enemySpawner.HasCompletedAllWaves())
        {
            SpawnBossForCurrentLevel();
        }
    }

    /// <summary>
    /// Handles all logic for transitioning to a new level.
    /// This is now the single, robust method for level changes.
    /// </summary>
    /// <param name="newIndex">The index of the level to load.</param>
    private void ChangeLevel(int newIndex)
    {
        // --- 1. Validate the new level index ---
        if (newIndex >= levels.Length)
        {
            Debug.LogWarning($"GameManager: Tried to change to level {newIndex}, but it is the final level or out of bounds. Game complete?");
            // Optionally, handle game completion logic here (e.g., show credits screen)
            return;
        }
        if (newIndex < 0)
        {
            Debug.LogError($"GameManager: Tried to change to an invalid negative level index: {newIndex}");
            return;
        }

        Debug.LogWarning($"--- CHANGING TO LEVEL {newIndex} ---");

        // --- 2. Deactivate previous level's spawner (if any) ---
        if (CurrentLevelIndex >= 0 && CurrentLevelIndex < levels.Length)
        {
            levels[CurrentLevelIndex].enemySpawner?.StopAllCoroutines();
        }
        
        // --- 3. Update State for the New Level ---
        CurrentLevelIndex = newIndex;
        LevelData currentLevelData = levels[CurrentLevelIndex];
        bossHasBeenSpawnedForCurrentLevel = false;
        IsBossAboutToSpawn = false;
        Timer = 0f;

        // --- 4. Update GameState Properties and Notify Listeners ---
        CurrentEnemiesRemaining = currentLevelData.totalEnemies;
        OnEnemiesRemainingChanged?.Invoke(CurrentEnemiesRemaining); // Notify UI of new total

        // --- 5. Teleport the Player ---
        TeleportPlayerToSpawnPoint(currentLevelData.playerSpawnPoint);
        
        // Restore Player Health
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.RestoreHealthToFull();
        }

        // --- 6. Start the New Level's Spawner ---
        if (currentLevelData.enemySpawner != null)
        {
            Debug.Log($"GameManager: Starting spawner for level {CurrentLevelIndex} with {currentLevelData.totalEnemies} enemies.");
            currentLevelData.enemySpawner.StartSpawningWaves(currentLevelData.totalEnemies);
        }
        else
        {
            Debug.LogError($"GameManager: Enemy Spawner for level {CurrentLevelIndex} is not assigned!", this);
        }
    }

    /// <summary>
    /// Spawns the boss for the currently active level.
    /// </summary>
    private void SpawnBossForCurrentLevel()
    {
        Debug.Log($"GameManager: Spawning boss for level {CurrentLevelIndex}.");

        // Set state flags
        bossHasBeenSpawnedForCurrentLevel = true;
        IsBossAboutToSpawn = true;
        
        // Play sound and invoke event for UI/other effects
        if (alarmSfx != null)
        {
            AudioSource.PlayClipAtPoint(alarmSfx, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
        OnBossAboutToSpawn?.Invoke();

        // Get the current level's data and spawn the boss
        LevelData currentLevelData = levels[CurrentLevelIndex];
        if (currentLevelData.enemySpawner != null && currentLevelData.bossSpawnPoint != null)
        {
            currentLevelData.enemySpawner.SpawnBoss(currentLevelData.bossSpawnPoint.position);
        }
        else
        {
            Debug.LogError($"GameManager: Cannot spawn boss for level {CurrentLevelIndex}. Spawner or BossSpawnPoint is not assigned.", this);
        }
    }

    #endregion

    #region Debug & Teleport Logic

    /// <summary>
    /// Handles keyboard input for debug teleporting.
    /// </summary>
    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeLevel(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeLevel(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeLevel(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeLevel(3);
        // Add more keys for more levels if needed
    }

    /// <summary>
    /// Safely moves the player to a specified spawn point.
    /// </summary>
    private void TeleportPlayerToSpawnPoint(Transform spawnPoint)
    {
        if (player == null || spawnPoint == null)
        {
            Debug.LogWarning("GameManager: Cannot teleport player. Player or SpawnPoint is not assigned.");
            return;
        }

        var characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            characterController.enabled = true;
        } else { // Fallback for objects without a character controller
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
        }

        Debug.Log($"GameManager: Player teleported to '{spawnPoint.name}'.");
    }

    #endregion
}