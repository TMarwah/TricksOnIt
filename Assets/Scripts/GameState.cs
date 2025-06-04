using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GameState script to manage global game progress, including current level index and enemy counts.
/// This is a Singleton, so it will persist across scenes if marked DontDestroyOnLoad.
/// </summary>
public class GameState : MonoBehaviour
{
    // Singleton instance
    public static GameState Instance { get; private set; }

    // Event to notify listeners (like UI) when the enemy count changes
    public event Action<int> OnEnemiesRemainingChanged;
    // Event to notify when the boss is about to spawn
    public event Action OnBossAboutToSpawn;
    // NEW: Event to notify when the current level index changes
    public event Action<int> OnLevelIndexChanged;

    private int _currentEnemiesRemaining;
    public AudioClip alarmSfx;
    public int CurrentEnemiesRemaining
    {
        get { return _currentEnemiesRemaining; }
        private set
        {
            if (_currentEnemiesRemaining != value)
            {
                _currentEnemiesRemaining = value;
                OnEnemiesRemainingChanged?.Invoke(_currentEnemiesRemaining);
            }
        }
    }

    private bool _isBossAboutToSpawn = false;
    public bool IsBossAboutToSpawn
    {
        get { return _isBossAboutToSpawn; }
        private set
        {
            if (_isBossAboutToSpawn != value)
            {
                _isBossAboutToSpawn = value;
                if (_isBossAboutToSpawn)
                {
                    OnBossAboutToSpawn?.Invoke();
                }
            }
        }
    }

    // NEW: Property to hold and update the current level index
    // Initialize to 0 so a LevelManager for level 0 can activate immediately.
    private int _currentLevelIndex = 0;
    public int CurrentLevelIndex
    {
        get { return _currentLevelIndex; }
        private set
        {
            if (_currentLevelIndex != value)
            {
                _currentLevelIndex = value;
                Debug.Log($"GameState: Current Level Index changed to {_currentLevelIndex}");
                OnLevelIndexChanged?.Invoke(_currentLevelIndex); // Notify listeners
            }
        }
    }

    void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // You might want to uncomment this if GameState should persist across scene loads
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // NEW: Method to set the current level, usually called by LevelDebug or level transitions
    public void SetCurrentLevel(int levelIndex)
    {
        CurrentLevelIndex = levelIndex;
        // When a new level is set, you might want to reset other level-specific state
        IsBossAboutToSpawn = false;
        // Enemy count will be set by the active LevelManager itself when it initializes.
    }

    /// <summary>
    /// Call this from the active LevelManager to set the total enemies for the current level.
    /// </summary>
    /// <param name="total">Total enemies in the current level.</param>
    public void SetTotalEnemiesForLevel(int total)
    {
        CurrentEnemiesRemaining = total;
        IsBossAboutToSpawn = false; // Reset boss flag for new level
        Debug.Log($"GameState: Total enemies for current level set to {total}");
    }

    /// <summary>
    /// Call this from EnemyHealth when an enemy is defeated.
    /// </summary>
    public void DecrementEnemiesRemaining()
    {
        CurrentEnemiesRemaining--;
        Debug.Log($"GameState: Enemies remaining: {CurrentEnemiesRemaining}");
    }

    /// <summary>
    /// Call this when the boss is defeated to signal level completion and advance to the next level.
    /// </summary>
    public void NotifyBossDefeatedAndAdvanceLevel()
    {
        Debug.Log("GameState: Boss defeated! Advancing to next level.");
        LevelDebug levelDebug = GetComponent<LevelDebug>();
        if (levelDebug != null)
        {
            levelDebug.TeleportPlayer(CurrentLevelIndex);
        }
        // Optionally, you can trigger additional events or logic here for level completion.
    }

    /// <summary>
    /// Call this from the active LevelManager when the boss spawn condition is met.
    /// </summary>
    public void NotifyBossAboutToSpawn()
    {
        if (alarmSfx != null)
        {
            AudioSource.PlayClipAtPoint(alarmSfx, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
        IsBossAboutToSpawn = true;
        Debug.Log("GameState: Boss is about to spawn!");
    }

    // You can add more global game state variables here (e.g., player score, game over state)
}