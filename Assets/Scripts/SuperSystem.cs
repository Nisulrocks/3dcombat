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
    [SerializeField] GameObject swordFirePrefab; 
    
    [Header("Time Slow Settings")]
    [SerializeField] float timeSlowScale = 0.1f;

    [Header("Super Activation Timer")]
    [SerializeField] float superActivationDuration = 5f; 

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

    
    public System.Action<float, float> OnSuperChargeChanged; 
    public System.Action OnSuperReady;
    public System.Action OnSuperActivated;
    public System.Action OnSuperEnded;
    public System.Action<float, float> OnSuperTimerChanged; 

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
        
        Instance = this;

        animator = GetComponent<Animator>();
        playerHealth = GetComponent<HealthSystem>();
    }

    public void SetPlayer(GameObject newPlayer)
    {
        animator = newPlayer.GetComponent<Animator>();
        playerHealth = newPlayer.GetComponent<HealthSystem>();
        
        
        ResetSuperState();
    }

    public void ResetSuperState()
    {
        isSuperActive = false;
        isSuperReady = false;
        isSuperAttackTriggered = false;
        currentCharge = 0f;
        superActivationTimer = 0f;
        
        if (superTimerCoroutine != null)
        {
            StopCoroutine(superTimerCoroutine);
            superTimerCoroutine = null;
        }
        
        DestroySwordFire();
        
        OnSuperChargeChanged?.Invoke(0f, maxSuperCharge);
        OnSuperTimerChanged?.Invoke(0f, superActivationDuration);
    }

    public void SetFreeLookCamera(CinemachineCamera camera)
    {
        freeLookCamera = camera;
    }

    private void Start()
    {
    }

    private void Update()
    {
    }

    public void RefreshSwordFire()
    {
    }

    public void AddCharge(float amount)
    {
        if (isSuperActive) return; 

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

        
        PushBackEnemiesInRange(initialPushBackForce);

        
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

        
        if (isSuperActive && !isSuperAttackTriggered)
        {
            Debug.Log("Super activation timer expired! Super cancelled.");
            CancelSuper();
        }
        
        superTimerCoroutine = null;
    }

    public void CancelSuper()
    {
        
        isSuperActive = false;
        isSuperAttackTriggered = false;
        currentCharge = 0f;
        superActivationTimer = 0f;

        
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

        
        EquipmentSystem equipment = GetComponent<EquipmentSystem>();
        if (equipment != null && equipment.CurrentWeapon != null)
        {
            
            currentSwordFireInstance = Instantiate(swordFirePrefab, equipment.CurrentWeapon.transform);
            currentSwordFireInstance.transform.localPosition = Vector3.zero;
            
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

        
        if (superTimerCoroutine != null)
        {
            StopCoroutine(superTimerCoroutine);
            superTimerCoroutine = null;
        }
        superActivationTimer = 0f;
        OnSuperTimerChanged?.Invoke(0f, superActivationDuration);

        
        RecentreCamera();

        
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
            
            var freeLook = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();
            if (freeLook != null)
            {
                
                freeLook.ForceCameraPosition(freeLookCamera.transform.position, Quaternion.LookRotation(transform.forward));
            }

            
            var rotationComposer = freeLookCamera.GetComponent<CinemachineRotationComposer>();
            if (rotationComposer != null)
            {
                
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

        
        if (superTimerCoroutine != null)
        {
            StopCoroutine(superTimerCoroutine);
            superTimerCoroutine = null;
        }

        
        if (animator != null)
        {
            animator.SetTrigger("move");
        }

        OnSuperChargeChanged?.Invoke(0f, maxSuperCharge);
        OnSuperTimerChanged?.Invoke(0f, superActivationDuration);
        OnSuperEnded?.Invoke();
        Debug.Log("Super ENDED!");
    }

    
    public void TimeSlowStart()
    {
        Time.timeScale = timeSlowScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        Debug.Log("Time Slow START");
    }

    
    public void TimeSlowEnd()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("Time Slow END");
    }

    
    public void TriggerRadiusDamage()
    {
        
        if (radiusDamageVFX != null)
        {
            GameObject vfx = Instantiate(radiusDamageVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radiusDamageRange);
        foreach (Collider hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                
                EquipmentSystem equipment = GetComponent<EquipmentSystem>();
                float baseDamage = 10f; 
                
                
                float radiusDamage = baseDamage * radiusDamageMultiplier;
                enemy.TakeDamage(radiusDamage);

                
                DamageText.CreateSuperDamageText(enemy.transform.position + Vector3.up, radiusDamage);

                Debug.Log($"Radius damage dealt to {enemy.name}: {radiusDamage}");
            }
            
            
            BossEnemy boss = hitCollider.GetComponent<BossEnemy>();
            if (boss != null)
            {
                
                EquipmentSystem equipment = GetComponent<EquipmentSystem>();
                float baseDamage = 10f; 
                
                
                float radiusDamage = baseDamage * radiusDamageMultiplier;
                boss.TakeDamage(radiusDamage);

                
                DamageText.CreateSuperDamageText(boss.transform.position + Vector3.up * 2f, radiusDamage);

                Debug.Log($"Radius damage dealt to BOSS {boss.name}: {radiusDamage}");
            }
        }

        
        PushBackEnemiesInRange(radiusPushBackForce);

        Debug.Log("Radius Damage TRIGGERED!");
    }

    
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
                
                Vector3 pushDirection = (enemy.transform.position - transform.position).normalized;
                pushDirection.y = 0.3f; 
                pushDirection.Normalize();

                
                Rigidbody rb = enemy.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(pushDirection * force, ForceMode.Impulse);
                }
                else
                {
                    
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

    
    
    
    public void StartFireVFX()
    {
        SpawnSwordFire();
        Debug.Log("Fire VFX STARTED (animation event)");
    }
    
    
    public void EndFireVFX()
    {
        DestroySwordFire();
        Debug.Log("Fire VFX ENDED (animation event)");
    }
    
    
    public void StartInvincibility()
    {
        if (playerHealth != null)
        {
            playerHealth.SetInvincible(true);
            Debug.Log("Invincibility STARTED (animation event)");
        }
    }
    
    
    public void EndInvincibility()
    {
        if (playerHealth != null)
        {
            playerHealth.SetInvincible(false);
            Debug.Log("Invincibility ENDED (animation event)");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radiusDamageRange);
    }
}
