using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] Color maxHealthColor = Color.red;
    [SerializeField] Color midHealthColor = new Color(1f, 0.5f, 0f); // orange
    [SerializeField] Color lowHealthColor = Color.yellow;
    [SerializeField] float midThreshold = 0.5f;
    [SerializeField] float lowThreshold = 0.25f;

    private Enemy enemy;
    private Transform target;
    private Camera cam;

    public void Bind(Enemy enemyToBind, Transform followTarget)
    {
        enemy = enemyToBind;
        target = followTarget;
        cam = Camera.main;

        if (enemy != null)
        {
            enemy.OnHealthChanged += HandleHealthChanged;
            enemy.OnDied += HandleDied;

            HandleHealthChanged(enemy.CurrentHealth, enemy.MaxHealth);
        }
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + worldOffset;
        }

        if (cam != null)
        {
            transform.rotation = cam.transform.rotation;
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (slider == null || enemy == null) return;

        // Validate that the values match what the enemy actually has
        // This prevents stale event data from affecting the health bar
        if (Mathf.Abs(current - enemy.CurrentHealth) > 0.01f || Mathf.Abs(max - enemy.MaxHealth) > 0.01f)
        {
            Debug.LogWarning($"EnemyHealthBar: Received stale health data. Event: {current}/{max}, Actual: {enemy.CurrentHealth}/{enemy.MaxHealth}");
            current = enemy.CurrentHealth;
            max = enemy.MaxHealth;
        }

        float normalized = max <= 0f ? 0f : current / max;
        
        slider.value = normalized;

        UpdateFillColor(normalized);
    }

    private void UpdateFillColor(float normalized)
    {
        Image fill = slider.fillRect?.GetComponent<Image>();
        if (fill == null) return;

        if (normalized >= midThreshold)
            fill.color = maxHealthColor;
        else if (normalized >= lowThreshold)
            fill.color = midHealthColor;
        else
            fill.color = lowHealthColor;
    }

    private void HandleDied()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (enemy != null)
        {
            enemy.OnHealthChanged -= HandleHealthChanged;
            enemy.OnDied -= HandleDied;
        }
    }
}
