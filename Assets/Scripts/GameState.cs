using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering; // Required for Volume
using UnityEngine.Rendering.Universal; // Required for URP effects

/// <summary>
/// A centralized manager for game state, level progression, and debug controls.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public event Action<int> OnEnemiesRemainingChanged;
    public event Action OnBossAboutToSpawn;
    [Header("Core References")]
    public GameObject player;
    [Tooltip("The Global Volume component in your scene for post-processing effects.")]
    public Volume globalVolume;
    [Header("UI References")]
    [SerializeField]
    private TMPro.TextMeshProUGUI timerText;
    [SerializeField]
    private GameObject mainCanvasUI;
    [SerializeField]
    private GameObject endGameUI;
    [SerializeField]
    private GameObject pauseMenuUI;
    [Header("Audio")]
    [SerializeField]
    private AudioClip alarmSfx;
    [SerializeField]
    private AudioClip winSFX;
    [SerializeField]
    private AudioSource musicSource;
    [SerializeField]
    private AudioClip backgroundMusicLoop;
    [Header("Level Configuration")]
    public LevelData[] levels;
    [Header("Boss Sequence Settings")]
    public float bossSpawnDelay = 2.5f;
    [Header("Level Transition Settings")]
    [Range(0.01f, 1f)]
    public float bossDefeatSlowMoFactor = 0.1f;
    public float bossDefeatSlowMoDuration = 3.0f;
    public Color bossDefeatLightColor = Color.red;
    [Header("End Game UI Animation")]
    public float popInDuration = 0.5f;
    public AnimationCurve popInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Header("Lighting References")]
    [SerializeField]
    private Light directionalLight1;
    [SerializeField]
    private Light directionalLight2;
    public int CurrentLevelIndex { get; private set; } = -1;
    public int CurrentEnemiesRemaining { get; private set; }
    public float Timer { get; private set; }
    public bool IsBossAboutToSpawn { get; private set; }
    private bool bossHasBeenSpawnedForCurrentLevel = false;
    private bool isAdvancingLevel = false;
    private Color originalLightColor1;
    private Color originalLightColor2;
    private Color originalSkyboxTint;
    private bool isPaused = false;
    private ColorAdjustments colorAdjustments;
    private float originalSaturation;

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
        if (endGameUI != null) endGameUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        FindMainDirectionalLight();
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
        {
            originalSkyboxTint = RenderSettings.skybox.GetColor("_Tint");
        }
        else
        {
            Debug.LogWarning("GameManager: No Skybox material found or it lacks a _Tint property. Skybox tinting will not work.");
        }

        // --- This is the key section for your issue ---
        if (globalVolume != null && globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            // Successfully found the Color Adjustments override.
            // We store the original value so we can restore it later.
            originalSaturation = colorAdjustments.saturation.value; 
            Debug.Log("GameManager: Found and cached Color Adjustments from the Global Volume Profile.");
        }
        else
        {
            // This warning will appear if the Volume isn't assigned or the Profile is missing the override.
            Debug.LogWarning("GameManager: Global Volume reference is missing or its profile does not contain a 'Color Adjustments' override. Saturation effects will not work.", this);
            colorAdjustments = null; // Ensure it's null if not found.
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        ChangeLevel(0);
    }

    void Update()
    {
        if (!isPaused)
        {
            HandleDebugInput();
        }

        if (!isPaused && !isAdvancingLevel)
        {
            Timer += Time.deltaTime;
            if (timerText != null)
            {
                timerText.text = $"{Timer:F2}";
            }
            RunCurrentLevelLogic();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    #endregion

    #region Public Methods

    // Unchanged
    public void DecrementEnemiesRemaining()
    {
        if (CurrentEnemiesRemaining > 0)
        {
            CurrentEnemiesRemaining--;
            OnEnemiesRemainingChanged?.Invoke(CurrentEnemiesRemaining);
        }
    }

    // Unchanged
    public void NotifyBossDefeated()
    {
        if (isAdvancingLevel)
        {
            Debug.LogWarning("GameManager: Received NotifyBossDefeated call while already advancing a level. Ignoring.");
            return;
        }
        StartCoroutine(BossDefeatSequenceCoroutine());
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        Debug.Log("Resuming game...");
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if(mainCanvasUI != null) mainCanvasUI.SetActive(true);

        Time.timeScale = 1f;
        isPaused = false;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = originalSaturation;
        }
    }
    
    public void GoToTitleScreen()
    {
        Debug.Log("Going to Title Screen...");
        Time.timeScale = 1f;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("Menu");
    }

    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    #endregion

    #region Core Game Logic

    // --- All methods in this region are unchanged from your original script ---
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
    public void ChangeLevel(int newIndex)
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
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
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
        if (endGameUI != null) StartCoroutine(AnimateEndGameUICoroutine());
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false); 

        if (directionalLight1 != null) originalLightColor1 = directionalLight1.color;
        if (directionalLight2 != null) originalLightColor2 = directionalLight2.color;

        try
        {
            float elapsedTime = 0f;
            while (elapsedTime < bossDefeatSlowMoDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / bossDefeatSlowMoDuration;

                Time.timeScale = Mathf.Lerp(1.0f, bossDefeatSlowMoFactor, progress);
                
                if (directionalLight1 != null) directionalLight1.color = Color.Lerp(originalLightColor1, bossDefeatLightColor, progress);
                if (directionalLight2 != null) directionalLight2.color = Color.Lerp(originalLightColor2, bossDefeatLightColor, progress);
                if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
                {
                    RenderSettings.skybox.SetColor("_Tint", Color.Lerp(originalSkyboxTint, bossDefeatLightColor, progress));
                }
                
                if(colorAdjustments != null)
                {
                    colorAdjustments.saturation.value = Mathf.Lerp(originalSaturation, -100f, progress);
                }
                
                yield return null;
            }
            
            Time.timeScale = bossDefeatSlowMoFactor;
            if(colorAdjustments != null) colorAdjustments.saturation.value = -100f;
            
            yield return new WaitForSecondsRealtime(bossDefeatSlowMoDuration);
        }
        finally
        {
            Debug.Log("GameManager: Slow-mo finished. Restoring state.");
            Time.timeScale = 1.0f;
            if (directionalLight1 != null) directionalLight1.color = originalLightColor1;
            if (directionalLight2 != null) directionalLight2.color = originalLightColor2;
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
            {
                RenderSettings.skybox.SetColor("_Tint", originalSkyboxTint);
            }
            if(colorAdjustments != null)
            {
                colorAdjustments.saturation.value = originalSaturation;
            }
        }
        
        ChangeLevel(CurrentLevelIndex + 1);
    }
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

    #region Debug & Pause Logic
    
    private void HandleDebugInput() { }
    
    public void TogglePause()
    {
        if (isAdvancingLevel)
        {
            Debug.Log("GameManager: Cannot pause during level transition/boss defeat sequence.");
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = -100f;
        }

        Debug.Log("Pausing game...");
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        if (mainCanvasUI != null) mainCanvasUI.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void FindMainDirectionalLight()
    {
        if (directionalLight1 != null && directionalLight2 != null) return;

        Light[] lights = FindObjectsOfType<Light>();
        List<Light> foundDirectionalLights = new List<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional && light.gameObject.activeInHierarchy)
            {
                foundDirectionalLights.Add(light);
            }
        }

        if (foundDirectionalLights.Count >= 1 && directionalLight1 == null) directionalLight1 = foundDirectionalLights[0];
        if (foundDirectionalLights.Count >= 2 && directionalLight2 == null) directionalLight2 = foundDirectionalLights[1];
        
        if (directionalLight1 != null) originalLightColor1 = directionalLight1.color;
        if (directionalLight2 != null) originalLightColor2 = directionalLight2.color;
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