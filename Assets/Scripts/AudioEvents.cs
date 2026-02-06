using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioEvents : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private bool useAudioManager = true;
    [SerializeField] private AudioSource customAudioSource;
    [SerializeField] private AudioClip[] audioClips;
    [SerializeField] private Animator animator;
    [SerializeField] private float sfxCooldown = 0.1f;
    [SerializeField] private bool preventOverlap = true;

    private float lastSFXTime;
    private int lastSFXIndex = -1;
    private HashSet<int> currentlyPlayingSFX = new HashSet<int>();

    // Animation event methods
    public void PlaySFX(int index)
    {
        // Safety check: only play SFX if animator is actively playing
        if (animator != null && !animator.enabled)
        {
            return; // Don't play SFX if animator is disabled
        }

        // Overlap prevention check
        if (preventOverlap && currentlyPlayingSFX.Contains(index))
        {
            return; // Skip if this SFX is already playing
        }

        // Check if we're in a valid animation state
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // For non-looping animations, skip SFX if animation has finished
            if (!stateInfo.loop && stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(0))
            {
                return; // Animation finished, not looping, skip SFX
            }
        }

        // Simple cooldown check
        if (Time.time - lastSFXTime < sfxCooldown)
        {
            return; // Skip if SFX played too recently
        }

        AudioClip clipToPlay = null;
        
        if (useAudioManager && AudioManager.Instance != null)
        {
            // Get clip from AudioManager (you'd need to extend AudioManager to return clips)
            // For now, we'll use the local array as fallback
            if (index >= 0 && index < audioClips.Length)
            {
                clipToPlay = audioClips[index];
            }
        }
        else if (customAudioSource != null && index >= 0 && index < audioClips.Length)
        {
            clipToPlay = audioClips[index];
        }

        if (clipToPlay != null)
        {
            // Play the SFX
            if (useAudioManager && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clipToPlay);
            }
            else if (customAudioSource != null)
            {
                customAudioSource.PlayOneShot(clipToPlay);
            }

            // Track this SFX as playing
            if (preventOverlap)
            {
                currentlyPlayingSFX.Add(index);
                // Start coroutine to remove SFX from playing set when it finishes
                StartCoroutine(RemoveSFXWhenFinished(index, clipToPlay.length));
            }

            // Update last played SFX info
            lastSFXTime = Time.time;
            lastSFXIndex = index;
        }
        else
        {
            Debug.LogWarning($"AudioEvents: Cannot play SFX at index {index}. Clip not found or audio source not assigned!");
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

    // Convenience methods for common SFX (using PlaySFX with specific indices)
    public void PlayFootstep()
    {
        PlaySFX(0); // Footstep at index 0
    }

    public void PlayJump()
    {
        PlaySFX(1); // Jump at index 1
    }

    public void PlayDamage()
    {
        PlaySFX(2); // Damage at index 2
    }

    // Method to play SFX with custom cooldown (useful for different animation types)
    public void PlaySFXWithCooldown(int index, float customCooldown)
    {
        float originalCooldown = sfxCooldown;
        sfxCooldown = customCooldown;
        PlaySFX(index);
        sfxCooldown = originalCooldown;
    }

    // Method to reset SFX state (call this when stopping animations)
    public void ResetSFXState()
    {
        lastSFXTime = 0f;
        lastSFXIndex = -1;
        currentlyPlayingSFX.Clear();
    }

    // Method to force stop all SFX
    public void StopAllSFX()
    {
        if (customAudioSource != null)
        {
            customAudioSource.Stop();
        }
        
        if (useAudioManager && AudioManager.Instance != null)
        {
            // You could extend AudioManager to have a StopAllSFX method
            Debug.Log("AudioEvents: StopAllSFX called (requires AudioManager extension)");
        }
        
        // Clear all playing SFX
        currentlyPlayingSFX.Clear();
    }

    // Method to manually stop a specific SFX
    public void StopSFX(int index)
    {
        if (preventOverlap && currentlyPlayingSFX.Contains(index))
        {
            currentlyPlayingSFX.Remove(index);
        }
    }

    // Coroutine to remove SFX from playing set when it finishes
    private IEnumerator RemoveSFXWhenFinished(int index, float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        
        // Remove from playing set
        if (currentlyPlayingSFX.Contains(index))
        {
            currentlyPlayingSFX.Remove(index);
        }
    }

    // Debug method to check if an SFX is currently playing
    public bool IsSFXPlaying(int index)
    {
        return currentlyPlayingSFX.Contains(index);
    }

    // Debug method to get all currently playing SFX
    public int[] GetCurrentlyPlayingSFX()
    {
        int[] playingArray = new int[currentlyPlayingSFX.Count];
        currentlyPlayingSFX.CopyTo(playingArray);
        return playingArray;
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
