using UnityEngine;

public class AudioEvents : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private bool useAudioManager = true;
    [SerializeField] private AudioSource customAudioSource;
    [SerializeField] private AudioClip[] audioClips;

    // Animation event methods
    public void PlaySFX(int index)
    {
        if (useAudioManager && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(index);
        }
        else if (customAudioSource != null && index >= 0 && index < audioClips.Length)
        {
            customAudioSource.PlayOneShot(audioClips[index]);
        }
        else
        {
            Debug.LogWarning($"AudioEvents: Cannot play SFX at index {index}. Check AudioManager assignment or audio clips!");
        }
    }

    public void PlaySFXByName(string clipName)
    {
        if (useAudioManager && AudioManager.Instance != null)
        {
            // Find clip by name in AudioManager (you'd need to extend AudioManager for this)
            Debug.Log($"AudioEvents: Playing SFX by name '{clipName}' (requires AudioManager extension)");
        }
        else if (customAudioSource != null)
        {
            // Find clip by name in local array
            foreach (var clip in audioClips)
            {
                if (clip != null && clip.name == clipName)
                {
                    customAudioSource.PlayOneShot(clip);
                    return;
                }
            }
            Debug.LogWarning($"AudioEvents: Clip '{clipName}' not found!");
        }
    }

    // Specific player SFX methods for convenience
    public void PlayFootstep()
    {
        if (useAudioManager && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFootstepSound();
        }
        else
        {
            PlaySFX(0); // Assuming footstep is at index 0
        }
    }

    public void PlayJump()
    {
        if (useAudioManager && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJumpSound();
        }
        else
        {
            PlaySFX(1); // Assuming jump is at index 1
        }
    }

    public void PlayDamage()
    {
        if (useAudioManager && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDamageSound();
        }
        else
        {
            PlaySFX(2); // Assuming damage is at index 2
        }
    }

    // Utility method to add clips at runtime
    public void AddAudioClip(AudioClip clip)
    {
        if (clip != null)
        {
            System.Array.Resize(ref audioClips, audioClips.Length + 1);
            audioClips[audioClips.Length - 1] = clip;
        }
    }
}
