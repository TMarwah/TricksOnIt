using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int MaxHealth = 100;

    public Action OnHealthChange;

    public float _currentHealth; // TODO: Make this private 

    public int CurrentHealth => Mathf.CeilToInt(_currentHealth);

    private void Start()
    {
        _currentHealth = MaxHealth;
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
}