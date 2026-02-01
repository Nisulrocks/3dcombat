using UnityEngine;

public class BossDamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float rageDamage = 75f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float damageRadius = 1.5f;
    [SerializeField] private float damageCooldown = 0.3f; // Time between damage checks during window

    [Header("Combo Settings")]
    [SerializeField] private float comboMultiplier = 0.25f; // 25% more per combo level

    [Header("VFX")]
    [SerializeField] private GameObject slashVFX;
    [SerializeField] private Transform vfxSpawnPoint;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSFX;

    private BossEnemy bossEnemy;
    private AudioSource audioSource;
    private bool damageWindowActive = false;
    private float damageCooldownTimer = 0f;
    private int currentCombo = 1;
    private bool hasDealtDamageThisWindow = false;

    private void Awake()
    {
        bossEnemy = GetComponentInParent<BossEnemy>();
        audioSource = GetComponentInParent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (!damageWindowActive) return;

        // Cooldown between damage checks
        if (damageCooldownTimer > 0)
        {
            damageCooldownTimer -= Time.deltaTime;
            return;
        }

        // Continuously check for player during damage window
        CheckAndDealDamage();
    }

    // Called by animation event to START damage window
    public void StartDamageWindow()
    {
        damageWindowActive = true;
        hasDealtDamageThisWindow = false;
        damageCooldownTimer = 0f;
        Debug.Log("Damage Window STARTED");
    }

    // Called by animation event to END damage window
    public void EndDamageWindow()
    {
        damageWindowActive = false;
        hasDealtDamageThisWindow = false;
        Debug.Log("Damage Window ENDED");
    }

    // Set combo level for this attack
    public void SetComboLevel(int combo)
    {
        currentCombo = Mathf.Clamp(combo, 1, 3);
    }

    private void CheckAndDealDamage()
    {
        // Find player in damage radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius, playerLayer);
        
        foreach (Collider hitCollider in hitColliders)
        {
            HealthSystem playerHealth = hitCollider.GetComponent<HealthSystem>();
            if (playerHealth != null && !hasDealtDamageThisWindow)
            {
                DealDamageToPlayer(playerHealth, hitCollider.transform);
                break; // Only hit one player per window
            }
        }
    }

    private void DealDamageToPlayer(HealthSystem playerHealth, Transform playerTransform)
    {
        hasDealtDamageThisWindow = true;
        damageCooldownTimer = damageCooldown;

        // Calculate final damage
        float baseDamage = bossEnemy != null && bossEnemy.IsInRageMode ? rageDamage : damage;
        float comboMult = 1f + (currentCombo - 1) * comboMultiplier;
        float finalDamage = baseDamage * comboMult;

        // Apply damage
        playerHealth.TakeDamage(finalDamage);

        // Show damage text
        ShowDamageText(playerTransform.position, finalDamage);

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = damageWindowActive ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
