using UnityEngine;

public class PlayerAnimationEvent : MonoBehaviour
{
    public PlayerAttack playerAttack;

    public void LightAttackHit()
    {
        if (playerAttack != null)
        {
            playerAttack.LightAttackHit();
        }
    }
}
