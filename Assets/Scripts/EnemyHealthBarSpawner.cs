using System.Collections.Generic;
using UnityEngine;

public class EnemyHealthBarSpawner : MonoBehaviour
{
    [SerializeField] EnemyHealthBar healthBarPrefab;
    [SerializeField] Canvas worldSpaceCanvas;
    [SerializeField] Transform followTarget;

    private EnemyHealthBar spawned;
    private static HashSet<int> trackedEnemyIDs = new HashSet<int>();
    private bool hasSpawned = false;

    private void Start()
    {
        Enemy enemy = GetComponent<Enemy>();
        if (enemy == null) return;

        int enemyID = enemy.GetInstanceID();

        
        if (trackedEnemyIDs.Contains(enemyID) || hasSpawned)
        {
            Debug.LogWarning($"EnemyHealthBarSpawner: Enemy {enemyID} already has a health bar", this);
            return;
        }

        if (healthBarPrefab == null)
        {
            Debug.LogWarning("EnemyHealthBarSpawner: healthBarPrefab not assigned", this);
            return;
        }

        if (followTarget == null)
            followTarget = transform;

        Canvas canvasToUse = worldSpaceCanvas;
        if (canvasToUse == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].renderMode == RenderMode.WorldSpace)
                {
                    canvasToUse = canvases[i];
                    break;
                }
            }
        }

        Transform parent = canvasToUse != null ? canvasToUse.transform : null;

        spawned = parent != null
            ? Instantiate(healthBarPrefab, parent)
            : Instantiate(healthBarPrefab);

        spawned.Bind(enemy, followTarget);
        trackedEnemyIDs.Add(enemyID);
        hasSpawned = true;
        
        Debug.Log($"EnemyHealthBarSpawner: Spawned health bar for enemy {enemyID}");
    }

    private void OnDestroy()
    {
        if (spawned != null)
        {
            Destroy(spawned.gameObject);
            spawned = null;
        }

        
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            trackedEnemyIDs.Remove(enemy.GetInstanceID());
        }
    }

    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearStaticData()
    {
        trackedEnemyIDs.Clear();
    }
}
