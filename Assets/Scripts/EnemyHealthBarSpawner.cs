using System.Collections.Generic;
using UnityEngine;

public class EnemyHealthBarSpawner : MonoBehaviour
{
    [SerializeField] EnemyHealthBar healthBarPrefab;
    [SerializeField] Canvas worldSpaceCanvas;
    [SerializeField] Transform followTarget;

    private EnemyHealthBar spawned;
    private static HashSet<Enemy> trackedEnemies = new HashSet<Enemy>();

    private void Start()
    {
        Enemy enemy = GetComponent<Enemy>();
        if (enemy == null) return;

        // Prevent multiple health bars for the same enemy
        if (trackedEnemies.Contains(enemy))
        {
            Debug.LogWarning("EnemyHealthBarSpawner: Enemy already has a health bar", this);
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
            Canvas[] canvases = FindObjectsOfType<Canvas>();
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
        trackedEnemies.Add(enemy);
    }

    private void OnDestroy()
    {
        if (spawned != null)
        {
            Destroy(spawned.gameObject);
            spawned = null;
        }

        // Remove enemy from tracked set
        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            trackedEnemies.Remove(enemy);
        }
    }
}
