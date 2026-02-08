using System.Collections;
using UnityEngine;

public class TimeStopManager : MonoBehaviour
{
    public static TimeStopManager Instance { get; private set; }

    [SerializeField] float stopDuration = 0.1f;
    [SerializeField] float timeScale = 0.1f;

    private Coroutine currentStopCoroutine;
    private float originalTimeScale;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            originalTimeScale = Time.timeScale;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StopTime()
    {
        StopTime(stopDuration, timeScale);
    }

    public void StopTime(float duration, float scale)
    {
        
        if (currentStopCoroutine != null)
        {
            StopCoroutine(currentStopCoroutine);
        }

        
        currentStopCoroutine = StartCoroutine(CustomTimeStopCoroutine(duration, scale));
    }

    private IEnumerator CustomTimeStopCoroutine(float duration, float scale)
    {
        
        float currentTimeScale = Time.timeScale;
        
        
        Time.timeScale = scale;
        
        
        yield return new WaitForSecondsRealtime(duration);
        
        
        Time.timeScale = originalTimeScale;
        
        
        currentStopCoroutine = null;
    }
}
