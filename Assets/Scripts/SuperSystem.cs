using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class SuperSystem : MonoBehaviour
{
    public static SuperSystem Instance { get; private set; }

    [Header("Super Bar Settings")]
    [SerializeField] float maxSuperCharge = 100f;
    [SerializeField] float chargePerHit = 10f;
    
    [Header("Super Damage Settings")]
    [SerializeField] float superDamageMultiplier = 3f;
    [SerializeField] float radiusDamageMultiplier = 5f;
    [SerializeField] float radiusDamageRange = 5f;
    [SerializeField] float initialPushBackForce = 5f;
    [SerializeField] float radiusPushBackForce = 15f;
    
    [Header("VFX")]
    [SerializeField] GameObject radiusDamageVFX;
    [SerializeField] GameObject swordFirePrefab; // Prefab to spawn on sword
    
    [Header("Time Slow Settings")]
    [SerializeField] float timeSlowScale = 0.1f;

    [Header("Super Activation Timer")]
    [SerializeField] float superActivationDuration = 5f; // Time player has to use super once activated

    [Header("Camera")]
    [SerializeField] CinemachineCamera freeLookCamera;

    private float currentCharge = 0f;
    private bool isSuperReady = false;
    private bool isSuperActive = false;
    private bool isSuperAttackTriggered = false;
    private Animator animator;
    private GameObject currentSwordFireInstance;
    private HealthSystem playerHealth;
    private float superActivationTimer = 0f;
    private Coroutine superTimerCoroutine;

    // Events
    public System.Action<float, float> OnSuperChargeChanged; // current, max
    public System.Action OnSuperReady;
    public System.Action OnSuperActivated;
    public System.Action OnSuperEnded;
    public System.Action<float, float> OnSuperTimerChanged; // current, max

    public bool IsSuperReady => isSuperReady;
    public bool IsSuperActive => isSuperActive;
    public bool IsSuperAttackTriggered => isSuperAttackTriggered;
    public float SuperDamageMultiplier => superDamageMultiplier;
    public float RadiusDamageMultiplier => radiusDamageMultiplier;
    public float CurrentCharge => currentCharge;
    public float MaxCharge => maxSuperCharge;
    public float SuperActivationTimer => superActivationTimer;
    public float SuperActivationDuration => superActivationDuration;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        animator = GetComponent<Animator>();
        playerHealth = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        // Nothing to initialize here - fire VFX will be spawned when needed
    }

    private void Update()
    {
        // Keep fire VFX alive during entire super - if it was destroyed, respawn it
        if (isSuperActive && currentSwordFireInstance == null)
        {
            SpawnSwordFire();
        }
    }

    public void RefreshSwordFire()
    {
        // Called when weapon is drawn to re-attach fire VFX if super is active
        // Fire should persist during entire super (both before and during attack)
        if (isSuperActive)
        {
            // Destroy old instance if exists
            DestroySwordFire();
            // Spawn new one on the new sword
            SpawnSwordFire();
        }
    }

    public void AddCharge(float amount)
    {
        if (isSuperActive) return; // Don't charge during super

        currentCharge = Mathf.Min(currentCharge + amount, maxSuperCharge);
        OnSuperChargeChanged?.Invoke(currentCharge, maxSuperCharge);

        if (currentCharge >= maxSuperCharge && !isSuperReady)
        {
            isSuperReady = true;
            OnSuperReady?.Invoke();
            Debug.Log("Super is READY!");
        }
    }

    public void AddChargeFromHit()
    {
        AddCharge(chargePerHit);
    }

    public bool TryActivateSuper()
    {
        if (!isSuperReady || isSuperActive) return false;

        ActivateSuper();
        return true;
    }

    private void ActivateSuper()
    {
        isSuperActive = true;
        isSuperReady = false;
        isSuperAttackTriggered = false;

        // Make player invincible immediately
        if (playerHealth != null)
        {
            playerHealth.SetInvincible(true);
            Debug.Log("SuperSystem: Player is now INVINCIBLE");
        }

        // Spawn fire VFX on sword
        SpawnSwordFire();

        // Initial push back enemies in range
        PushBackEnemiesInRange(initialPushBackForce);

        // Start the activation timer
        if (superTimerCoroutine != null)
        {
            StopCoroutine(superTimerCoroutine);
        }
        superTimerCoroutine = StartCoroutine(SuperActivationTimerCoroutine());

        OnSuperActivated?.Invoke();
        Debug.Log("Super ACTIVATED!");
    }

    private IEnumerator SuperActivationTimerCoroutine()
    {
        superActivationTimer = superActivationDuration;
        
        while (superActivationTimer > 0f && isSuperActive && !isSuperAttackTriggered)
        {
            superActivationTimer -= Time.unscaledDeltaTime;
            OnSuperTimerChanged?.Invoke(superActivationTimer, superActivationDuration);
            yield return null;
        }

        // If timer ran out and super attack wasn't triggered, cancel the super
        if (isSuperActive && !isSuperAttackTriggered)
        {
            Debug.Log("Super activation timer expired! Super cancelled.");
            CancelSuper();
        }
        
        superTimerCoroutine = null;
    }

    public void CancelSuper()
    {
        // Called when timer expires without using super
        isSuperActive = false;
        isSuperAttackTriggered = false;
        currentCharge = 0f;
        superActivationTimer = 0f;

        // Remove invincibility
        if (playerHealth != null)
        {
            playerHealth.SetInvincible(false);
            Debug.Log("SuperSystem: Player invincibility REMOVED (super cancelled)");
        }

        // Destroy fire VFX
        DestroySwordFire();

        // Stop timer coroutine if running
        if (superTimerCoroutine != null)
        {
            StopCoroutine(superTimerCoroutine);
            superTimerCoroutine = null;
        }

        OnSuperChargeChanged?.Invoke(0f, maxSuperCharge);
        OnSuperTimerChanged?.Invoke(0f, superActivationDuration);
        OnSuperEnded?.Invoke();
        Debug.Log("Super CANCELLED!");
    }

    private void SpawnSwordFire()
    {
        if (swordFirePrefab == null) return;

        // Find the sword in the equipment system
        EquipmentSystem equipment = GetComponent<EquipmentSystem>();
        if (equipment != null && equipment.CurrentWeapon != null)
        {
            // Spawn fire as child of sword - preserve prefab rotation
            currentSwordFireInstance = Instantiate(swordFirePrefab, equipment.CurrentWeapon.transform);
            currentSwordFireInstance.transform.localPosition = Vector3.zero;
            // Don't reset rotation - use the prefab's rotation
            Debug.Log("Sword fire VFX spawned!");
        }
        else
        {
            Debug.LogWarning("SuperSystem: Could not find sword to attach fire VFX");
        }
    }

    private void DestroySwordFire()
    {
        if (currentSwordFireInstance != null)
        {
            Destroy(currentSwordFireInstance);
            currentSwordFireInstance = null;
            Debug.Log("Sword fire VFX destroyed!");
        }
    }

    public void TriggerSuperAttack()
    {
        if (!isSuperActive) return;

        isSuperAttackTriggered = true;

        // Stop the activation timer since super attack was triggered
        if (superTimerCoroutine != null)
        {
            StopCoroutine(superTimerCoroutine);
            superTimerCoroutine = null;
        }
        superActivationTimer = 0f;
        OnSuperTimerChanged?.Invoke(0f, superActivationDuration);

        // Recentre the camera before performing super attack
        RecentreCamera();

        // Trigger super animation
        if (animator != null)
        {
            animator.SetTrigger("super");
        }

        Debug.Log("Super Attack TRIGGERED - Timer stopped");
    }

    private void RecentreCamera()
    {
        if (freeLookCamera != null)
        {
            // In Cinemachine 3.x, we need to access the FreeLook component
            var freeLook = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();
            if (freeLook != null)
            {
                // Force camera to look at target direction
                freeLook.ForceCameraPosition(freeLookCamera.transform.position, Quaternion.LookRotation(transform.forward));
            }

            // Alternative: Reset the camera's rotation to follow target
            var rotationComposer = freeLookCamera.GetComponent<CinemachineRotationComposer>();
            if (rotationComposer != null)
            {
                // Recenter by forcing position update
                freeLookCamera.ForceCameraPosition(freeLookCamera.transform.position, Quaternion.LookRotation(transform.forward));
            }
            
            Debug.Log("Camera recentered for super attack");
        }
    }

    public void EndSuper()
    {
        isSuperActive = false;
        isSuperAttackTriggered = false;
        currentCharge = 0f;
        superActivationTimer = 0f;

        // Remove invincibility
        if (playerHealth != null)
        {
            playerHealth.SetInvincible(false);
            Debug.Log("SuperSystem: Player invincibility REMOVED (super ended)");
        }

        // Stop timer coroutine if running
        if (superTimerCoroutine != null)
        {
            StopCoroutine(superTimerCoroutine);
            superTimerCoroutine = null;
        }

        // Destroy fire VFX
        DestroySwordFire();

        // Trigger move to return to idle
        if (animator != null)
        {
            animator.SetTrigger("move");
        }

        OnSuperChargeChanged?.Invoke(0f, maxSuperCharge);
        OnSuperTimerChanged?.Invoke(0f, superActivationDuration);
        OnSuperEnded?.Invoke();
        Debug.Log("Super ENDED!");
    }

    // Animation Event: Start time slow
    public void TimeSlowStart()
    {
        Time.timeScale = timeSlowScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        Debug.Log("Time Slow START");
    }

    // Animation Event: End time slow
    public void TimeSlowEnd()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("Time Slow END");
    }

    // Animation Event: Trigger radius damage
    public void TriggerRadiusDamage()
    {
        // Spawn VFX
        if (radiusDamageVFX != null)
        {
            GameObject vfx = Instantiate(radiusDamageVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Find all enemies in range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radiusDamageRange);
        foreach (Collider hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Get base sword damage from equipment system
                EquipmentSystem equipment = GetComponent<EquipmentSystem>();
                float baseDamage = 10f; // Default damage if can't get from equipment
                
                // Apply radius damage (base * radiusDamageMultiplier)
                float radiusDamage = baseDamage * radiusDamageMultiplier;
                enemy.TakeDamage(radiusDamage);

                // Spawn SUPER damage text
                DamageText.CreateSuperDamageText(enemy.transform.position + Vector3.up, radiusDamage);

                Debug.Log($"Radius damage dealt to {enemy.name}: {radiusDamage}");
            }
        }

        // Push back enemies with strong force
        PushBackEnemiesInRange(radiusPushBackForce);

        Debug.Log("Radius Damage TRIGGERED!");
    }

    // Animation Event: Push back enemies (can be called separately)
    public void TriggerPushBack()
    {
        PushBackEnemiesInRange(initialPushBackForce);
    }

    private void PushBackEnemiesInRange(float force)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radiusDamageRange);
        foreach (Collider hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Calculate push direction (away from player)
                Vector3 pushDirection = (enemy.transform.position - transform.position).normalized;
                pushDirection.y = 0.3f; // Add slight upward force
                pushDirection.Normalize();

                // Apply push force
                Rigidbody rb = enemy.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(pushDirection * force, ForceMode.Impulse);
                }
                else
                {
                    // If no rigidbody, move the enemy directly
                    CharacterController cc = enemy.GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        enemy.StartCoroutine(PushBackCoroutine(enemy.transform, pushDirection, force));
                    }
                }
            }
        }
    }

    private IEnumerator PushBackCoroutine(Transform target, Vector3 direction, float force)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration && target != null)
        {
            float t = elapsed / duration;
            float currentForce = Mathf.Lerp(force, 0f, t);
            
            CharacterController cc = target.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.Move(direction * currentForce * Time.deltaTime);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radiusDamageRange);
    }
}
