using UnityEngine;

public class FollowTargetVFX : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = Vector3.zero;
    [SerializeField] bool followPosition = true;
    [SerializeField] bool followRotation = false;
    [SerializeField] float destroyAfterTime = 3f;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private float timer;

    private void Awake()
    {
        // Store initial local transform
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    private void Start()
    {
        timer = destroyAfterTime;
    }

    private void Update()
    {
        if (target != null)
        {
            if (followPosition)
            {
                // Follow target position with offset
                transform.position = target.position + offset;
            }

            if (followRotation)
            {
                // Follow target rotation
                transform.rotation = target.rotation;
            }
        }

        // Auto-destroy after time
        if (destroyAfterTime > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public void SetTarget(Transform newTarget, Vector3 hitOffset)
    {
        target = newTarget;
        offset = hitOffset;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        offset = initialLocalPosition;
    }
}
