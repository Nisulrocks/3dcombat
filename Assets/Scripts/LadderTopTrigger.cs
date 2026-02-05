using UnityEngine;

public class LadderTopTrigger : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private Transform ladderTopPosition; // Empty GameObject at the top
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool debugMode = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Character character = other.GetComponent<Character>();
            if (character != null)
            {
                // Check if player is in climbing state
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

        // Get player components
        CharacterController characterController = player.GetComponent<CharacterController>();
        Character character = player.GetComponent<Character>();
        
        // Exit climbing state
        if (character != null)
        {
            character.movementSM.ChangeState(character.standing);
        }

        // Fade to black
        if (ScreenFadeManager.Instance != null)
        {
            ScreenFadeManager.Instance.StartFadeToBlack(fadeDuration);
        }
        else
        {
            // Fallback: simple wait if no fade manager
            yield return new WaitForSeconds(fadeDuration * 0.5f);
        }

        // Wait for fade to complete
        yield return new WaitForSeconds(fadeDuration);

        // Disable character controller temporarily for teleport
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Teleport player to ladder top position
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

        // Re-enable character controller
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // Fade back in
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
            // Draw trigger zone
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(Vector3.zero, collider.size);
            }

            // Draw line to teleport target
            if (ladderTopPosition != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, ladderTopPosition.position);
                
                // Draw teleport target
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                Gizmos.DrawWireSphere(ladderTopPosition.position, 0.5f);
            }
        }
    }
}
