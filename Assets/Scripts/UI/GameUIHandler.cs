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
    [Tooltip("Optional: A UI Toolkit VisualElement to show/hide as a boss warning panel.")]
    public VisualElement bossWarningPanel; // This will be found by name in UIDoc

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
            // If you uncommented m_HealthLabel, find it here:
            // m_HealthLabel = UIDoc.rootVisualElement.Q<Label>("HealthLabel");

            // Find the boss warning panel from the UIDocument if it exists
            bossWarningPanel = UIDoc.rootVisualElement.Q<VisualElement>("BossWarningPanel"); // Assuming you name your boss warning panel "BossWarningPanel"
            if (bossWarningPanel != null)
            {
                bossWarningPanel.style.display = DisplayStyle.None; // Hide it initially
            }
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
        UpdateEnemiesText(GameState.Instance.CurrentEnemiesRemaining);
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
        if (enemiesOrBossText != null)
        {
            enemiesOrBossText.text = $"Enemies Remaining: {count}";
            // Ensure boss warning is hidden when showing enemy count
            if (bossWarningPanel != null)
            {
                bossWarningPanel.style.display = DisplayStyle.None;
            }
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
            enemiesOrBossText.text = "BOSS INCOMING! BOSS INCOMING! BOSS INCOMING! BOSS INCOMING!"; // Change TMPro text to warning
        }

        if (bossWarningPanel != null)
        {
            bossWarningPanel.style.display = DisplayStyle.Flex; // Show the UI Toolkit panel
            // You might want to add animations, sound effects, or a timer to hide this panel
            // after a few seconds here, depending on your design.
            Debug.Log("UI: Boss Warning Panel Activated!");
        }
    }
}