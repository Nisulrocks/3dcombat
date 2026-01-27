using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class HealthSystem : MonoBehaviour
{
    [SerializeField] float health = 100;
    [SerializeField] GameObject hitVFX;
    [SerializeField] GameObject ragdoll;

    private bool isInvincible = false;
 
    Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        Debug.Log($"HealthSystem: Invincibility set to {invincible}");
    }

    public bool IsInvincible => isInvincible;

    public void TakeDamage(float damageAmount)
    {
        // Don't take damage if invincible
        if (isInvincible)
        {
            Debug.Log("HealthSystem: Damage blocked - player is invincible");
            return;
        }

        health -= damageAmount;
        animator.SetTrigger("damage");
        //CameraShake.Instance.ShakeCamera(2f, 0.2f);

        // Reset combo when player takes damage
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.ResetCombo();
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Instantiate(ragdoll, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }
    public void HitVFX(Vector3 hitPosition)
    {
        GameObject hit = Instantiate(hitVFX, hitPosition, Quaternion.identity);
        Destroy(hit, 3f);
 
    }
}