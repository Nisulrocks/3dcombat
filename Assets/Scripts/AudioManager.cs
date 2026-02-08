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
    [SerializeField] private float normalBGMVolume = 1f;
    [SerializeField] private float bossBGMVolume = 1f;

    [Header("SFX Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private List<AudioClip> sfxClips = new List<AudioClip>();

    private Transform playerTransform;
    private Transform bossTransform;
    private bool isBossInRange;
    private Coroutine bgmFadeCoroutine;

    private void Awake()
    {
        
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
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        GameObject boss = GameObject.FindWithTag("Boss"); 
        if (boss != null)
        {
            bossTransform = boss.transform;
        }

        
        if (normalBGMSource != null)
        {
            normalBGMSource.Play();
            normalBGMSource.volume = normalBGMVolume;
        }

        if (bossBGMSource != null)
        {
            bossBGMSource.Play();
            bossBGMSource.volume = 0f;
        }

        
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
                        
                        SwitchBGM(true);
                    }
                    else
                    {
                        
                        SwitchBGM(false);
                    }
                }
            }

            yield return new WaitForSeconds(0.5f); 
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
                float targetVolume = toBoss ? 0f : normalBGMVolume;
                normalBGMSource.volume = Mathf.Lerp(startNormalVolume, targetVolume, normalizedTime);
            }

            if (bossBGMSource != null)
            {
                float targetVolume = toBoss ? bossBGMVolume : 0f;
                bossBGMSource.volume = Mathf.Lerp(startBossVolume, targetVolume, normalizedTime);
            }

            yield return null;
        }

        
        if (normalBGMSource != null)
        {
            normalBGMSource.volume = toBoss ? 0f : normalBGMVolume;
        }

        if (bossBGMSource != null)
        {
            bossBGMSource.volume = toBoss ? bossBGMVolume : 0f;
        }

        bgmFadeCoroutine = null;
    }

    
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

    
    public void SetNormalBGMVolume(float volume)
    {
        normalBGMVolume = Mathf.Clamp01(volume);
        
        
        if (normalBGMSource != null && !isBossInRange)
        {
            normalBGMSource.volume = normalBGMVolume;
        }
    }

    public void SetBossBGMVolume(float volume)
    {
        bossBGMVolume = Mathf.Clamp01(volume);
        
        
        if (bossBGMSource != null && isBossInRange)
        {
            bossBGMSource.volume = bossBGMVolume;
        }
    }

    public float GetNormalBGMVolume()
    {
        return normalBGMVolume;
    }

    public float GetBossBGMVolume()
    {
        return bossBGMVolume;
    }

    public void SetBothBGMVolumes(float normalVolume, float bossVolume)
    {
        SetNormalBGMVolume(normalVolume);
        SetBossBGMVolume(bossVolume);
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
