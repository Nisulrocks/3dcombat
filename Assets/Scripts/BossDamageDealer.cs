using UnityEngine;
using System.Collections.Generic;

public class BossDamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float rageDamage = 75f;
    [SerializeField] private float weaponLength = 2f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Multi-Raycast Settings")]
    [SerializeField] private int rayCount = 5;
    [SerializeField] private float spreadAngle = 60f; 

    [Header("Combo Settings")]
    [SerializeField] private float comboMultiplier = 0.25f; // 25% more per combo level

    [Header("VFX")]
    [SerializeField] private GameObject slashVFX;
    [SerializeField] private Transform vfxSpawnPoint;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSFX;

    [Header("Collider Control")]
    [SerializeField] private SwordColliderController swordColliderController;

    private BossEnemy bossEnemy;
    private AudioSource audioSource;
    private bool damageWindowActive = false;
    private int currentCombo = 1;
    private List<GameObject> hasDealtDamage = new List<GameObject>();

    private void Awake()
    {
        bossEnemy = GetComponentInParent<BossEnemy>();
        audioSource = GetComponentInParent<AudioSource>();
        
        // Find SwordColliderController if not assigned
        if (swordColliderController == null)
        {
            swordColliderController = GetComponentInChildren<SwordColliderController>();
        }
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (!damageWindowActive) return;

        // Continuously check for player during damage window (no cooldown like player)
        CheckAndDealDamage();
    }

    // Called by animation event to START damage window
    public void StartDamageWindow()
    {
        damageWindowActive = true;
        hasDealtDamage.Clear();
        
        // Enable sword collider for camera shake via Cinemachine collision impulse
        if (swordColliderController != null)
        {
            swordColliderController.StartDealDamage();
        }
        
        Debug.Log("Boss Damage Window STARTED");
    }

    // Called by animation event to END damage window
    public void EndDamageWindow()
    {
        damageWindowActive = false;
        hasDealtDamage.Clear();
        
        // Disable sword collider
        if (swordColliderController != null)
        {
            swordColliderController.EndDealDamage();
        }
        
        Debug.Log("Boss Damage Window ENDED");
    }

    // Set combo level for this attack
    public void SetComboLevel(int combo)
    {
        currentCombo = Mathf.Clamp(combo, 1, 3);
    }

    private void CheckAndDealDamage()
    {
        float halfSpread = spreadAngle / 2f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = rayCount == 1 ? 0f : Mathf.Lerp(-halfSpread, halfSpread, (float)i / (rayCount - 1));
            Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * (-transform.up);

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, weaponLength, playerLayer))
            {
                if (hasDealtDamage.Contains(hit.transform.gameObject)) continue;

                if (hit.transform.TryGetComponent(out HealthSystem playerHealth))
                {
                    DealDamageToPlayer(playerHealth, hit);
                    hasDealtDamage.Add(hit.transform.gameObject);
                }
            }
        }
    }

    private void DealDamageToPlayer(HealthSystem playerHealth, RaycastHit hit)
    {
        // Check if player has an active shield
        ShieldSystem shieldSystem = hit.transform.GetComponent<ShieldSystem>();
        if (shieldSystem != null && shieldSystem.CurrentShield != null)
        {
            // Shield blocked the attack!
            Debug.Log("Boss attack blocked by player shield!");
            
            // Show "BLOCKED" damage text
            DamageText.CreateDamageText(hit.point, 0, 0); // 0 damage, no combo
            
            // Optional: Play block effect/sound here
            // Could add shield impact VFX or sound
            
            return; // Don't deal damage
        }

        // Calculate final damage
        float baseDamage = bossEnemy != null && bossEnemy.IsInRageMode ? rageDamage : damage;
        float comboMult = 1f + (currentCombo - 1) * comboMultiplier;
        float finalDamage = baseDamage * comboMult;

        // Apply damage
        playerHealth.TakeDamage(finalDamage);
        playerHealth.HitVFX(hit.point);

        // Check if this damage killed the player and trigger victory emote
        if (playerHealth.CurrentHealth <= 0 && bossEnemy != null)
        {
            bossEnemy.TriggerVictoryEmote();
        }

        // Show damage text
        ShowDamageText(hit.point, finalDamage);

        // Spawn VFX
        if (slashVFX != null && vfxSpawnPoint != null)
        {
            Instantiate(slashVFX, vfxSpawnPoint.position, vfxSpawnPoint.rotation);
        }

        // Play hit SFX
        AudioClip attackSFXToPlay = null;
        if (bossEnemy != null)
        {
            // Get attack SFX based on combo level
            attackSFXToPlay = bossEnemy.GetAttackSFX(currentCombo);
        }
        
        // Fallback to default hitSFX if no combo-specific SFX is available
        AudioClip sfxToPlay = attackSFXToPlay != null ? attackSFXToPlay : hitSFX;
        if (sfxToPlay != null)
        {
            audioSource.PlayOneShot(sfxToPlay);
        }

        // Trigger time stop effect (boss attacks also trigger time stop)
        if (TimeStopManager.Instance != null)
        {
            TimeStopManager.Instance.StopTime();
        }

        // Trigger chromatic aberration effect on successful hit
        if (HitChromaticEffect.Instance != null)
        {
            HitChromaticEffect.Instance.TriggerHitChromatic();
        }

        Debug.Log($"Boss dealt {finalDamage} damage (Combo: {currentCombo})");
    }

    private void ShowDamageText(Vector3 position, float finalDamage)
    {
        if (bossEnemy != null && bossEnemy.IsInRageMode)
        {
            DamageText.CreateRageDamageText(position, finalDamage);
        }
        else
        {
            DamageText.CreateDamageText(position, finalDamage, currentCombo);
        }
    }

    private void OnDrawGizmos()
    {
        float halfSpread = spreadAngle / 2f;
        Gizmos.color = damageWindowActive ? Color.green : Color.red;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = rayCount == 1 ? 0f : Mathf.Lerp(-halfSpread, halfSpread, (float)i / (rayCount - 1));
            Vector3 direction = Quaternion.AngleAxis(angle, transform.forward) * (-transform.up);
            Gizmos.DrawLine(transform.position, transform.position + direction * weaponLength);
        }
    }
}
