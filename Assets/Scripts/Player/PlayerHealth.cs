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
    private int deathLayerIndex = 2;
    private bool isDead = false;

    private void Awake()
    {
        Animator foundAnimator = GetComponent<Animator>();
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
        animator.SetTrigger("Die");
        isDead = true;
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