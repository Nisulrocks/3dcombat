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

    
    public void PlaySFX(int index)
    {
        
        if (animator != null && !animator.enabled)
        {
            return; 
        }

        
        if (preventOverlap && currentlyPlayingSFX.Contains(index))
        {
            return; 
        }

        
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            
            if (!stateInfo.loop && stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(0))
            {
                return; 
            }
        }

        
        if (Time.time - lastSFXTime < sfxCooldown)
        {
            return; 
        }

        AudioClip clipToPlay = null;
        
        if (useAudioManager && AudioManager.Instance != null)
        {
            
            
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
            
            if (useAudioManager && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clipToPlay);
            }
            else if (customAudioSource != null)
            {
                customAudioSource.PlayOneShot(clipToPlay);
            }

            
            if (preventOverlap)
            {
                currentlyPlayingSFX.Add(index);
                
                StartCoroutine(RemoveSFXWhenFinished(index, clipToPlay.length));
            }

            
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
            
            Debug.Log($"AudioEvents: Playing SFX by name '{clipName}' (requires AudioManager extension)");
        }
        else if (customAudioSource != null)
        {
            
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

    
    public void PlayFootstep()
    {
        PlaySFX(0); 
    }

    public void PlayJump()
    {
        PlaySFX(1); 
    }

    public void PlayDamage()
    {
        PlaySFX(2); 
    }

    
    public void PlaySFXWithCooldown(int index, float customCooldown)
    {
        float originalCooldown = sfxCooldown;
        sfxCooldown = customCooldown;
        PlaySFX(index);
        sfxCooldown = originalCooldown;
    }

    
    public void ResetSFXState()
    {
        lastSFXTime = 0f;
        lastSFXIndex = -1;
        currentlyPlayingSFX.Clear();
    }

    
    public void StopAllSFX()
    {
        if (customAudioSource != null)
        {
            customAudioSource.Stop();
        }
        
        if (useAudioManager && AudioManager.Instance != null)
        {
            
            Debug.Log("AudioEvents: StopAllSFX called (requires AudioManager extension)");
        }
        
        
        currentlyPlayingSFX.Clear();
    }

    
    public void StopSFX(int index)
    {
        if (preventOverlap && currentlyPlayingSFX.Contains(index))
        {
            currentlyPlayingSFX.Remove(index);
        }
    }

    
    private IEnumerator RemoveSFXWhenFinished(int index, float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        
        
        if (currentlyPlayingSFX.Contains(index))
        {
            currentlyPlayingSFX.Remove(index);
        }
    }

    
    public bool IsSFXPlaying(int index)
    {
        return currentlyPlayingSFX.Contains(index);
    }

    
    public int[] GetCurrentlyPlayingSFX()
    {
        int[] playingArray = new int[currentlyPlayingSFX.Count];
        currentlyPlayingSFX.CopyTo(playingArray);
        return playingArray;
    }

    
    public void AddAudioClip(AudioClip clip)
    {
        if (clip != null)
        {
            System.Array.Resize(ref audioClips, audioClips.Length + 1);
            audioClips[audioClips.Length - 1] = clip;
        }
    }
}
