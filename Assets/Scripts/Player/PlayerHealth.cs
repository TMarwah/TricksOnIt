using System;
using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int MaxHealth = 100;

    public Action OnHealthChange;
    private Animator animator;

    public float _currentHealth; // TODO: Make this private 
    public float invulnerabilityDuration = 1f;

    public int CurrentHealth => Mathf.CeilToInt(_currentHealth);
    [SerializeField] private float healthDrainRate = 1f;
    private bool isDraining = true;
    private bool isDead = false;
     private bool isInvulnerable = false;
    private Unity.Cinemachine.CinemachineCamera virtualCamera;

    private void Awake()
    {
        Animator foundAnimator = GetComponent<Animator>();
        if (foundAnimator != null)
        {
            animator = foundAnimator;
        }
        virtualCamera = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
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
        // _currentHealth = Mathf.Clamp(_currentHealth, 0, MaxHealth);
        OnHealthChange?.Invoke();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Restores the player's health to full.
    /// </summary>
    public void RestoreHealthToFull()
    {
        _currentHealth = MaxHealth;
        OnHealthChange?.Invoke();
    }

/// <summary>
    /// Applies damage to the player, triggering invulnerability frames.
    /// </summary>
    /// <param name="damage">The amount of damage to take.</param>
    public void TakeDamage(float damage)
    {
        // Only take damage if not currently invulnerable and not dead
        if (isInvulnerable || isDead)
        {
            return;
        }

        ChangeHealth(-damage); // Apply the damage
        animator?.SetTrigger("takeDamage"); // Trigger damage animation if an Animator is present

        // Get the CameraEffects script from the virtual camera and call Shake
        if (virtualCamera != null)
        {
            var cameraEffects = virtualCamera.GetComponent<CameraEffects>();
            if (cameraEffects != null)
            {
                cameraEffects.Shake(0.1f);
            }
        }

        StartCoroutine(InvulnerabilityCoroutine()); // Start the invulnerability period
    }

    /// <summary>
    /// Coroutine to manage invulnerability duration and visual feedback (transparency).
    /// This uses standard shader properties to enable/disable transparency.
    /// </summary>
    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true; // Player becomes invulnerable
        bool originalDraining = isDraining; // Store original health draining state
        isDraining = false; // Pause health drain during invulnerability for the invulnerability duration


        yield return new WaitForSeconds(invulnerabilityDuration); // Wait for the specified invulnerability duration

        isDraining = originalDraining; // Restore health draining state
        isInvulnerable = false; // Player is no longer invulnerable
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        animator.SetTrigger("Die");
        isDead = true;
        _currentHealth = 0;
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