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
    [SerializeField] private float comboMultiplier = 0.25f; 

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

        
        CheckAndDealDamage();
    }

    
    public void StartDamageWindow()
    {
        damageWindowActive = true;
        hasDealtDamage.Clear();
        
        
        if (swordColliderController != null)
        {
            swordColliderController.StartDealDamage();
        }
        
        Debug.Log("Boss Damage Window STARTED");
    }

    
    public void EndDamageWindow()
    {
        damageWindowActive = false;
        hasDealtDamage.Clear();
        
        
        if (swordColliderController != null)
        {
            swordColliderController.EndDealDamage();
        }
        
        Debug.Log("Boss Damage Window ENDED");
    }

    
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
        
        ShieldSystem shieldSystem = hit.transform.GetComponent<ShieldSystem>();
        if (shieldSystem != null && shieldSystem.CurrentShield != null)
        {
            
            Debug.Log("Boss attack blocked by player shield!");
            
            
            DamageText.CreateDamageText(hit.point, 0, 0); 
            
            
            
            
            return; 
        }

        
        float baseDamage = bossEnemy != null && bossEnemy.IsInRageMode ? rageDamage : damage;
        float comboMult = 1f + (currentCombo - 1) * comboMultiplier;
        float finalDamage = baseDamage * comboMult;

        
        playerHealth.TakeDamage(finalDamage);
        playerHealth.HitVFX(hit.point);

        
        if (playerHealth.CurrentHealth <= 0 && bossEnemy != null)
        {
            bossEnemy.TriggerVictoryEmote();
        }

        
        ShowDamageText(hit.point, finalDamage);

        
        if (slashVFX != null && vfxSpawnPoint != null)
        {
            Instantiate(slashVFX, vfxSpawnPoint.position, vfxSpawnPoint.rotation);
        }

        
        AudioClip attackSFXToPlay = null;
        if (bossEnemy != null)
        {
            
            attackSFXToPlay = bossEnemy.GetAttackSFX(currentCombo);
        }
        
        
        AudioClip sfxToPlay = attackSFXToPlay != null ? attackSFXToPlay : hitSFX;
        if (sfxToPlay != null)
        {
            audioSource.PlayOneShot(sfxToPlay);
        }

        
        if (TimeStopManager.Instance != null)
        {
            TimeStopManager.Instance.StopTime();
        }

        
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
