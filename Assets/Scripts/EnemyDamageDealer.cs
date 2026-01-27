using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamageDealer : MonoBehaviour
{
    bool canDealDamage;
    bool hasDealtDamage;

    [SerializeField] float weaponLength;
    [SerializeField] float weaponDamage;
    [SerializeField] LayerMask shieldLayerMask; // Layer for shield colliders

    void Start()
    {
        canDealDamage = false;
        hasDealtDamage = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (canDealDamage && !hasDealtDamage)
        {
            RaycastHit hit;

            int playerLayerMask = 1 << 8; // Player layer
            if (Physics.Raycast(transform.position, -transform.up, out hit, weaponLength, playerLayerMask))
            {
                Debug.Log("Hit something: " + hit.transform.name);
                
                // Check if player has an active shield
                ShieldSystem shieldSystem = hit.transform.GetComponent<ShieldSystem>();
                if (shieldSystem != null && shieldSystem.CurrentShield != null)
                {
                    // Shield blocked the attack!
                    Debug.Log("Attack blocked by shield!");
                    hasDealtDamage = true;
                    
                    // Show "BLOCKED" damage text
                    DamageText.CreateDamageText(hit.point, 0, 0); // 0 damage, no combo
                    
                    // Optional: Play block effect/sound here
                    // Could add shield impact VFX or sound
                    
                    return; // Don't apply damage
                }
                
                // No shield active, apply damage normally
                if (hit.transform.TryGetComponent(out HealthSystem health))
                {
                    health.TakeDamage(weaponDamage);
                    health.HitVFX(hit.point);
                    hasDealtDamage = true;
                    Debug.Log("hit");

                    // Spawn damage text at hit position (enemy damage doesn't use combo)
                    DamageText.CreateDamageText(hit.point, weaponDamage, 0);

                    // Trigger time stop effect
                    if (TimeStopManager.Instance != null)
                    {
                        TimeStopManager.Instance.StopTime();
                    }
                }
            }
        }
    }
    public void StartDealDamage()
    {
        canDealDamage = true;
        hasDealtDamage = false;
    }
    public void EndDealDamage()
    {
        canDealDamage = false;
    }
 
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position - transform.up * weaponLength);
    }
}