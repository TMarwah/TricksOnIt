using UnityEngine;
using System; // Required for Action

/// <summary>
/// GameState script to manage global game progress, especially enemy counts for UI display.
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

    private int _currentEnemiesRemaining;
    public int CurrentEnemiesRemaining
    {
        get { return _currentEnemiesRemaining; }
        private set
        {
            if (_currentEnemiesRemaining != value)
            {
                _currentEnemiesRemaining = value;
                // Invoke the event whenever the count changes
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

    void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // If you want this GameState to persist across all levels/scenes:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If another instance already exists, destroy this one
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this from LevelManager to set the total enemies for the current level.
    /// </summary>
    /// <param name="total">Total enemies in the current level.</param>
    public void SetTotalEnemiesForLevel(int total)
    {
        CurrentEnemiesRemaining = total;
        IsBossAboutToSpawn = false; // Reset boss flag for new level
        Debug.Log($"GameState: Total enemies for level set to {total}");
    }

    /// <summary>
    /// Call this from LevelManager (via EnemyHealth) when an enemy is defeated.
    /// </summary>
    public void DecrementEnemiesRemaining()
    {
        CurrentEnemiesRemaining--;
        Debug.Log($"GameState: Enemies remaining: {CurrentEnemiesRemaining}");
    }

    /// <summary>
    /// Call this from LevelManager when the boss spawn condition is met.
    /// </summary>
    public void NotifyBossAboutToSpawn()
    {
        IsBossAboutToSpawn = true;
        Debug.Log("GameState: Boss is about to spawn!");
    }

    // You can add more global game state variables here (e.g., player score, game over state, current level number)
}