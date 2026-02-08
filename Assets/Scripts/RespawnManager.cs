using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private RespawnPoint[] respawnPoints;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject respawnVFX;
    [SerializeField] private Vector3 respawnVFXOffset = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool showRespawnPointGizmos = true;

    [Header("UI")]
    [SerializeField] private RespawnUI respawnUI;
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("Camera")]
    [SerializeField] private string freeLookCameraName = "Attack";

    private bool isRespawning = false;
    private GameObject currentRagdoll = null;
    private RespawnPoint currentRespawnPoint;
    private RespawnPoint defaultRespawnPoint;

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
        
        InitializeRespawnPoints();

        
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

        
        if (respawnUI != null)
        {
            respawnUI.Show();
            respawnUI.SetAlpha(0f);
            yield return StartCoroutine(FadeInPanel());
        }

        
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

        
        RespawnPlayer();

        
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
        if (playerPrefab == null || currentRespawnPoint == null)
        {
            Debug.LogError("RespawnManager: Player prefab or current respawn point not assigned!");
            return;
        }

        
        if (currentRagdoll != null)
        {
            Destroy(currentRagdoll);
            currentRagdoll = null;
        }

        
        if (respawnVFX != null)
        {
            Vector3 vfxPosition = currentRespawnPoint.Position + respawnVFXOffset;
            GameObject vfx = Instantiate(respawnVFX, vfxPosition, Quaternion.identity);
            Destroy(vfx, 3f); 
        }

        
        GameObject newPlayer = Instantiate(playerPrefab, currentRespawnPoint.Position, currentRespawnPoint.Rotation);

        
        HealthSystem healthSystem = newPlayer.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.ResetHealth();
        }

        
        Character character = newPlayer.GetComponent<Character>();
        if (character != null)
        {
            
            if (character.movementSM != null && character.standing != null)
            {
                character.movementSM.ChangeState(character.standing);
            }
        }

        
        UpdateCinemachineCameras(newPlayer.transform, newPlayer.GetComponent<Animator>());

        
        UpdateEnemyTargets(newPlayer);

        
        UpdateCameraSoftLock(newPlayer);

        
        UpdateSuperSystem(newPlayer);

        
        UpdatePlayerHUD();

        Debug.Log($"Player respawned at: {currentRespawnPoint.PointName} ({currentRespawnPoint.Position})");
    }

    #region Respawn Point Management
    private void InitializeRespawnPoints()
    {
        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            Debug.LogError("RespawnManager: No respawn points assigned!");
            return;
        }

        
        defaultRespawnPoint = null;
        foreach (RespawnPoint point in respawnPoints)
        {
            if (point.IsDefault)
            {
                defaultRespawnPoint = point;
                break;
            }
        }

        
        if (defaultRespawnPoint == null)
        {
            defaultRespawnPoint = respawnPoints[0];
            Debug.LogWarning($"RespawnManager: No default respawn point found. Using '{defaultRespawnPoint.PointName}' as default.");
        }

        
        currentRespawnPoint = defaultRespawnPoint;
        Debug.Log($"RespawnManager initialized with {respawnPoints.Length} respawn points. Current: {currentRespawnPoint.PointName}");
    }

    
    
    
    
    
    public bool SetActiveRespawnPoint(string pointName)
    {
        if (string.IsNullOrEmpty(pointName))
        {
            Debug.LogWarning("RespawnManager: Cannot set respawn point with null or empty name");
            return false;
        }

        foreach (RespawnPoint point in respawnPoints)
        {
            if (point.PointName == pointName)
            {
                RespawnPoint previousPoint = currentRespawnPoint;
                currentRespawnPoint = point;
                
                Debug.Log($"Respawn point changed from '{previousPoint.PointName}' to '{point.PointName}'");
                return true;
            }
        }

        Debug.LogWarning($"RespawnManager: Respawn point '{pointName}' not found");
        return false;
    }

    
    
    
    public RespawnPoint GetCurrentRespawnPoint()
    {
        return currentRespawnPoint;
    }

    
    
    
    public RespawnPoint[] GetAllRespawnPoints()
    {
        return respawnPoints;
    }

    
    
    
    public void ResetToDefaultRespawnPoint()
    {
        if (defaultRespawnPoint != null)
        {
            currentRespawnPoint = defaultRespawnPoint;
            Debug.Log($"Respawn point reset to default: {defaultRespawnPoint.PointName}");
        }
    }
    #endregion

    private void UpdateCinemachineCameras(Transform newTarget, Animator newAnimator)
    {
        
        CinemachineCamera[] cinemachineCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        
        foreach (CinemachineCamera cam in cinemachineCameras)
        {
            
            cam.Target.TrackingTarget = newTarget;
            
            
            if (cam.Target.LookAtTarget != null)
            {
                cam.Target.LookAtTarget = newTarget;
            }
            
            Debug.Log($"Updated Cinemachine camera '{cam.name}' to track new player");
        }

        
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

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (!showRespawnPointGizmos || respawnPoints == null) return;

        foreach (RespawnPoint point in respawnPoints)
        {
            if (point.Transform == null) continue;

            
            Gizmos.color = point.IsDefault ? Color.green : Color.blue;
            if (point == currentRespawnPoint)
            {
                Gizmos.color = Color.red; 
            }

            
            Gizmos.DrawWireSphere(point.Position, 0.5f);

            
            Gizmos.DrawLine(point.Position, point.Position + point.Transform.forward * 1f);

            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(point.Position + Vector3.up * 0.7f, 
                $"{point.PointName} {(point.IsDefault ? "(Default)" : "")} {(point == currentRespawnPoint ? "[CURRENT]" : "")}");
            #endif
        }
    }
    #endregion
}
