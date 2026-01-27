using System.Collections.Generic;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    bool canDealDamage;
    List<GameObject> hasDealtDamage;

    [SerializeField] float weaponLength;
    [SerializeField] float weaponDamage;
    
    void Start()
    {
        canDealDamage = false;
        hasDealtDamage = new List<GameObject>();
    }

    void Update()
    {
        if (canDealDamage)
        {
            RaycastHit hit;

            int layerMask = 1 << 9;
            if (Physics.Raycast(transform.position, -transform.up, out hit, weaponLength, layerMask))
            {
                if (hit.transform.TryGetComponent(out Enemy enemy) && !hasDealtDamage.Contains(hit.transform.gameObject))
                {
                    // Check if super is active
                    bool isSuperActive = SuperSystem.Instance != null && SuperSystem.Instance.IsSuperActive;
                    
                    // Calculate damage with combo multiplier
                    float comboMultiplier = 1f;
                    int comboLevel = 0;
                    if (ComboManager.Instance != null && !isSuperActive)
                    {
                        comboMultiplier = ComboManager.Instance.GetDamageMultiplier();
                        comboLevel = ComboManager.Instance.GetCurrentCombo();
                        ComboManager.Instance.RegisterHit();
                    }
                    
                    // Apply super damage multiplier if active
                    float superMultiplier = 1f;
                    if (isSuperActive && SuperSystem.Instance != null)
                    {
                        superMultiplier = SuperSystem.Instance.SuperDamageMultiplier;
                    }
                    
                    float finalDamage = weaponDamage * comboMultiplier * superMultiplier;
                    enemy.TakeDamage(finalDamage);
                    enemy.HitVFX(hit.point);
                    hasDealtDamage.Add(hit.transform.gameObject);
                    
                    // Spawn damage text at hit position
                    if (isSuperActive)
                    {
                        DamageText.CreateSuperDamageText(hit.point, finalDamage);
                    }
                    else
                    {
                        DamageText.CreateDamageText(hit.point, finalDamage, comboLevel);
                    }
                    
                    // Add super charge on hit (only if not in super mode)
                    if (!isSuperActive && SuperSystem.Instance != null)
                    {
                        SuperSystem.Instance.AddChargeFromHit();
                    }
                    
                    // Trigger time stop effect (only for normal attacks, super has its own time control)
                    if (!isSuperActive && TimeStopManager.Instance != null)
                    {
                        TimeStopManager.Instance.StopTime();
                    }
                    
                    Debug.Log($"Damage dealt: {finalDamage} (Base: {weaponDamage} x Combo: {comboMultiplier} x Super: {superMultiplier})");
                }
            }
        }
    }
    
    public void StartDealDamage()
    {
        canDealDamage = true;
        hasDealtDamage.Clear();
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