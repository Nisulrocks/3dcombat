using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private PlayerHealthData healthData;

    private float health;
    private float maxHealth;

    private bool isInvincible = false;
    private float lastDamageTime;
    private bool isHealing = false;
    private Coroutine healCoroutine;

    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;

    Animator animator;
    void Start()
    {
        if (healthData == null)
        {
            Debug.LogError("HealthSystem: PlayerHealthData not assigned!");
            return;
        }

        animator = GetComponent<Animator>();
        maxHealth = healthData.maxHealth;
        health = maxHealth;
        lastDamageTime = Time.time; 
    }

    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        Debug.Log($"HealthSystem: Invincibility set to {invincible}");
    }

    public bool IsInvincible => isInvincible;

    public void TakeDamage(float damageAmount)
    {
        
        if (isInvincible)
        {
            Debug.Log("HealthSystem: Damage blocked - player is invincible");
            
            DamageText.CreateInvincibleText(transform.position + Vector3.up);
            return;
        }

        health -= damageAmount;
        animator.SetTrigger("damage");
        

        
        if (DamageFlashEffect.Instance != null)
        {
            DamageFlashEffect.Instance.TriggerDamageFlash();
        }

        
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.ResetCombo();
        }

        
        lastDamageTime = Time.time;
        if (isHealing)
        {
            StopHealing();
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        
        health = 0;

        
        if (PlayerHUD.Instance != null)
        {
            
            PlayerHUD.Instance.ForceHealthUpdate(0, maxHealth);
        }

        GameObject spawnedRagdoll = Instantiate(healthData.ragdoll, transform.position, transform.rotation);

        
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
        lastDamageTime = Time.time;
        StopHealing();
        Debug.Log($"Health reset to {health}");
    }

    private void Update()
    {
        if (healthData != null && healthData.enableAutoHeal && !isHealing && health < maxHealth)
        {
            
            if (Time.time - lastDamageTime >= healthData.healDelay)
            {
                StartHealing();
            }
        }
    }

    private void StartHealing()
    {
        if (isHealing || health >= maxHealth) return;

        isHealing = true;
        healCoroutine = StartCoroutine(HealCoroutine());
        Debug.Log("Player auto-healing started");

        
        if (healthData.healVFX != null)
        {
            Vector3 vfxPosition = transform.position + healthData.healVFXOffset;
            Instantiate(healthData.healVFX, vfxPosition, Quaternion.identity);
        }
    }

    private void StopHealing()
    {
        if (healCoroutine != null)
        {
            StopCoroutine(healCoroutine);
            healCoroutine = null;
        }
        isHealing = false;
        Debug.Log("Player auto-healing stopped");
    }

    private IEnumerator HealCoroutine()
    {
        while (health < maxHealth)
        {
            
            if (Time.time - lastDamageTime < healthData.healDelay)
            {
                StopHealing();
                yield break;
            }

            
            float healAmount = healthData.healRate * healthData.healInterval;
            health = Mathf.Min(health + healAmount, maxHealth);

            
            if (PlayerHUD.Instance != null)
            {
                PlayerHUD.Instance.ForceHealthUpdate(health, maxHealth);
            }

            
            DamageText.CreateHealText(transform.position + Vector3.up);
            Debug.Log($"Heal text shown for {healAmount:F1} HP at {transform.position + Vector3.up}");

            yield return new WaitForSeconds(healthData.healInterval);
        }

        
        StopHealing();
        Debug.Log("Player fully healed");
    }

    public void HitVFX(Vector3 hitPosition)
    {
        if (healthData == null || healthData.hitVFX == null) return;

        
        GameObject hit = Instantiate(healthData.hitVFX, hitPosition, Quaternion.identity);

        
        FollowTargetVFX followComponent = hit.GetComponent<FollowTargetVFX>();
        if (followComponent == null)
        {
            followComponent = hit.AddComponent<FollowTargetVFX>();
        }
        
        
        Vector3 offset = hitPosition - transform.position;
        followComponent.SetTarget(transform, offset);
    }
}