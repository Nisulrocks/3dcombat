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
    public GameObject CurrentShield => currentShield; // Public getter for external access
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
            
            // Enable shield collider if it has one
            ShieldColliderController colliderController = currentShield.GetComponentInChildren<ShieldColliderController>();
            if (colliderController != null)
            {
                colliderController.StartBlock();
            }

            // Start auto-deactivate timer
            if (shieldCoroutine != null)
                StopCoroutine(shieldCoroutine);
            shieldCoroutine = StartCoroutine(ShieldDurationCoroutine());
        }
    }

    private void EndBlock()
    {
        if (currentShield != null)
        {
            // Disable shield collider before destroying
            ShieldColliderController colliderController = currentShield.GetComponentInChildren<ShieldColliderController>();
            if (colliderController != null)
            {
                colliderController.EndBlock();
            }

            Destroy(currentShield);
            currentShield = null;
        }

        // Stop duration coroutine if running
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
            shieldCoroutine = null;
        }

        // Trigger move animation to return to locomotion idle
        if (animator != null)
        {
            animator.SetTrigger("move");
        }

        // Start cooldown
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
        // Clean up shield if it exists
        if (currentShield != null)
        {
            Destroy(currentShield);
            currentShield = null;
        }

        // Stop coroutines safely
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

    // For UI or debugging
    public float GetCooldownProgress()
    {
        if (!isOnCooldown) return 1f;
        return 0f; // Could implement actual progress tracking if needed
    }
}
