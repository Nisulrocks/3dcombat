using UnityEngine;
using System.Collections;

public class Trail : MonoBehaviour
{

    public float activeTime = 2f;
    public float meshRefreshRate = 0.1f;

    public Transform positionToSpawn;

    [Header("Shader Settings")]
    public Material[] trailMaterials; // Array of materials to choose from
    public float rate = 0.1f;
    public float refreshRate = 0.05f;
    public string shaderVarRef;
    
    [Header("Trail Settings")]
    private bool isTrailActive;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    public float DestroyTime = 2f;
    private int currentMaterialIndex = 0; // Store current material index

    // Animation event methods
    public void StartMeshTrail(int materialIndex)
    {
        if (!isTrailActive)
        {
            // Validate material index
            if (trailMaterials == null || trailMaterials.Length == 0)
            {
                Debug.LogError("Trail: No materials assigned to trailMaterials array!");
                return;
            }
            
            // Clamp index to valid range
            currentMaterialIndex = Mathf.Clamp(materialIndex, 0, trailMaterials.Length - 1);
            
            isTrailActive = true;
            StartCoroutine(ActiveTrail(activeTime));
        }
    }

    // Overload method without parameter for backward compatibility
    public void StartMeshTrail()
    {
        StartMeshTrail(0); // Default to first material
    }

    public void EndMeshTrail()
    {
        isTrailActive = false;
    }

    // Helper method to get the number of available materials
    public int GetMaterialCount()
    {
        return trailMaterials != null ? trailMaterials.Length : 0;
    }

    // Helper method to validate material setup
    public bool ValidateMaterials()
    {
        if (trailMaterials == null || trailMaterials.Length == 0)
        {
            Debug.LogWarning("Trail: No materials assigned!");
            return false;
        }
        
        for (int i = 0; i < trailMaterials.Length; i++)
        {
            if (trailMaterials[i] == null)
            {
                Debug.LogWarning($"Trail: Material at index {i} is null!");
                return false;
            }
        }
        
        return true;
    }

    IEnumerator ActiveTrail(float timeActive)
    {
        // Initialize skinned mesh renderers once
        if(skinnedMeshRenderers == null)
        {
            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        while(timeActive > 0 && isTrailActive)
        {
            timeActive -= meshRefreshRate;

            // Create mesh trail every frame
            for(int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                GameObject gObj = new GameObject();
                gObj.transform.SetPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);
                
                MeshRenderer meshRenderer = gObj.AddComponent<MeshRenderer>();
                MeshFilter meshFilter = gObj.AddComponent<MeshFilter>();

                Mesh mesh = new Mesh();
                skinnedMeshRenderers[i].BakeMesh(mesh);
                meshFilter.mesh = mesh;

                // Use material from array based on current index
                meshRenderer.material = trailMaterials[currentMaterialIndex];

                StartCoroutine(AnimateMaterialFloat(meshRenderer.material, 0f, rate, refreshRate));

                Destroy(gObj, DestroyTime);
            }

            yield return new WaitForSeconds(meshRefreshRate);
        }
        isTrailActive = false;
    }

    IEnumerator AnimateMaterialFloat(Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat(shaderVarRef);

        while(valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat(shaderVarRef, valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
