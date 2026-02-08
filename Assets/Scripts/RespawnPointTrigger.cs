using UnityEngine;

/// <summary>
/// Trigger collider that activates a specific respawn point when the player enters
/// </summary>
public class RespawnPointTrigger : MonoBehaviour
{
    [Header("Respawn Point Settings")]
    [SerializeField] private string respawnPointName;
    [SerializeField] private bool oneTimeUse = false;
    [SerializeField] private GameObject activationVFX;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;
    [SerializeField] private AudioClip activationSound;

    private bool hasBeenUsed = false;

    private void Start()
    {
        // Ensure this is a trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"RespawnPointTrigger '{gameObject.name}' collider was not set to trigger. Auto-fixed.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Check if this is a one-time trigger that's already been used
            if (oneTimeUse && hasBeenUsed)
            {
                return;
            }

            // Try to activate the respawn point
            if (RespawnManager.Instance != null)
            {
                bool success = RespawnManager.Instance.SetActiveRespawnPoint(respawnPointName);
                
                if (success)
                {
                    // Play activation effects
                    PlayActivationEffects();
                    
                    // Mark as used if it's one-time
                    if (oneTimeUse)
                    {
                        hasBeenUsed = true;
                    }

                    Debug.Log($"Respawn point '{respawnPointName}' activated by player trigger", this);
                }
                else
                {
                    Debug.LogWarning($"Failed to activate respawn point '{respawnPointName}'. Point not found in RespawnManager.", this);
                }
            }
            else
            {
                Debug.LogError("RespawnManager instance not found!", this);
            }
        }
    }

    private void PlayActivationEffects()
    {
        // Spawn VFX
        if (activationVFX != null)
        {
            Vector3 spawnPosition = transform.position + vfxOffset;
            GameObject vfxInstance = Instantiate(activationVFX, spawnPosition, Quaternion.identity);
            Destroy(vfxInstance, 3f); // Auto-destroy after 3 seconds
        }

        // Play sound
        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        // Draw trigger volume
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
            
            if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCol.center, boxCol.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius);
            }
            else if (col is CapsuleCollider capsuleCol)
            {
                // Draw capsule as a wireframe
                Vector3 center = transform.position + capsuleCol.center;
                float radius = capsuleCol.radius;
                float height = capsuleCol.height;
                
                // Draw top and bottom spheres
                Gizmos.DrawWireSphere(center + Vector3.up * (height/2 - radius), radius);
                Gizmos.DrawWireSphere(center - Vector3.up * (height/2 - radius), radius);
                
                // Draw connecting lines
                Vector3 topPoint = center + Vector3.up * (height/2 - radius);
                Vector3 bottomPoint = center - Vector3.up * (height/2 - radius);
                Gizmos.DrawLine(topPoint + Vector3.right * radius, bottomPoint + Vector3.right * radius);
                Gizmos.DrawLine(topPoint - Vector3.right * radius, bottomPoint - Vector3.right * radius);
                Gizmos.DrawLine(topPoint + Vector3.forward * radius, bottomPoint + Vector3.forward * radius);
                Gizmos.DrawLine(topPoint - Vector3.forward * radius, bottomPoint - Vector3.forward * radius);
            }
        }

        // Draw label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, $"Respawn Trigger: {respawnPointName}");
        #endif
    }
}
