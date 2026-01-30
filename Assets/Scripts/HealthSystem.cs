using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class HealthSystem : MonoBehaviour
{
    [SerializeField] float health = 100;
    [SerializeField] float maxHealth = 100;
    [SerializeField] GameObject hitVFX;
    [SerializeField] GameObject ragdoll;

    private bool isInvincible = false;
 
    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;

    Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        maxHealth = health; // Set max health to initial health value
    }

    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        Debug.Log($"HealthSystem: Invincibility set to {invincible}");
    }

    public bool IsInvincible => isInvincible;

    public void TakeDamage(float damageAmount)
    {
        // Don't take damage if invincible, but show invincible text
        if (isInvincible)
        {
            Debug.Log("HealthSystem: Damage blocked - player is invincible");
            // Show invincible damage text
            DamageText.CreateInvincibleText(transform.position + Vector3.up);
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
        // Force health to 0 to ensure UI updates
        health = 0;
        
        // Update PlayerHUD immediately before destroying
        if (PlayerHUD.Instance != null)
        {
            // Force immediate update without smoothing
            PlayerHUD.Instance.ForceHealthUpdate(0, maxHealth);
        }
        
        GameObject spawnedRagdoll = Instantiate(ragdoll, transform.position, transform.rotation);
        
        // Notify RespawnManager before destroying, pass the ragdoll reference
        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance.OnPlayerDeath(transform.position, transform.rotation, spawnedRagdoll);
        }
        
        Destroy(this.gameObject);
    }

    public void ResetHealth()
    {
        health = maxHealth;
        isInvincible = false;
        Debug.Log($"Health reset to {health}");
    }

    public void HitVFX(Vector3 hitPosition)
    {
        if (hitVFX == null) return;

        // Instantiate VFX at hit position
        GameObject hit = Instantiate(hitVFX, hitPosition, Quaternion.identity);
        
        // Add FollowTargetVFX component to make it follow this transform
        FollowTargetVFX followComponent = hit.GetComponent<FollowTargetVFX>();
        if (followComponent == null)
        {
            followComponent = hit.AddComponent<FollowTargetVFX>();
        }
        
        // Set target to this transform with offset from hit position
        Vector3 offset = hitPosition - transform.position;
        followComponent.SetTarget(transform, offset);
    }
}