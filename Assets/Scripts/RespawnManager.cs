using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject respawnVFX;

    [Header("UI")]
    [SerializeField] private RespawnUI respawnUI;
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("Camera")]
    [SerializeField] private string freeLookCameraName = "Attack";

    private bool isRespawning = false;
    private GameObject currentRagdoll = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Hide respawn UI at start
        if (respawnUI != null)
        {
            respawnUI.Hide();
        }
    }

    public void OnPlayerDeath(Vector3 deathPosition, Quaternion deathRotation, GameObject ragdoll)
    {
        if (isRespawning) return;
        
        currentRagdoll = ragdoll;
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        isRespawning = true;

        // Show and fade in the respawn panel
        if (respawnUI != null)
        {
            respawnUI.Show();
            respawnUI.SetAlpha(0f);
            yield return StartCoroutine(FadeInPanel());
        }

        // Countdown timer
        float timeRemaining = respawnTime;
        while (timeRemaining > 0)
        {
            if (respawnUI != null)
            {
                respawnUI.UpdateText(timeRemaining);
            }
            yield return new WaitForSeconds(0.1f);
            timeRemaining -= 0.1f;
        }

        // Respawn the player
        RespawnPlayer();

        // Fade out and hide panel
        yield return StartCoroutine(FadeOutPanel());
        
        if (respawnUI != null)
        {
            respawnUI.Hide();
        }

        isRespawning = false;
    }

    private IEnumerator FadeInPanel()
    {
        if (respawnUI == null) yield break;

        float elapsed = 0f;
        respawnUI.SetAlpha(0f);

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            respawnUI.SetAlpha(Mathf.Lerp(0f, 1f, elapsed / fadeInDuration));
            yield return null;
        }

        respawnUI.SetAlpha(1f);
    }

    private IEnumerator FadeOutPanel()
    {
        if (respawnUI == null) yield break;

        float elapsed = 0f;
        float startAlpha = respawnUI.GetAlpha();

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            respawnUI.SetAlpha(Mathf.Lerp(startAlpha, 0f, elapsed / fadeInDuration));
            yield return null;
        }

        respawnUI.SetAlpha(0f);
    }

    private void RespawnPlayer()
    {
        if (playerPrefab == null || respawnPoint == null)
        {
            Debug.LogError("RespawnManager: Player prefab or respawn point not assigned!");
            return;
        }

        // Destroy the ragdoll before spawning new player
        if (currentRagdoll != null)
        {
            Destroy(currentRagdoll);
            currentRagdoll = null;
        }

        // Spawn respawn VFX at respawn point
        if (respawnVFX != null)
        {
            GameObject vfx = Instantiate(respawnVFX, respawnPoint.position, Quaternion.identity);
            Destroy(vfx, 3f); // Auto-destroy VFX after 3 seconds
        }

        // Instantiate new player at respawn point
        GameObject newPlayer = Instantiate(playerPrefab, respawnPoint.position, respawnPoint.rotation);

        // Reset player health to max
        HealthSystem healthSystem = newPlayer.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.ResetHealth();
        }

        // Reset character state
        Character character = newPlayer.GetComponent<Character>();
        if (character != null)
        {
            // Reset to standing state
            if (character.movementSM != null && character.standing != null)
            {
                character.movementSM.ChangeState(character.standing);
            }
        }

        // Update all Cinemachine cameras to track the new player
        UpdateCinemachineCameras(newPlayer.transform, newPlayer.GetComponent<Animator>());

        // Update all enemies to target the new player
        UpdateEnemyTargets(newPlayer);

        // Update CameraSoftLock with new player reference
        UpdateCameraSoftLock(newPlayer);

        // Update SuperSystem with new player reference
        UpdateSuperSystem(newPlayer);

        // Reset PlayerHUD
        UpdatePlayerHUD();

        Debug.Log("Player respawned at: " + respawnPoint.position);
    }

    private void UpdateCinemachineCameras(Transform newTarget, Animator newAnimator)
    {
        // Find all CinemachineCamera components in the scene (Cinemachine 3.x)
        CinemachineCamera[] cinemachineCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        
        foreach (CinemachineCamera cam in cinemachineCameras)
        {
            // Update tracking target
            cam.Target.TrackingTarget = newTarget;
            
            // If there's a LookAt target, update that too
            if (cam.Target.LookAtTarget != null)
            {
                cam.Target.LookAtTarget = newTarget;
            }
            
            Debug.Log($"Updated Cinemachine camera '{cam.name}' to track new player");
        }

        // Update State Driven Cameras with new animator
        CinemachineStateDrivenCamera[] stateDrivenCameras = FindObjectsByType<CinemachineStateDrivenCamera>(FindObjectsSortMode.None);
        
        foreach (CinemachineStateDrivenCamera sdCam in stateDrivenCameras)
        {
            if (newAnimator != null)
            {
                sdCam.AnimatedTarget = newAnimator;
                Debug.Log($"Updated State Driven Camera '{sdCam.name}' animator reference");
            }
        }
    }

    private void UpdateEnemyTargets(GameObject newPlayer)
    {
        // Find all enemies and update their player reference
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        
        foreach (Enemy enemy in enemies)
        {
            enemy.SetPlayer(newPlayer);
        }
        
        Debug.Log($"Updated {enemies.Length} enemies to target new player");
    }

    private void UpdateCameraSoftLock(GameObject newPlayer)
    {
        CameraSoftLock softLock = newPlayer.GetComponent<CameraSoftLock>();
        if (softLock != null)
        {
            // Find camera by name
            CinemachineCamera freeLookCam = FindCameraByName(freeLookCameraName);
            if (freeLookCam != null)
            {
                softLock.SetFreeLookCamera(freeLookCam);
                Debug.Log($"Updated CameraSoftLock with camera: {freeLookCam.name}");
            }
            else
            {
                Debug.LogWarning($"Could not find camera with name: {freeLookCameraName}");
            }
        }
    }

    private void UpdateSuperSystem(GameObject newPlayer)
    {
        SuperSystem superSystem = newPlayer.GetComponent<SuperSystem>();
        if (superSystem != null)
        {
            // Find camera by name
            CinemachineCamera freeLookCam = FindCameraByName(freeLookCameraName);
            if (freeLookCam != null)
            {
                superSystem.SetFreeLookCamera(freeLookCam);
                Debug.Log($"Updated SuperSystem with camera: {freeLookCam.name}");
            }
        }
    }

    private CinemachineCamera FindCameraByName(string cameraName)
    {
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (CinemachineCamera cam in cameras)
        {
            if (cam.name == cameraName)
            {
                return cam;
            }
        }
        return null;
    }

    private void UpdatePlayerHUD()
    {
        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.ResetHUD();
            Debug.Log("Reset PlayerHUD after respawn");
        }
    }
}
