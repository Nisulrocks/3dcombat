using UnityEngine;
using System.Collections;

public class Trail : MonoBehaviour
{

    public float activeTime = 2f;
    public float meshRefreshRate = 0.1f;

    public Transform positionToSpawn;

    [Header("Shader Settings")]
    public Material mat;
    public float rate = 0.1f;
    public float refreshRate = 0.05f;
    public string shaderVarRef;
    
    [Header("Trail Settings")]
    private bool isTrailActive;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    public float DestroyTime = 2f;

    // Animation event methods
    public void StartMeshTrail()
    {
        if (!isTrailActive)
        {
            isTrailActive = true;
            StartCoroutine(ActiveTrail(activeTime));
        }
    }

    public void EndMeshTrail()
    {
        isTrailActive = false;
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

                meshRenderer.material = mat;

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
