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

    [Header("UI References")]
    [SerializeField]
    [Tooltip("UI Text element to display the timer.")]
    private TMPro.TextMeshProUGUI timerText;
    [SerializeField]
    [Tooltip("Drag the main UI Canvas GameObject that holds all in-game UI.")]
    private GameObject mainCanvasUI;
    [SerializeField]
    [Tooltip("Drag the End Game UI Canvas GameObject.")]
    private GameObject endGameUI;

    [Header("Audio")]
    [SerializeField]
    [Tooltip("Sound effect for the boss spawn warning.")]
    private AudioClip alarmSfx;
    [SerializeField]
    private AudioClip winSFX;
    [SerializeField]
    [Tooltip("The AudioSource that plays the looping background music.")]
    private AudioSource musicSource;
    [SerializeField]
    [Tooltip("The main background music clip that loops during levels.")]
    private AudioClip backgroundMusicLoop;

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
    [Tooltip("The color to tint the directional lights during the boss defeat slow-mo.")]
    public Color bossDefeatLightColor = Color.red;
    
    // NEW: Settings for the dynamic UI pop-in
    [Header("End Game UI Animation")]
    [Tooltip("How long the pop-in animation for the End Game UI takes.")]
    public float popInDuration = 0.5f;
    [Tooltip("The animation curve for the pop-in effect. Creates a nice 'bounce' or 'overshoot'.")]
    public AnimationCurve popInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Lighting References")] // NEW: Header for specific light references
    [SerializeField]
    [Tooltip("Drag the first directional light here.")]
    private Light directionalLight1;
    [SerializeField]
    [Tooltip("Drag the second directional light here.")]
    private Light directionalLight2;


    // --- Public Properties ---
    public int CurrentLevelIndex { get; private set; } = -1;
    public int CurrentEnemiesRemaining { get; private set; }
    public float Timer { get; private set; }
    public bool IsBossAboutToSpawn { get; private set; }

    // --- Private State ---
    private bool bossHasBeenSpawnedForCurrentLevel = false;
    private bool isAdvancingLevel = false;
    // Reverted to individual light color storage
    private Color originalLightColor1;
    private Color originalLightColor2;
    // NEW: Store original skybox tint color
    private Color originalSkyboxTint;

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
            Debug.LogError("GameManager: Core references are not configured!", this);
            enabled = false;
            return;
        }
        if (musicSource != null && backgroundMusicLoop != null)
        {
            musicSource.loop = true;
            musicSource.clip = backgroundMusicLoop;
            musicSource.Play();
        }
        
        if (mainCanvasUI == null)
        {
            Debug.LogWarning("GameManager: Main Canvas UI reference is not assigned. UI visibility will not be controlled.", this);
        }
        // Hide the end game UI at the start
        if (endGameUI != null)
        {
            endGameUI.SetActive(false);
        }

        FindMainDirectionalLight(); // This method now explicitly checks the two assigned lights
        // NEW: Store original skybox tint at start
        if (RenderSettings.skybox != null)
        {
            originalSkyboxTint = RenderSettings.skybox.HasProperty("_Tint") ? RenderSettings.skybox.GetColor("_Tint") : Color.white;
        }
        else
        {
            Debug.LogWarning("GameManager: No Skybox material found in RenderSettings. Skybox tinting will not work.");
        }

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

        if (!isAdvancingLevel)
        {
            RunCurrentLevelLogic();
        }
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

    private void ChangeLevel(int newIndex)
    {
        if (newIndex >= levels.Length)
        {
            Debug.LogWarning("GameManager: Reached final level.");
            isAdvancingLevel = false;
            return;
        }
        if (newIndex < 0)
        {
            Debug.LogError($"GameManager: Invalid level index: {newIndex}");
            return;
        }

        Debug.LogWarning($"--- CHANGING TO LEVEL {newIndex} ---");

        if (CurrentLevelIndex >= 0 && CurrentLevelIndex < levels.Length)
        {
            levels[CurrentLevelIndex].enemySpawner?.StopAllCoroutines();
        }
        
        CurrentLevelIndex = newIndex;
        LevelData currentLevelData = levels[CurrentLevelIndex];
        bossHasBeenSpawnedForCurrentLevel = false;
        IsBossAboutToSpawn = false;
        Timer = 0f;

        CurrentEnemiesRemaining = currentLevelData.totalEnemies;
        OnEnemiesRemainingChanged?.Invoke(CurrentEnemiesRemaining);

        TeleportPlayerToSpawnPoint(currentLevelData.playerSpawnPoint);
        
        player.GetComponent<PlayerHealth>()?.RestoreHealthToFull();

        if (currentLevelData.enemySpawner != null)
        {
            currentLevelData.enemySpawner.StartSpawningWaves(currentLevelData.totalEnemies);
        }
        
        if (musicSource != null && backgroundMusicLoop != null)
        {
            musicSource.clip = backgroundMusicLoop;
            musicSource.Play();
        }
        
        if (mainCanvasUI != null) mainCanvasUI.SetActive(true);
        if (endGameUI != null) endGameUI.SetActive(false);

        StartCoroutine(ReleaseLevelAdvancementLock());
    }

    private IEnumerator BossSpawnSequenceCoroutine()
    {
        Debug.Log("GameManager: Boss spawn sequence started.");
        
        IsBossAboutToSpawn = true;
        OnBossAboutToSpawn?.Invoke();
        if (alarmSfx != null && musicSource != null)
        {
            musicSource.PlayOneShot(alarmSfx);
        }

        yield return new WaitForSeconds(bossSpawnDelay);

        Debug.Log("GameManager: Delay finished. Spawning boss.");
        LevelData currentLevelData = levels[CurrentLevelIndex];
        currentLevelData.enemySpawner?.SpawnBoss(currentLevelData.bossSpawnPoint.position);
    }

    private IEnumerator BossDefeatSequenceCoroutine()
    {
        isAdvancingLevel = true;
        
        Debug.Log($"GameManager: Boss defeated! Starting slow-mo sequence.");

        if (musicSource != null)
        {
            musicSource.Stop();
            if(winSFX != null) musicSource.PlayOneShot(winSFX);
        }

        if (mainCanvasUI != null) mainCanvasUI.SetActive(false);
        if (endGameUI != null)
        {
            StartCoroutine(AnimateEndGameUICoroutine());
        }

        // Store original light colors for the two specific directional lights
        if (directionalLight1 != null)
        {
            originalLightColor1 = directionalLight1.color;
        }
        if (directionalLight2 != null)
        {
            originalLightColor2 = directionalLight2.color;
        }

        try
        {
            float elapsedTime = 0f;
            while (elapsedTime < bossDefeatSlowMoDuration)
            {
                // Smoothly interpolate time scale from 1.0 to bossDefeatSlowMoFactor
                Time.timeScale = Mathf.Lerp(1.0f, bossDefeatSlowMoFactor, elapsedTime / bossDefeatSlowMoDuration);
                
                // Interpolate color for the two specific directional lights
                if (directionalLight1 != null)
                {
                    directionalLight1.color = Color.Lerp(originalLightColor1, bossDefeatLightColor, elapsedTime / bossDefeatSlowMoDuration);
                }
                if (directionalLight2 != null)
                {
                    directionalLight2.color = Color.Lerp(originalLightColor2, bossDefeatLightColor, elapsedTime / bossDefeatSlowMoDuration);
                }

                // NEW: Interpolate skybox tint color
                if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
                {
                    RenderSettings.skybox.SetColor("_Tint", Color.Lerp(originalSkyboxTint, bossDefeatLightColor, elapsedTime / bossDefeatSlowMoDuration));
                }
                
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = bossDefeatSlowMoFactor;

            // Ensure lights snap to final color
            if (directionalLight1 != null)
            {
                directionalLight1.color = bossDefeatLightColor;
            }
            if (directionalLight2 != null)
            {
                directionalLight2.color = bossDefeatLightColor;
            }
            // NEW: Ensure skybox snaps to final color
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
            {
                RenderSettings.skybox.SetColor("_Tint", bossDefeatLightColor);
            }
            
            yield return new WaitForSecondsRealtime(bossDefeatSlowMoDuration);
        }
        finally
        {
            Debug.Log("GameManager: Slow-mo finished. Restoring time and light.");
            Time.timeScale = 1.0f;
            // Restore original colors for the two specific directional lights
            if (directionalLight1 != null)
            {
                directionalLight1.color = originalLightColor1;
            }
            if (directionalLight2 != null)
            {
                directionalLight2.color = originalLightColor2;
            }
            // NEW: Restore original skybox tint
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
            {
                RenderSettings.skybox.SetColor("_Tint", originalSkyboxTint);
            }
        }
        
        ChangeLevel(CurrentLevelIndex + 1);
    }
    
    /// <summary>
    /// Animates the children of the End Game UI by scaling them up and fading them in.
    /// </summary>
    private IEnumerator AnimateEndGameUICoroutine()
    {
        CanvasGroup canvasGroup = endGameUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("GameManager: End Game UI is missing a CanvasGroup component! Animation will fail.", endGameUI);
            endGameUI.SetActive(true);
            yield break;
        }

        canvasGroup.alpha = 0;
        endGameUI.SetActive(true);

        List<RectTransform> childrenToAnimate = new List<RectTransform>();
        foreach (Transform child in endGameUI.transform)
        {
            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt != null)
            {
                childrenToAnimate.Add(rt);
                rt.localScale = Vector3.zero;
            }
        }

        float elapsedTime = 0f;
        while (elapsedTime < popInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; 
            float progress = Mathf.Clamp01(elapsedTime / popInDuration);
            float curveValue = popInCurve.Evaluate(progress);

            canvasGroup.alpha = curveValue;
            foreach (RectTransform child in childrenToAnimate)
            {
                child.localScale = Vector3.one * curveValue;
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
        foreach (RectTransform child in childrenToAnimate)
        {
            child.localScale = Vector3.one;
        }
    }

    private IEnumerator ReleaseLevelAdvancementLock()
    {
        yield return new WaitForEndOfFrame();
        isAdvancingLevel = false;
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
    }

    /// <summary>
    /// Finds and stores references to the two specific directional lights.
    /// </summary>
    private void FindMainDirectionalLight()
    {
        // If the lights are not assigned in the Inspector, try to find them by type.
        // This is a fallback and assumes there are exactly two directional lights in the scene.
        if (directionalLight1 == null || directionalLight2 == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            List<Light> foundDirectionalLights = new List<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional && light.gameObject.activeInHierarchy)
                {
                    foundDirectionalLights.Add(light);
                }
            }

            if (foundDirectionalLights.Count >= 2)
            {
                if (directionalLight1 == null) directionalLight1 = foundDirectionalLights[0];
                if (directionalLight2 == null) directionalLight2 = foundDirectionalLights[1];
                Debug.Log("GameManager: Automatically assigned directional lights based on scene discovery.");
            }
            else if (foundDirectionalLights.Count == 1)
            {
                if (directionalLight1 == null) directionalLight1 = foundDirectionalLights[0];
                Debug.LogWarning("GameManager: Only one directional light found. Ensure both directionalLight1 and directionalLight2 are assigned for full effect.");
            }
            else
            {
                Debug.LogWarning("GameManager: No directional lights found in scene. Boss defeat light effect will not work.");
            }
        }
        
        // Store original colors of the assigned lights
        if (directionalLight1 != null)
        {
            originalLightColor1 = directionalLight1.color;
            Debug.Log("GameManager: Directional Light 1 found: '" + directionalLight1.gameObject.name + "'.");
        }
        else
        {
            Debug.LogWarning("GameManager: Directional Light 1 is not assigned and could not be found automatically. Lighting effects for it will not apply.");
        }

        if (directionalLight2 != null)
        {
            originalLightColor2 = directionalLight2.color;
            Debug.Log("GameManager: Directional Light 2 found: '" + directionalLight2.gameObject.name + "'.");
        }
        else
        {
            Debug.LogWarning("GameManager: Directional Light 2 is not assigned and could not be found automatically. Lighting effects for it will not apply.");
        }
    }
    
    private void TeleportPlayerToSpawnPoint(Transform spawnPoint)
    {
        if (player == null || spawnPoint == null) return;

        var characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            characterController.enabled = true;
        } else {
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
        }
    }

    #endregion
}
