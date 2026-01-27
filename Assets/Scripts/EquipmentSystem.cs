using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class EquipmentSystem : MonoBehaviour
{
    [SerializeField] GameObject weaponHolder;
    [SerializeField] GameObject weapon;
    [SerializeField] GameObject weaponSheath;
    [SerializeField] ShieldSystem shieldSystem;
 
 
    GameObject currentWeaponInHand;
    GameObject currentWeaponInSheath;

    public GameObject CurrentWeapon => currentWeaponInHand;

    void Start()
    {
        currentWeaponInSheath = Instantiate(weapon, weaponSheath.transform);
    }
 
    public void DrawWeapon()
    {
        currentWeaponInHand = Instantiate(weapon, weaponHolder.transform);
        Destroy(currentWeaponInSheath);

        // Refresh sword fire VFX if super is active
        if (SuperSystem.Instance != null)
        {
            SuperSystem.Instance.RefreshSwordFire();
        }
    }
 
    public void SheathWeapon()
    {
        currentWeaponInSheath = Instantiate(weapon, weaponSheath.transform);
        Destroy(currentWeaponInHand);
    }

    public void StartDealDamage()
    {
        if (currentWeaponInHand != null)
        {
            DamageDealer damageDealer = currentWeaponInHand.GetComponentInChildren<DamageDealer>();
            if (damageDealer != null)
            {
                damageDealer.StartDealDamage();
            }

            SwordColliderController colliderController = currentWeaponInHand.GetComponentInChildren<SwordColliderController>();
            if (colliderController != null)
            {
                colliderController.StartDealDamage();
            }
        }
    }

    public void EndDealDamage()
    {
        if (currentWeaponInHand != null)
        {
            DamageDealer damageDealer = currentWeaponInHand.GetComponentInChildren<DamageDealer>();
            if (damageDealer != null)
            {
                damageDealer.EndDealDamage();
            }

            SwordColliderController colliderController = currentWeaponInHand.GetComponentInChildren<SwordColliderController>();
            if (colliderController != null)
            {
                colliderController.EndDealDamage();
            }
        }
    }

    public void StartBlock()
    {
        if (shieldSystem != null)
        {
            shieldSystem.TryStartBlock();
        }
    }

    public void EndBlock()
    {
        // No longer needed - ShieldSystem handles auto-deactivation
        // Kept for compatibility but doesn't do anything
    }
}