using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldSystem : MonoBehaviour
{
    [SerializeField] GameObject shieldHolder;
    [SerializeField] GameObject shieldPrefab;
    [SerializeField] float shieldDuration = 2f;
    [SerializeField] float shieldCooldown = 3f;

    GameObject currentShield;
    public GameObject CurrentShield => currentShield; 
    private bool isOnCooldown = false;
    private Coroutine shieldCoroutine;
    private Coroutine cooldownCoroutine;
    private Animator animator;

    public bool CanBlock => !isOnCooldown && currentShield == null;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void TryStartBlock()
    {
        if (!CanBlock) return;

        StartBlock();
    }

    private void StartBlock()
    {
        if (currentShield == null && shieldPrefab != null && shieldHolder != null)
        {
            currentShield = Instantiate(shieldPrefab, shieldHolder.transform);
            
            
            ShieldColliderController colliderController = currentShield.GetComponentInChildren<ShieldColliderController>();
            if (colliderController != null)
            {
                colliderController.StartBlock();
            }

            
            if (shieldCoroutine != null)
                StopCoroutine(shieldCoroutine);
            shieldCoroutine = StartCoroutine(ShieldDurationCoroutine());
        }
    }

    private void EndBlock()
    {
        if (currentShield != null)
        {
            
            ShieldColliderController colliderController = currentShield.GetComponentInChildren<ShieldColliderController>();
            if (colliderController != null)
            {
                colliderController.EndBlock();
            }

            Destroy(currentShield);
            currentShield = null;
        }

        
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
            shieldCoroutine = null;
        }

        
        if (animator != null)
        {
            animator.SetTrigger("move");
        }

        
        StartCooldown();
    }

    private void StartCooldown()
    {
        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);
        
        isOnCooldown = true;
        cooldownCoroutine = StartCoroutine(CooldownCoroutine());
    }

    private IEnumerator ShieldDurationCoroutine()
    {
        yield return new WaitForSeconds(shieldDuration);
        EndBlock();
    }

    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(shieldCooldown);
        isOnCooldown = false;
        cooldownCoroutine = null;
    }

    private void OnDestroy()
    {
        
        if (currentShield != null)
        {
            Destroy(currentShield);
            currentShield = null;
        }

        
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
            shieldCoroutine = null;
        }

        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }
    }

    
    public float GetCooldownProgress()
    {
        if (!isOnCooldown) return 1f;
        return 0f; 
    }
}
