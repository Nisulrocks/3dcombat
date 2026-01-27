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
                    // Calculate damage with combo multiplier
                    float comboMultiplier = 1f;
                    int comboLevel = 0;
                    if (ComboManager.Instance != null)
                    {
                        comboMultiplier = ComboManager.Instance.GetDamageMultiplier();
                        comboLevel = ComboManager.Instance.GetCurrentCombo();
                        ComboManager.Instance.RegisterHit();
                    }
                    
                    float finalDamage = weaponDamage * comboMultiplier;
                    enemy.TakeDamage(finalDamage);
                    enemy.HitVFX(hit.point);
                    hasDealtDamage.Add(hit.transform.gameObject);
                    
                    // Spawn damage text at hit position
                    DamageText.CreateDamageText(hit.point, finalDamage, comboLevel);
                    
                    // Trigger time stop effect
                    if (TimeStopManager.Instance != null)
                    {
                        TimeStopManager.Instance.StopTime();
                    }
                    
                    Debug.Log($"Damage dealt: {finalDamage} (Base: {weaponDamage} x {comboMultiplier})");
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