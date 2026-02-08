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
        lastDamageTime = Time.time; // Initialize damage time
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

        // Trigger damage flash effect
        if (DamageFlashEffect.Instance != null)
        {
            DamageFlashEffect.Instance.TriggerDamageFlash();
        }

        // Reset combo when player takes damage
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.ResetCombo();
        }

        // Reset healing timer when damage is taken
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
        // Force health to 0 to ensure UI updates
        health = 0;

        // Update PlayerHUD immediately before destroying
        if (PlayerHUD.Instance != null)
        {
            // Force immediate update without smoothing
            PlayerHUD.Instance.ForceHealthUpdate(0, maxHealth);
        }

        GameObject spawnedRagdoll = Instantiate(healthData.ragdoll, transform.position, transform.rotation);

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
        lastDamageTime = Time.time;
        StopHealing();
        Debug.Log($"Health reset to {health}");
    }

    private void Update()
    {
        if (healthData != null && healthData.enableAutoHeal && !isHealing && health < maxHealth)
        {
            // Check if enough time has passed since last damage
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

        // Spawn heal VFX if available
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
            // Check if player took damage during healing
            if (Time.time - lastDamageTime < healthData.healDelay)
            {
                StopHealing();
                yield break;
            }

            // Heal the player
            float healAmount = healthData.healRate * healthData.healInterval;
            health = Mathf.Min(health + healAmount, maxHealth);

            // Update HUD
            if (PlayerHUD.Instance != null)
            {
                PlayerHUD.Instance.ForceHealthUpdate(health, maxHealth);
            }

            // Show heal text (always for testing)
            DamageText.CreateHealText(transform.position + Vector3.up);
            Debug.Log($"Heal text shown for {healAmount:F1} HP at {transform.position + Vector3.up}");

            yield return new WaitForSeconds(healthData.healInterval);
        }

        // Healing complete
        StopHealing();
        Debug.Log("Player fully healed");
    }

    public void HitVFX(Vector3 hitPosition)
    {
        if (healthData == null || healthData.hitVFX == null) return;

        // Instantiate VFX at hit position
        GameObject hit = Instantiate(healthData.hitVFX, hitPosition, Quaternion.identity);

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