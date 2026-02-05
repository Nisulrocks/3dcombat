using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] float health = 100;
    [SerializeField] float maxHealth = 100;
    [SerializeField] GameObject hitVFX;
    [SerializeField] GameObject ragdoll;

    [Header("Auto Healing")]
    [SerializeField] private bool enableAutoHeal = true;
    [SerializeField] private float healDelay = 10f; // Time without damage before healing starts
    [SerializeField] private float healRate = 5f; // Health per second
    [SerializeField] private float healInterval = 0.5f; // How often to heal (in seconds)
    [SerializeField] private GameObject healVFX; // Visual effect for healing

    private bool isInvincible = false;
    private float lastDamageTime;
    private bool isHealing = false;
    private Coroutine healCoroutine;

    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;

    Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        maxHealth = health; // Set max health to initial health value
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

        // Play damage sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDamageSound();
        }

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
        lastDamageTime = Time.time;
        StopHealing();
        Debug.Log($"Health reset to {health}");
    }

    private void Update()
    {
        if (enableAutoHeal && !isHealing && health < maxHealth)
        {
            // Check if enough time has passed since last damage
            if (Time.time - lastDamageTime >= healDelay)
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
        if (healVFX != null)
        {
            Instantiate(healVFX, transform.position, Quaternion.identity);
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
            if (Time.time - lastDamageTime < healDelay)
            {
                StopHealing();
                yield break;
            }

            // Heal the player
            float healAmount = healRate * healInterval;
            health = Mathf.Min(health + healAmount, maxHealth);

            // Update HUD
            if (PlayerHUD.Instance != null)
            {
                PlayerHUD.Instance.ForceHealthUpdate(health, maxHealth);
            }

            // Show heal text (always for testing)
            DamageText.CreateHealText(transform.position + Vector3.up);
            Debug.Log($"Heal text shown for {healAmount:F1} HP at {transform.position + Vector3.up}");

            yield return new WaitForSeconds(healInterval);
        }

        // Healing complete
        StopHealing();
        Debug.Log("Player fully healed");
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