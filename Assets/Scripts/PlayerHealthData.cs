using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthData", menuName = "Game Data/Player Health Data")]
public class PlayerHealthData : ScriptableObject
{
    [Header("Health Settings")]
    public float maxHealth = 100f;

    [Header("Auto Healing")]
    public bool enableAutoHeal = true;
    public float healDelay = 10f; 
    public float healRate = 5f; 
    public float healInterval = 0.5f; 

    [Header("VFX")]
    public GameObject hitVFX;
    public GameObject healVFX;
    public Vector3 healVFXOffset = Vector3.zero;

    [Header("Death")]
    public GameObject ragdoll;
}
