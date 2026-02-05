using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM Settings")]
    [SerializeField] private AudioSource normalBGMSource;
    [SerializeField] private AudioSource bossBGMSource;
    [SerializeField] private float bgmFadeDuration = 2f;
    [SerializeField] private float bossDetectionRange = 20f;

    [Header("SFX Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private List<AudioClip> sfxClips = new List<AudioClip>();

    private Transform playerTransform;
    private Transform bossTransform;
    private bool isBossInRange;
    private Coroutine bgmFadeCoroutine;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Find player and boss
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        GameObject boss = GameObject.FindWithTag("Boss"); // Assuming boss has "Boss" tag
        if (boss != null)
        {
            bossTransform = boss.transform;
        }

        // Start with normal BGM
        if (normalBGMSource != null)
        {
            normalBGMSource.Play();
            normalBGMSource.volume = 1f;
        }

        if (bossBGMSource != null)
        {
            bossBGMSource.Play();
            bossBGMSource.volume = 0f;
        }

        // Start checking boss distance
        StartCoroutine(CheckBossDistance());
    }

    private IEnumerator CheckBossDistance()
    {
        while (true)
        {
            if (playerTransform != null && bossTransform != null)
            {
                float distance = Vector3.Distance(playerTransform.position, bossTransform.position);
                bool bossInRange = distance <= bossDetectionRange;

                if (bossInRange != isBossInRange)
                {
                    isBossInRange = bossInRange;
                    
                    if (isBossInRange)
                    {
                        // Switch to boss BGM
                        SwitchBGM(true);
                    }
                    else
                    {
                        // Switch to normal BGM
                        SwitchBGM(false);
                    }
                }
            }

            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }

    private void SwitchBGM(bool toBoss)
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        bgmFadeCoroutine = StartCoroutine(FadeBGM(toBoss));
    }

    private IEnumerator FadeBGM(bool toBoss)
    {
        float timer = 0f;
        float startNormalVolume = normalBGMSource != null ? normalBGMSource.volume : 0f;
        float startBossVolume = bossBGMSource != null ? bossBGMSource.volume : 0f;

        while (timer < bgmFadeDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / bgmFadeDuration;

            if (normalBGMSource != null)
            {
                float targetVolume = toBoss ? 0f : 1f;
                normalBGMSource.volume = Mathf.Lerp(startNormalVolume, targetVolume, normalizedTime);
            }

            if (bossBGMSource != null)
            {
                float targetVolume = toBoss ? 1f : 0f;
                bossBGMSource.volume = Mathf.Lerp(startBossVolume, targetVolume, normalizedTime);
            }

            yield return null;
        }

        // Ensure final values are set
        if (normalBGMSource != null)
        {
            normalBGMSource.volume = toBoss ? 0f : 1f;
        }

        if (bossBGMSource != null)
        {
            bossBGMSource.volume = toBoss ? 1f : 0f;
        }

        bgmFadeCoroutine = null;
    }

    // SFX Methods
    public void PlaySFX(int index)
    {
        if (index >= 0 && index < sfxClips.Count && sfxClips[index] != null)
        {
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(sfxClips[index]);
            }
            else
            {
                Debug.LogWarning("AudioManager: SFX Source not assigned!");
            }
        }
        else
        {
            Debug.LogWarning($"AudioManager: SFX index {index} is invalid or clip is null!");
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("AudioManager: SFX clip is null or SFX Source not assigned!");
        }
    }

    // Player SFX Methods
    public void PlayFootstepSound()
    {
        // You can customize this to use specific footstep indices
        PlaySFX(0); // Assuming index 0 is footstep
    }

    public void PlayJumpSound()
    {
        PlaySFX(1); // Assuming index 1 is jump
    }

    public void PlayDamageSound()
    {
        PlaySFX(2); // Assuming index 2 is damage
    }

    // Utility Methods
    public void AddSFXClip(AudioClip clip)
    {
        if (clip != null)
        {
            sfxClips.Add(clip);
        }
    }

    public void SetBossDetectionRange(float range)
    {
        bossDetectionRange = range;
    }

    private void OnDrawGizmosSelected()
    {
        if (bossTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bossTransform.position, bossDetectionRange);
        }
    }
}
