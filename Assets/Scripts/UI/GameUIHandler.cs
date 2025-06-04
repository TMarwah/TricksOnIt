using UnityEngine;
using UnityEngine.UIElements; // For your existing UI Toolkit elements
using TMPro; // For TextMeshPro

public class GameUIHandler : MonoBehaviour
{
    [Header("Player UI Elements (UI Toolkit)")]
    public PlayerHealth PlayerHealth;
    public UIDocument UIDoc;
    private VisualElement m_HealthBarMask;

    [Header("Enemy/Boss UI Elements (TextMeshPro)")]
    [Tooltip("Assign your TextMeshProUGUI element that will display enemy count or boss warning.")]
    public TextMeshProUGUI enemiesOrBossText;

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

        if (UIDoc != null)
        {
            m_HealthBarMask = UIDoc.rootVisualElement.Q<VisualElement>("HealthBarMask");
        }
        else
        {
            Debug.LogWarning("GameUIHandler: UIDocument reference is missing!");
        }

        HealthChanged(); // Initial update for player health


        // --- Enemy/Boss UI Setup (New Logic) ---
        if (enemiesOrBossText == null)
        {
            Debug.LogError("GameUIHandler: 'Enemies Or Boss Text' (TextMeshProUGUI) is not assigned! Please assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        if (GameState.Instance == null)
        {
            Debug.LogError("GameUIHandler: GameState instance not found! Make sure a GameState GameObject exists in your scene.", this);
            enabled = false;
            return;
        }

        // Subscribe to GameState events
        GameState.Instance.OnEnemiesRemainingChanged += UpdateEnemiesText;
        GameState.Instance.OnBossAboutToSpawn += ShowBossWarning;

        // Set initial enemy count text
        if (!GameState.Instance.IsBossAboutToSpawn)
        {
            UpdateEnemiesText(GameState.Instance.CurrentEnemiesRemaining);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks when this GameObject is destroyed
        if (PlayerHealth != null)
        {
            PlayerHealth.OnHealthChange -= HealthChanged;
        }

        if (GameState.Instance != null)
        {
            GameState.Instance.OnEnemiesRemainingChanged -= UpdateEnemiesText;
            GameState.Instance.OnBossAboutToSpawn -= ShowBossWarning;
        }
    }

    // --- Player Health UI Methods (Existing Logic) ---
    void HealthChanged()
    {
        Debug.Log("[GameUIHandler] HealthChanged event received.");
        if (PlayerHealth != null && m_HealthBarMask != null)
        {
            float normalized = PlayerHealth.HealthNormalized();
            // m_HealthLabel.text = $"{PlayerHealth.CurrentHealth}/{PlayerHealth.MaxHealth}";
            m_HealthBarMask.style.width = Length.Percent(normalized * 100f);
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
        if (!GameState.Instance.IsBossAboutToSpawn && enemiesOrBossText != null)
        {
            enemiesOrBossText.text = $"Enemies Remaining: {count}";
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
            enemiesOrBossText.text = "BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING!";
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

        while (GameState.Instance != null && GameState.Instance.IsBossAboutToSpawn)
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