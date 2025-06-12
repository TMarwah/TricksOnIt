using UnityEngine;
using UnityEngine.UIElements; // For your existing UI Toolkit elements
using TMPro; // For TextMeshPro

public class GameUIHandler : MonoBehaviour
{
    [Header("Player UI Elements (UI Toolkit)")]
    public PlayerHealth PlayerHealth;

    [Header("Enemy/Boss UI Elements (TextMeshPro)")]
    [Tooltip("Assign your TextMeshProUGUI element that will display enemy count or boss warning.")]
    public TextMeshProUGUI enemiesOrBossText;
    public TextMeshProUGUI healthText;

    private Label m_HealthLabel; // Commented out in your original, keeping for reference if needed

    private void Start()
    {
        // --- Player Health UI Setup (Existing Logic) ---
        if (PlayerHealth != null)
        {
            PlayerHealth.OnHealthChange += HealthChanged;
        }
        else
        {
            Debug.LogWarning("GameUIHandler: PlayerHealth reference is missing!");
        }

        HealthChanged(); // Initial update for player health


        // --- Enemy/Boss UI Setup (New Logic) ---
        if (enemiesOrBossText == null)
        {
            Debug.LogError("GameUIHandler: 'Enemies Or Boss Text' (TextMeshProUGUI) is not assigned! Please assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameUIHandler: GameManager instance not found! Make sure a GameManager GameObject exists in your scene.", this);
            enabled = false;
            return;
        }

        // Subscribe to GameManager events
        GameManager.Instance.OnEnemiesRemainingChanged += UpdateEnemiesText;
        GameManager.Instance.OnBossAboutToSpawn += ShowBossWarning;

        // Set initial enemy count text
        if (!GameManager.Instance.IsBossAboutToSpawn)
        {
            UpdateEnemiesText(GameManager.Instance.CurrentEnemiesRemaining);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks when this GameObject is destroyed
        if (PlayerHealth != null)
        {
            PlayerHealth.OnHealthChange -= HealthChanged;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemiesRemainingChanged -= UpdateEnemiesText;
            GameManager.Instance.OnBossAboutToSpawn -= ShowBossWarning;
        }
    }

    // --- Player Health UI Methods (Existing Logic) ---
    void HealthChanged()
    {
        if (healthText != null && PlayerHealth != null)
        {
            int healthBars = Mathf.CeilToInt(PlayerHealth.CurrentHealth / 5f);
            healthText.text = new string('|', healthBars);
        }
    }

    // --- Enemy/Boss UI Methods (New Logic) ---

    /// <summary>
    /// Updates the TextMeshPro text with the current number of enemies remaining.
    /// This method is called automatically when the OnEnemiesRemainingChanged event fires.
    /// </summary>
    /// <param name="count">The new count of enemies remaining.</param>
    private void UpdateEnemiesText(int count)
    {
        if (!GameManager.Instance.IsBossAboutToSpawn && enemiesOrBossText != null)
        {
            enemiesOrBossText.text = $"Enemies Remaining: {count}";
            enemiesOrBossText.alignment = TextAlignmentOptions.Center;

        }
        else
        {
            ShowBossWarning();
        }
    }

    /// <summary>
    /// Activates a UI element to warn about the boss.
    /// This method is called automatically when the OnBossAboutToSpawn event fires.
    /// </summary>
    private void ShowBossWarning()
    {
        if (enemiesOrBossText != null)
        {
            enemiesOrBossText.text = "BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING!";
            enemiesOrBossText.alignment = TextAlignmentOptions.Left;
            StartCoroutine(ScrollWarningText());
        }
    }

    private System.Collections.IEnumerator ScrollWarningText()
    {
        float scrollSpeed = 100f; // pixels per second
        float resetDelay = 0.5f;

        RectTransform rectTransform = enemiesOrBossText.GetComponent<RectTransform>();
        float startX = rectTransform.anchoredPosition.x;
        float textWidth = enemiesOrBossText.preferredWidth;
        float parentWidth = rectTransform.rect.width;

        // Start from the right edge
        rectTransform.anchoredPosition = new Vector2(parentWidth, rectTransform.anchoredPosition.y);

        while (GameManager.Instance != null && GameManager.Instance.IsBossAboutToSpawn)
        {
            float newX = rectTransform.anchoredPosition.x - scrollSpeed * Time.deltaTime;

            // If text has fully scrolled out, reset to right edge
            if (newX < -textWidth)
            {
                newX = parentWidth;
                yield return new WaitForSeconds(resetDelay);
            }

            rectTransform.anchoredPosition = new Vector2(newX, rectTransform.anchoredPosition.y);
            yield return null;
        }

        // Reset position and alignment when done
        rectTransform.anchoredPosition = new Vector2(startX, rectTransform.anchoredPosition.y);
        enemiesOrBossText.alignment = TextAlignmentOptions.Center;
    }
}