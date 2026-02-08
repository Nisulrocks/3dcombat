using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamageDealer : MonoBehaviour
{
    bool canDealDamage;
    bool hasDealtDamage;

    [SerializeField] float weaponLength;
    [SerializeField] float weaponDamage;
    [SerializeField] LayerMask shieldLayerMask; 

    void Start()
    {
        canDealDamage = false;
        hasDealtDamage = false;
    }

    
    void Update()
    {
        if (canDealDamage && !hasDealtDamage)
        {
            RaycastHit hit;

            int playerLayerMask = 1 << 8; 
            if (Physics.Raycast(transform.position, -transform.up, out hit, weaponLength, playerLayerMask))
            {
                Debug.Log("Hit something: " + hit.transform.name);
                
                
                ShieldSystem shieldSystem = hit.transform.GetComponent<ShieldSystem>();
                if (shieldSystem != null && shieldSystem.CurrentShield != null)
                {
                    
                    Debug.Log("Attack blocked by shield!");
                    hasDealtDamage = true;
                    
                    
                    DamageText.CreateDamageText(hit.point, 0, 0); 
                    
                    
                    
                    
                    return; 
                }
                
                
                if (hit.transform.TryGetComponent(out HealthSystem health))
                {
                    health.TakeDamage(weaponDamage);
                    health.HitVFX(hit.point);
                    hasDealtDamage = true;
                    Debug.Log("hit");

                    
                    DamageText.CreateDamageText(hit.point, weaponDamage, 0);

                    
                    if (TimeStopManager.Instance != null)
                    {
                        TimeStopManager.Instance.StopTime();
                    }

                    
                    if (HitChromaticEffect.Instance != null)
                    {
                        HitChromaticEffect.Instance.TriggerHitChromatic();
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