using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering; // Required for Volume
using UnityEngine.Rendering.Universal; // Required for URP Vignette

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int MaxHealth = 100;
    public float invulnerabilityDuration = 1f;
    [SerializeField] private float healthDrainRate = 1f;

    [Header("Damage Visuals")]
    [Tooltip("The Volume component for post-processing effects like vignette.")]
    public Volume globalVolume;
    [Tooltip("How fast the player model flashes during invulnerability.")]
    public float flashInterval = 0.1f;
    [Tooltip("The alpha value to flash to. 0 is invisible, 1 is opaque.")]
    [Range(0f, 1f)]
    public float flashAlpha = 0.3f;

    public Action OnHealthChange;
    private Animator animator;

    public float _currentHealth;
    public int CurrentHealth => Mathf.CeilToInt(_currentHealth);

    private bool isDraining = true;
    private bool isDead = false;
    private bool isInvulnerable = false;
    private Unity.Cinemachine.CinemachineCamera virtualCamera;

    // UPDATED: Reference to the player's SkinnedMeshRenderer
    private SkinnedMeshRenderer[] playerRenderers;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        virtualCamera = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();

        // UPDATED: Find SkinnedMeshRenderer components on this object or its children.
        playerRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        if (playerRenderers == null || playerRenderers.Length == 0)
        {
            Debug.LogWarning("PlayerHealth: No SkinnedMeshRenderer found on player. Flashing effect will not work.", this);
        }
    }

    private void Start()
    {
        _currentHealth = MaxHealth;
    }

    private void Update()
    {
        if (isDraining && !IsDead())
        {
            float healthLoss = healthDrainRate * Time.deltaTime;
            ChangeHealth(-healthLoss);
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            Die();
        }
    }

    public void ChangeHealth(float changeAmount)
    {
        _currentHealth += changeAmount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, MaxHealth); // Good practice to clamp health
        OnHealthChange?.Invoke();

        if (_currentHealth <= 0 && !isDead) // Prevent Die() from being called multiple times
        {
            Die();
        }
    }

    public void RestoreHealthToFull()
    {
        _currentHealth = MaxHealth;
        OnHealthChange?.Invoke();
    }

    public void TakeDamage(float damage)
    {
        if (isInvulnerable || isDead) return;

        ChangeHealth(-damage);
        animator?.SetTrigger("takeDamage");

        if (virtualCamera != null)
        {
            var cameraEffects = virtualCamera.GetComponent<CameraEffects>();
            cameraEffects?.Shake(0.1f);
        }

        StartCoroutine(InvulnerabilityCoroutine());
    }

    /// <summary>
    /// MODIFIED: Coroutine now handles both vignette pulse and SkinnedMeshRenderer flashing.
    /// </summary>
    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        isDraining = false; // Pause health drain during invulnerability

        float timer = 0f;
        Vignette vignette = null;
        float originalVignetteIntensity = 50f;
        Color[] originalPlayerColors = new Color[playerRenderers.Length];

        // --- Setup Effects ---
        // Safely get the vignette and store its original intensity
        if (globalVolume != null && globalVolume.profile.TryGet<Vignette>(out vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
            // Set our overrides to true so the script has control
            vignette.color.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.color.value = Color.red;
        }

        // Store the original colors of all SkinnedMeshRenderers
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            originalPlayerColors[i] = playerRenderers[i].material.color;
        }

        // --- Main Invulnerability Loop ---
        while (timer < invulnerabilityDuration)
        {
            // Vignette Pulse Effect: Start high and fade out
            if (vignette != null)
            {
                // Lerp intensity from 1 back down to its original value over the duration
                vignette.intensity.value = Mathf.Lerp(0.5f, originalVignetteIntensity, timer / invulnerabilityDuration);
            }

            // Player Flashing Effect
            foreach (var renderer in playerRenderers)
            {
                if (renderer != null)
                {
                    // Use cosine wave to create a smooth flash on/off effect
                    float alpha = Mathf.Abs(Mathf.Cos(timer * (1 / flashInterval) * Mathf.PI * 2)) * (1 - flashAlpha) + flashAlpha;
                    Color originalColor = renderer.material.color;
                    renderer.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                }
            }

            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // --- Cleanup Effects ---
        isInvulnerable = false;
        isDraining = true; // Resume health drain

        // Reset vignette to its original state
        if (vignette != null)
        {
            vignette.intensity.value = originalVignetteIntensity;
            vignette.color.value = Color.black; // Or whatever your default color is
        }

        // Ensure all SkinnedMeshRenderers are fully visible after invulnerability ends
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
            {
                playerRenderers[i].material.color = originalPlayerColors[i];
            }
        }
    }

    private void Die()
    {
        if (isDead) return; // Ensure this only runs once
        isDead = true;
        _currentHealth = 0;
        OnHealthChange?.Invoke();

        Debug.Log("Player has died.");
        animator?.SetTrigger("Die");
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float HealthNormalized()
    {
        return _currentHealth / MaxHealth;
    }

    public void SetDraining(bool draining)
    {
        isDraining = draining;
    }
}