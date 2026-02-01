using UnityEngine;
using System.Collections.Generic;

public class BossDamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float rageDamage = 75f;
    [SerializeField] private float weaponLength = 2f;
    [SerializeField] private LayerMask playerLayer;

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
        RaycastHit hit;
        
        // Use raycast like player damage dealers
        if (Physics.Raycast(transform.position, -transform.up, out hit, weaponLength, playerLayer))
        {
            // Check if we already hit this target
            if (hasDealtDamage.Contains(hit.transform.gameObject)) return;

            // Check for player HealthSystem
            if (hit.transform.TryGetComponent(out HealthSystem playerHealth))
            {
                DealDamageToPlayer(playerHealth, hit);
                hasDealtDamage.Add(hit.transform.gameObject);
            }
        }
    }

    private void DealDamageToPlayer(HealthSystem playerHealth, RaycastHit hit)
    {
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
        if (hitSFX != null)
        {
            audioSource.PlayOneShot(hitSFX);
        }

        // Trigger time stop effect (boss attacks also trigger time stop)
        if (TimeStopManager.Instance != null)
        {
            TimeStopManager.Instance.StopTime();
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
        Gizmos.color = damageWindowActive ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position - transform.up * weaponLength);
    }
}
