using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

//This is to ensure that it is init before the script the user will write during tutorial so everything is setup
[DefaultExecutionOrder(-9999)]
public class PlayerControl : MonoBehaviour
{
    public Camera MainCamera;
    public float MoveSpeed = 10;
    public int MaxHealth;

    public Action OnHealthChange;
    public int CurrentHealth => Mathf.CeilToInt(m_CurrentHealth);
    
    private NavMeshAgent m_Agent;
    private Animator m_Animator;
    
    private int m_SpeedHash = Animator.StringToHash("Speed");
    private int m_MotionSpeedHash = Animator.StringToHash("MotionSpeed");

    private float m_CurrentHealth;
    
    private void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponentInChildren<Animator>();
        
        m_Agent.speed = MoveSpeed;
        m_Agent.acceleration = 999;
        m_Agent.angularSpeed = 360.0f;

        m_CurrentHealth = MaxHealth;
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var ray = MainCamera.ScreenPointToRay(Mouse.current.position.value);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, 1 << 31))
            {
                m_Agent.SetDestination(hit.point);
            }
        }
        
        m_Animator.SetFloat(m_SpeedHash, m_Agent.velocity.magnitude/MoveSpeed * 6);
        m_Animator.SetFloat(m_MotionSpeedHash, m_Agent.velocity.magnitude/MoveSpeed);
    }

    public void ChangeHealth(float changeAmount)
    {
        m_CurrentHealth += changeAmount;
        OnHealthChange?.Invoke();
    }
}
