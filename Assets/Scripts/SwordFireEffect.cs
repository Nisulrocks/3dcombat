using UnityEngine;

public class SwordFireEffect : MonoBehaviour
{
    [SerializeField] ParticleSystem fireParticle;

    private void Start()
    {
        // Start playing the fire particle when this prefab is spawned
        if (fireParticle != null)
        {
            fireParticle.Play();
        }
    }

    private void OnDisable()
    {
        // Stop fire when disabled
        if (fireParticle != null)
        {
            fireParticle.Stop();
        }
    }
}
