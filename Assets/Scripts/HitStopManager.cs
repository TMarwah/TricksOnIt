using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public bool IsHitStopActive { get; private set; }
    public static HitStopManager Instance;
    public float hitStopDuration = 0.1f;

    void Awake()
    {
        Instance = this;
    }

    public void TriggerHitStop(Animator attacker, Animator victim)
    {
        StartCoroutine(DoHitStop(new Animator[] { attacker, victim }));
    }

    IEnumerator DoHitStop(Animator[] animators)
    {
        IsHitStopActive = true;

        foreach (Animator animator in animators)
        {
            if (animator != null)
                animator.speed = 0.2f;
        }

        yield return new WaitForSecondsRealtime(hitStopDuration);

        foreach (Animator animator in animators)
        {
            if (animator != null)
                animator.speed = 1f;
        }
        IsHitStopActive = false;
    }
}
