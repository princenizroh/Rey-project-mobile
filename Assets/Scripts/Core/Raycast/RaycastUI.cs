using UnityEngine;

public class RaycastUI : MonoBehaviour
{
    [Header("Player Detection Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool rotateTowardsPlayer = true;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float searchInterval = 0.5f; // Search for player every 0.5 seconds instead of every frame
    
    private GameObject playerObject;
    private Camera playerCamera;
    private float lastSearchTime = 0f;
    
    // Static cache to share player reference across all instances
    private static GameObject cachedPlayerObject;
    private static Camera cachedPlayerCamera;
    private static float lastGlobalSearchTime = 0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Try to use cached player first for instant results
        if (cachedPlayerObject != null && Time.time - lastGlobalSearchTime < 1f)
        {
            playerObject = cachedPlayerObject;
            playerCamera = cachedPlayerCamera;
            Debug.Log($"Using cached player reference: {playerObject.name}");
        }
        else
        {
            // Find player immediately but asynchronously
            FindPlayerFast();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Only search for player periodically if not found, not every frame
        if (playerCamera == null && Time.time - lastSearchTime > searchInterval)
        {
            FindPlayerFast();
            lastSearchTime = Time.time;
        }
        
        if (rotateTowardsPlayer && playerCamera != null)
        {
            RotateTowardsPlayerCamera();
        }
    }
    
    /// <summary>
    /// Fast player search with caching and optimization
    /// </summary>
    private void FindPlayerFast()
    {
        // Use cached player if available and still valid
        if (cachedPlayerObject != null && Time.time - lastGlobalSearchTime < 2f)
        {
            playerObject = cachedPlayerObject;
            playerCamera = cachedPlayerCamera;
            return;
        }
        
        // Find player by tag (this is the fastest Unity search method)
        playerObject = GameObject.FindWithTag(playerTag);
        
        if (playerObject != null)
        {
            // Try to get the camera component from the player object
            playerCamera = playerObject.GetComponent<Camera>();
            
            // If no camera on the player object, try to find camera in children
            if (playerCamera == null)
            {
                playerCamera = playerObject.GetComponentInChildren<Camera>();
            }
            
            // If still no camera found, use the main camera as fallback
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera != null)
                {
                    Debug.LogWarning($"No camera found on player object '{playerObject.name}'. Using main camera as fallback.");
                }
            }
            
            // Cache the results globally for other instances
            cachedPlayerObject = playerObject;
            cachedPlayerCamera = playerCamera;
            lastGlobalSearchTime = Time.time;
            
            Debug.Log($"Player found: {playerObject.name} with camera: {playerCamera?.name}");
        }
        else
        {
            Debug.LogError($"No player object found with tag '{playerTag}' in the scene!");
        }
    }
    
    /// <summary>
    /// Rotates the UI object to face the player camera on Y-axis only
    /// </summary>
    private void RotateTowardsPlayerCamera()
    {
        if (playerCamera == null) return;
        
        // Get the direction from the player camera to this object (reversed direction)
        Vector3 targetPosition = playerCamera.transform.position;
        Vector3 direction = transform.position - targetPosition;
        
        // Only use X and Z components for Y-axis rotation only
        direction.y = 0f;
        
        // Check if direction is not zero to avoid errors
        if (direction.magnitude > 0.01f)
        {
            // Calculate the target rotation
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // Smoothly rotate towards the target
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Public method to manually refresh player search (useful if player is spawned dynamically)
    /// </summary>
    public void RefreshPlayerSearch()
    {
        FindPlayerFast();
    }
    
    /// <summary>
    /// Set player reference directly (fastest method if you have the reference)
    /// </summary>
    public void SetPlayerReference(GameObject player, Camera camera = null)
    {
        playerObject = player;
        playerCamera = camera ?? player.GetComponent<Camera>() ?? player.GetComponentInChildren<Camera>() ?? Camera.main;
        
        // Update global cache
        cachedPlayerObject = playerObject;
        cachedPlayerCamera = playerCamera;
        lastGlobalSearchTime = Time.time;
        
        Debug.Log($"Player reference set directly: {playerObject.name}");
    }
    
    /// <summary>
    /// Toggle rotation functionality on/off
    /// </summary>
    public void SetRotationEnabled(bool enabled)
    {
        rotateTowardsPlayer = enabled;
    }
}
