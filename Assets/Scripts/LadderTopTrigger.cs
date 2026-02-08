using UnityEngine;

public class LadderTopTrigger : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private Transform ladderTopPosition; 
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool debugMode = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Character character = other.GetComponent<Character>();
            if (character != null)
            {
                
                if (character.movementSM.currentState is ClimbingState)
                {
                    StartCoroutine(TeleportToTop(other.gameObject));
                }
            }
        }
    }

    private System.Collections.IEnumerator TeleportToTop(GameObject player)
    {
        if (debugMode)
            Debug.Log("LadderTopTrigger: Starting teleport to ladder top");

        
        CharacterController characterController = player.GetComponent<CharacterController>();
        Character character = player.GetComponent<Character>();
        
        
        if (character != null)
        {
            character.movementSM.ChangeState(character.standing);
        }

        
        if (ScreenFadeManager.Instance != null)
        {
            ScreenFadeManager.Instance.StartFadeToBlack(fadeDuration);
        }
        else
        {
            
            yield return new WaitForSeconds(fadeDuration * 0.5f);
        }

        
        yield return new WaitForSeconds(fadeDuration);

        
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        
        if (ladderTopPosition != null)
        {
            player.transform.position = ladderTopPosition.position;
            player.transform.rotation = ladderTopPosition.rotation;
            
            if (debugMode)
                Debug.Log($"LadderTopTrigger: Teleported player to {ladderTopPosition.position}");
        }
        else
        {
            Debug.LogError("LadderTopTrigger: Ladder top position not assigned!");
        }

        
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        
        if (ScreenFadeManager.Instance != null)
        {
            ScreenFadeManager.Instance.StartFadeFromBlack(fadeDuration);
        }

        if (debugMode)
            Debug.Log("LadderTopTrigger: Teleport complete");
    }

    private void OnDrawGizmos()
    {
        if (debugMode)
        {
            
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(Vector3.zero, collider.size);
            }

            
            if (ladderTopPosition != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, ladderTopPosition.position);
                
                
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                Gizmos.DrawWireSphere(ladderTopPosition.position, 0.5f);
            }
        }
    }
}
