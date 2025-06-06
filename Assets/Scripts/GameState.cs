using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A centralized manager for game state, level progression, and debug controls.
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

    [Header("UI References")] // NEW: Separated UI references
    [SerializeField]
    [Tooltip("UI Text element to display the timer.")]
    private TMPro.TextMeshProUGUI timerText;
    [SerializeField] // NEW: Field for the main UI Canvas
    [Tooltip("Drag the main UI Canvas GameObject that holds all in-game UI.")]
    private GameObject mainCanvasUI; // NEW: Reference to the main UI Canvas

    [Header("Audio")]
    [SerializeField]
    [Tooltip("Sound effect for the boss spawn warning.")]
    private AudioClip alarmSfx;
    [SerializeField]
    private AudioClip winSFX;
    [SerializeField]
    [Tooltip("The AudioSource that plays the looping background music.")]
    private AudioSource musicSource; // Reference to the music AudioSource
    [SerializeField]
    [Tooltip("The main background music clip that loops during levels.")]
    private AudioClip backgroundMusicLoop; // The default music clip

    [Header("Level Configuration")]
    [Tooltip("Define the settings for each level in order. The array index is the Level Index.")]
    public LevelData[] levels;

    [Header("Boss Sequence Settings")]
    [Tooltip("The delay (in seconds) between the boss warning and the actual spawn.")]
    public float bossSpawnDelay = 2.5f;

    [Header("Level Transition Settings")]
    [Tooltip("How much to slow down time on boss defeat (e.g., 0.1 for 10% speed).")]
    [Range(0.01f, 1f)]
    public float bossDefeatSlowMoFactor = 0.1f;
    [Tooltip("How long the slow-motion effect lasts (in real-world seconds) before advancing the level.")]
    public float bossDefeatSlowMoDuration = 3.0f;
    [Tooltip("The color to tint the main directional light during the boss defeat slow-mo.")]
    public Color bossDefeatLightColor = Color.red;


    // --- Public Properties (Read-only from outside) ---
    public int CurrentLevelIndex { get; private set; } = -1;
    public int CurrentEnemiesRemaining { get; private set; }
    public float Timer { get; private set; }
    public bool IsBossAboutToSpawn { get; private set; }

    // --- Private State ---
    private bool bossHasBeenSpawnedForCurrentLevel = false;
    private bool isAdvancingLevel = false;
    private Light mainDirectionalLight;
    private Color originalLightColor;

    [System.Serializable]
    public class LevelData
    {
        public string levelName;
        public Transform playerSpawnPoint;
        public EnemySpawner enemySpawner;
        public Transform bossSpawnPoint;
        public int totalEnemies;
        public int bossSpawnThreshold;
    }

    #region Unity Lifecycle Methods

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if GameManager should persist across scene loads
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (player == null || levels == null || levels.Length == 0)
        {
            Debug.LogError("GameManager: Core references (Player, Levels) are not configured!", this);
            enabled = false;
            return;
        }

        // Validate music source
        if (musicSource == null)
        {
            Debug.LogWarning("GameManager: Music AudioSource is not assigned. Background music will not play or be controlled.", this);
        } else {
            musicSource.loop = true; // Ensure the music source loops
            // Initial play of background music, it will be controlled by ChangeLevel later
            musicSource.clip = backgroundMusicLoop;
            musicSource.Play();
        }

        // NEW: Validate Canvas UI reference
        if (mainCanvasUI == null)
        {
            Debug.LogWarning("GameManager: Main Canvas UI reference is not assigned. UI visibility will not be controlled.", this);
        }


        FindMainDirectionalLight();
        ChangeLevel(0); // Start the first level
    }

    void Update()
    {
        Timer += Time.deltaTime;
        if (timerText != null)
        {
            timerText.text = $"{Timer:F2}";
        }

        HandleDebugInput();

        // Don't run level logic if we are in the middle of a level transition
        if (!isAdvancingLevel)
        {
            RunCurrentLevelLogic();
        }
        // else
        // {
        // Debug.Log("we are advancing rn"); // Uncomment for verbose debug if needed
        // }
    }

    #endregion

    #region Public Methods

    public void DecrementEnemiesRemaining()
    {
        if (CurrentEnemiesRemaining > 0)
        {
            CurrentEnemiesRemaining--;
            OnEnemiesRemainingChanged?.Invoke(CurrentEnemiesRemaining);
        }
    }

    public void NotifyBossDefeated()
    {
        if (isAdvancingLevel)
        {
            Debug.LogWarning("GameManager: Received NotifyBossDefeated call while already advancing a level. Ignoring.");
            return;
        }
        StartCoroutine(BossDefeatSequenceCoroutine());
    }

    #endregion

    #region Core Game Logic

    private void RunCurrentLevelLogic()
    {
        if (CurrentLevelIndex < 0 || CurrentLevelIndex >= levels.Length) return;

        LevelData currentLevelData = levels[CurrentLevelIndex];

        if (!bossHasBeenSpawnedForCurrentLevel &&
            CurrentEnemiesRemaining <= currentLevelData.bossSpawnThreshold &&
            currentLevelData.enemySpawner.HasCompletedAllWaves())
        {
            bossHasBeenSpawnedForCurrentLevel = true;
            StartCoroutine(BossSpawnSequenceCoroutine());
        }
    }

    /// <summary>
    /// This method now only sets up the level state.
    /// It no longer releases the isAdvancingLevel lock. That is handled by a separate coroutine.
    /// </summary>
    private void ChangeLevel(int newIndex)
    {
        if (newIndex >= levels.Length)
        {
            Debug.LogWarning("GameManager: Reached final level. No further levels to load.");
            // You might add a "Game Won" screen here
            isAdvancingLevel = false; // Release lock if no more levels
            return;
        }
        if (newIndex < 0)
        {
            Debug.LogError($"GameManager: Invalid level index: {newIndex}");
            return;
        }

        Debug.LogWarning($"--- CHANGING TO LEVEL {newIndex} ---");

        // Stop previous level's spawner if active
        if (CurrentLevelIndex >= 0 && CurrentLevelIndex < levels.Length && levels[CurrentLevelIndex].enemySpawner != null)
        {
            levels[CurrentLevelIndex].enemySpawner.StopAllCoroutines();
            // Optionally clear existing enemies from previous level here if they should despawn
        }

        CurrentLevelIndex = newIndex;
        LevelData currentLevelData = levels[CurrentLevelIndex];
        bossHasBeenSpawnedForCurrentLevel = false;
        IsBossAboutToSpawn = false;
        Timer = 0f;

        CurrentEnemiesRemaining = currentLevelData.totalEnemies;
        OnEnemiesRemainingChanged?.Invoke(CurrentEnemiesRemaining); // Update UI immediately

        TeleportPlayerToSpawnPoint(currentLevelData.playerSpawnPoint);
        
        // Ensure PlayerHealth has a RestoreHealthToFull method
        player.GetComponent<PlayerHealth>()?.RestoreHealthToFull();

        if (currentLevelData.enemySpawner != null)
        {
            Debug.Log($"GameManager: Starting spawner for level {CurrentLevelIndex} with {currentLevelData.totalEnemies} enemies.");
            currentLevelData.enemySpawner.StartSpawningWaves(currentLevelData.totalEnemies);
        }
        else
        {
            Debug.LogError($"GameManager: Enemy Spawner for level {CurrentLevelIndex} is not assigned!", this);
        }

        // Restart background music for the new level
        if (musicSource != null && backgroundMusicLoop != null)
        {
            musicSource.clip = backgroundMusicLoop; // Ensure the correct clip is set
            musicSource.Play();
            Debug.Log("GameManager: Background music restarted for new level.");
        }
        
        // NEW: Re-enable the main UI Canvas when a new level starts
        if (mainCanvasUI != null)
        {
            mainCanvasUI.SetActive(true);
            Debug.Log("GameManager: Main UI Canvas re-enabled.");
        }

        // We start a coroutine to release the lock after a frame has passed.
        // This gives the new level time to settle and prevents an instant re-trigger.
        StartCoroutine(ReleaseLevelAdvancementLock());
    }

    private IEnumerator BossSpawnSequenceCoroutine()
    {
        Debug.Log("GameManager: Boss spawn sequence started.");
        
        IsBossAboutToSpawn = true;
        OnBossAboutToSpawn?.Invoke(); // Notify UI
        if (alarmSfx != null)
        {
            musicSource.PlayOneShot(alarmSfx);
        }

        yield return new WaitForSeconds(bossSpawnDelay); // Wait for the pre-spawn delay

        Debug.Log("GameManager: Delay finished. Spawning boss.");
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

    private IEnumerator BossDefeatSequenceCoroutine()
    {
        isAdvancingLevel = true; // Lock level logic during transition
        
        Debug.Log($"GameManager: Boss defeated! Starting slow-mo sequence.");

        // NEW: Stop ALL current sounds on musicSource (including background music and any lingering one-shots like alarm)
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("GameManager: All sounds on musicSource stopped to play win SFX.");
        }

        if (mainCanvasUI != null)
        {
            mainCanvasUI.SetActive(false);
            Debug.Log("GameManager: Main UI Canvas disabled.");
        }

        if (musicSource != null && winSFX != null) // Check if audioSource and winSFX are assigned
        {
            musicSource.PlayOneShot(winSFX); // Play the win SFX
        }
        else
        {
            Debug.LogWarning("GameManager: Win SFX or MusicSource not assigned for BossDefeatSequence.", this);
        }


        if (mainDirectionalLight != null)
        {
            originalLightColor = mainDirectionalLight.color;
        }

        try
        {
            float elapsedTime = 0f;
            while (elapsedTime < bossDefeatSlowMoDuration)
            {
                // Smoothly interpolate time scale from 1.0 to bossDefeatSlowMoFactor
                Time.timeScale = Mathf.Lerp(1.0f, bossDefeatSlowMoFactor, elapsedTime / bossDefeatSlowMoDuration);
                
                // Smoothly interpolate light color
                if (mainDirectionalLight != null)
                {
                    mainDirectionalLight.color = Color.Lerp(originalLightColor, bossDefeatLightColor, elapsedTime / bossDefeatSlowMoDuration);
                }

                elapsedTime += Time.unscaledDeltaTime; // Use unscaledDeltaTime for time-independent slow-mo
                yield return null;
            }
            Time.timeScale = bossDefeatSlowMoFactor; // Ensure it snaps to final slow-mo factor
            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = bossDefeatLightColor; // Ensure it snaps to final color
            }
            // Wait for the remaining duration *in real time*
            // We wait for the *actual* duration of the slow-mo effect, not scaled by Time.timeScale
            yield return new WaitForSecondsRealtime(bossDefeatSlowMoDuration);
        }
        finally // This block always executes, even if the coroutine is stopped or an error occurs
        {
            Debug.Log("GameManager: Slow-mo finished. Restoring time and light.");
            Time.timeScale = 1.0f; // Restore time scale to normal
            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = originalLightColor; // Restore original light color
            }
        }
        
        ChangeLevel(CurrentLevelIndex + 1); // Advance to the next level
    }
    
    /// <summary>
    /// This coroutine waits for a single frame before releasing the advancement lock.
    /// This gives the new level time to settle and prevents an instant re-trigger.
    /// </summary>
    private IEnumerator ReleaseLevelAdvancementLock()
    {
        yield return new WaitForEndOfFrame(); // Wait for the end of the current frame
        isAdvancingLevel = false; // Release the lock
        Debug.Log("GameManager: Level advancement lock released.");
    }

    #endregion

    #region Debug & Helper Logic

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeLevel(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeLevel(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeLevel(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeLevel(3);
        // Add more debug keys if you have more levels
    }

    private void FindMainDirectionalLight()
    {
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                mainDirectionalLight = light;
                Debug.Log("GameManager: Found main directional light '" + light.gameObject.name + "'.");
                originalLightColor = mainDirectionalLight.color; // Store original color
                return;
            }
        }
        Debug.LogWarning("GameManager: No directional light found in scene. Boss defeat light effect will not work.");
    }
    
    private void TeleportPlayerToSpawnPoint(Transform spawnPoint)
    {
        if (player == null || spawnPoint == null) return;

        // Temporarily disable CharacterController for teleportation, then re-enable
        var characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation; // Apply rotation of spawn point
            characterController.enabled = true;
        } else {
            // For other types of controllers (e.g., Rigidbody), direct transform update might be fine
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
        }
        Debug.Log($"GameManager: Player teleported to '{spawnPoint.name}'.");
    }

    #endregion
}
