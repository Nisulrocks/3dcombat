using UnityEngine;




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
            
            if (oneTimeUse && hasBeenUsed)
            {
                return;
            }

            
            if (RespawnManager.Instance != null)
            {
                bool success = RespawnManager.Instance.SetActiveRespawnPoint(respawnPointName);
                
                if (success)
                {
                    
                    PlayActivationEffects();
                    
                    
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
        
        if (activationVFX != null)
        {
            Vector3 spawnPosition = transform.position + vfxOffset;
            GameObject vfxInstance = Instantiate(activationVFX, spawnPosition, Quaternion.identity);
            Destroy(vfxInstance, 3f); 
        }

        
        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); 
            
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
                
                Vector3 center = transform.position + capsuleCol.center;
                float radius = capsuleCol.radius;
                float height = capsuleCol.height;
                
                
                Gizmos.DrawWireSphere(center + Vector3.up * (height/2 - radius), radius);
                Gizmos.DrawWireSphere(center - Vector3.up * (height/2 - radius), radius);
                
                
                Vector3 topPoint = center + Vector3.up * (height/2 - radius);
                Vector3 bottomPoint = center - Vector3.up * (height/2 - radius);
                Gizmos.DrawLine(topPoint + Vector3.right * radius, bottomPoint + Vector3.right * radius);
                Gizmos.DrawLine(topPoint - Vector3.right * radius, bottomPoint - Vector3.right * radius);
                Gizmos.DrawLine(topPoint + Vector3.forward * radius, bottomPoint + Vector3.forward * radius);
                Gizmos.DrawLine(topPoint - Vector3.forward * radius, bottomPoint - Vector3.forward * radius);
            }
        }

        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, $"Respawn Trigger: {respawnPointName}");
        #endif
    }
}
