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

    
    public bool IsWeaponDrawn => currentWeaponInHand != null;

    void Start()
    {
        currentWeaponInSheath = Instantiate(weapon, weaponSheath.transform);
    }

    public void DrawWeapon()
    {
        
        if (currentWeaponInHand != null)
        {
            Debug.Log("EquipmentSystem: Weapon already in hand, skipping draw");
            return;
        }

        
        if (currentWeaponInSheath != null)
        {
            Destroy(currentWeaponInSheath);
            currentWeaponInSheath = null;
        }

        currentWeaponInHand = Instantiate(weapon, weaponHolder.transform);

        
        if (SuperSystem.Instance != null)
        {
            SuperSystem.Instance.RefreshSwordFire();
        }
    }

    public void SheathWeapon()
    {
        
        if (currentWeaponInSheath != null)
        {
            Debug.Log("EquipmentSystem: Weapon already in sheath, skipping sheath");
            return;
        }

        
        if (currentWeaponInHand != null)
        {
            Destroy(currentWeaponInHand);
            currentWeaponInHand = null;
        }

        currentWeaponInSheath = Instantiate(weapon, weaponSheath.transform);
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
        
        
    }
}