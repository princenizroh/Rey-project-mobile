using UnityEngine;

public class FloatingPanelMenu : MonoBehaviour
{
    [Header("Panel Settings")]
    public GameObject floatingPanel; // Drag your text GameObject here
    
    private bool playerInTrigger = false; // Prevent multiple rotations
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerInTrigger)
        {
            playerInTrigger = true;
            Debug.Log("[FloatingPanelMenu] Player entered trigger area.");
            
            // Get player position
            Transform playerTransform = other.transform;
            Vector3 playerPosition = playerTransform.position;
            
            if (floatingPanel != null)
            {
                Debug.Log($"Player position: {playerPosition}");
                Debug.Log($"Text position: {floatingPanel.transform.position}");
                Debug.Log($"Current rotation: {floatingPanel.transform.rotation.eulerAngles}");
                
                // Calculate direction from text to player
                Vector3 direction = (playerPosition - floatingPanel.transform.position).normalized;
                Debug.Log($"Direction vector: {direction}");
                
                // Only rotate if there's a meaningful distance
                if (direction.magnitude > 0.1f)
                {
                    // Calculate rotation to face the player (only Y-axis rotation to prevent weird tilting)
                    Vector3 lookDirection = new Vector3(direction.x, 0, direction.z).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    Debug.Log($"Target rotation: {targetRotation.eulerAngles}");
                    
                    // Apply the rotation
                    floatingPanel.transform.rotation = targetRotation;
                    Debug.Log($"New rotation: {floatingPanel.transform.rotation.eulerAngles}");
                }
                else
                {
                    Debug.LogWarning("Player too close to text, skipping rotation");
                }
                
                // Show the text
                floatingPanel.SetActive(true);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false; // Reset flag when player exits
            if (floatingPanel != null)
            {
                // Hide the text
                floatingPanel.SetActive(false);
            }
        }
    }
}
