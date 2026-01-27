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
        // Cancel any existing time stop
        if (currentStopCoroutine != null)
        {
            StopCoroutine(currentStopCoroutine);
        }

        // Start new time stop
        currentStopCoroutine = StartCoroutine(CustomTimeStopCoroutine(duration, scale));
    }

    private IEnumerator CustomTimeStopCoroutine(float duration, float scale)
    {
        // Store current time scale (in case it was already modified)
        float currentTimeScale = Time.timeScale;
        
        // Apply time stop
        Time.timeScale = scale;
        
        // Wait for the duration (in real time)
        yield return new WaitForSecondsRealtime(duration);
        
        // Restore original time scale
        Time.timeScale = originalTimeScale;
        
        // Clear coroutine reference
        currentStopCoroutine = null;
    }
}
