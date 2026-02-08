using UnityEngine;

public class SwordFireEffect : MonoBehaviour
{
    [SerializeField] ParticleSystem fireParticle;

    private void Start()
    {
        
        if (fireParticle != null)
        {
            fireParticle.Play();
        }
    }

    private void OnDisable()
    {
        
        if (fireParticle != null)
        {
            fireParticle.Stop();
        }
    }
}
