using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int MaxHealth = 100;

    public Action OnHealthChange;
    private Animator animator;

    public float _currentHealth; // TODO: Make this private 

    public int CurrentHealth => Mathf.CeilToInt(_currentHealth);
    [SerializeField] private float healthDrainRate = 1f;
    private bool isDraining = true;

    private void Awake()
    {
        Animator foundAnimator = GetComponentInChildren<Animator>();
        if (foundAnimator != null)
        {
            animator = foundAnimator;
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
    }

    public void ChangeHealth(float changeAmount)
    {
        _currentHealth += changeAmount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, MaxHealth);
        OnHealthChange?.Invoke();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(float damage)
    {
        ChangeHealth(damage);
        animator?.SetTrigger("takeDamage");
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        // Add death logic here: disable movement, play animation, etc.
    }

    public bool IsDead()
    {
        return _currentHealth <= 0;
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