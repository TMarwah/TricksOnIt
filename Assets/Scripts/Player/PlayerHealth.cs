using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    
    private SkinnedMeshRenderer[] playerRenderers;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        virtualCamera = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();

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
        _currentHealth = Mathf.Clamp(_currentHealth, 0, MaxHealth);
        OnHealthChange?.Invoke();

        if (_currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    public void RestoreHealthToFull()
    {
        _currentHealth = MaxHealth;
        animator.SetTrigger("Undie");
        isDead = false;
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

    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        isDraining = false;

        float timer = 0f;
        Vignette vignette = null;
        float originalVignetteIntensity = 50f;
        Color[] originalPlayerColors = new Color[playerRenderers.Length];

        if (globalVolume != null && globalVolume.profile.TryGet<Vignette>(out vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
            vignette.color.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.color.value = Color.red;
        }

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            originalPlayerColors[i] = playerRenderers[i].material.color;
        }

        while (timer < invulnerabilityDuration)
        {
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0.5f, originalVignetteIntensity, timer / invulnerabilityDuration);
            }

            foreach (var renderer in playerRenderers)
            {
                if (renderer != null)
                {
                    float alpha = Mathf.Abs(Mathf.Cos(timer * (1 / flashInterval) * Mathf.PI * 2)) * (1 - flashAlpha) + flashAlpha;
                    Color originalColor = renderer.material.color;
                    renderer.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        isInvulnerable = false;
        isDraining = true;

        if (vignette != null)
        {
            vignette.intensity.value = originalVignetteIntensity;
            vignette.color.value = Color.black;
        }

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
        if (isDead) return;
        isDead = true;
        _currentHealth = 0;
        OnHealthChange?.Invoke();

        Debug.Log("Player has died.");
        animator?.SetTrigger("Die");
        
        // Notify the GameManager to start the game over sequence
        GameManager.Instance?.NotifyPlayerDied();
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