using UnityEngine;

public class ShieldColliderController : MonoBehaviour
{
    [SerializeField] Collider shieldCollider;

    private void Awake()
    {
        if (shieldCollider == null)
            shieldCollider = GetComponent<Collider>();

        
        if (shieldCollider != null)
            shieldCollider.enabled = false;
    }

    public void StartBlock()
    {
        if (shieldCollider != null)
            shieldCollider.enabled = true;
    }

    public void EndBlock()
    {
        if (shieldCollider != null)
            shieldCollider.enabled = false;
    }
}
