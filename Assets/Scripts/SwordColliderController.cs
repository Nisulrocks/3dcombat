using UnityEngine;

public class SwordColliderController : MonoBehaviour
{
    [SerializeField] Collider swordCollider;

    private void Awake()
    {
        if (swordCollider == null)
            swordCollider = GetComponent<Collider>();

        // Start with collider disabled
        if (swordCollider != null)
            swordCollider.enabled = false;
    }

    public void StartDealDamage()
    {
        if (swordCollider != null)
            swordCollider.enabled = true;
    }

    public void EndDealDamage()
    {
        if (swordCollider != null)
            swordCollider.enabled = false;
    }
}
