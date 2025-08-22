using System.Collections;
using TMPro;
using UnityEngine;

public class RaycastObjectCam : MonoBehaviour
{
    [Header("Raycast Settings")]
    public bool raycastStatus = false;
    public GameObject currentHitObject;
    public float rayDistance = 10f;
    public LayerMask layerMask = -1; // All layers by default
    public CoreGameManager coreGameManager;

    public TextMeshProUGUI narratorText;
    
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string interactionMessage = "Interaction key pressed!";
    
    private Camera playerCamera;
    private bool isHitting = false;
    private RaycastObjectBehaviour currentHitBehaviour = null;
    
    void Start()
    {
        // Get the camera component
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }
    
    void Update()
    {
        PerformRaycast();
        HandleInteraction();
    }
    
    void PerformRaycast()
    {
        // Get ray from camera center
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        // Perform raycast
        if (Physics.Raycast(ray, out hit, rayDistance, layerMask))
        {
            // Check if hit object has the RaycastObjectBehaviour script
            RaycastObjectBehaviour objectBehaviour = hit.collider.GetComponent<RaycastObjectBehaviour>();
            
            if (objectBehaviour != null)
            {
                isHitting = true;
                
                // Store reference to current hit behaviour for interaction
                currentHitBehaviour = objectBehaviour;
                currentHitObject = hit.collider.gameObject;
                
                // Call the behaviour script to handle the hit detection
                objectBehaviour.OnRaycastHit(hit);
                Debug.Log($"Using existing RaycastObjectBehaviour script on: {hit.collider.name}");
            }
            else
            {
                isHitting = false;
                currentHitBehaviour = null; // Clear reference when not hitting object with script
                currentHitObject = null; // Clear reference when not hitting object with script
            }
        }
        else
        {
            isHitting = false;
            currentHitBehaviour = null; // Clear reference when not hitting anything
            currentHitObject = null; // Clear reference when not hitting anything
        }
    }
    
    // Draw gizmos in scene view for debugging
    void OnDrawGizmos()
    {
        Gizmos.color = isHitting ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * rayDistance);
    }
    
    /// <summary>
    /// Check for interaction input when an object is detected
    /// </summary>
    void HandleInteraction()
    {
        // Update raycastStatus based on current hit state
        raycastStatus = isHitting;
        
        // Check for interaction input when hitting a raycast object
        if (isHitting && currentHitBehaviour != null && Input.GetKeyDown(interactionKey))
        {
            // Trigger interaction on the hit object
            currentHitBehaviour.OnInteraction();
            Debug.Log($"Interaction triggered on: {currentHitBehaviour.gameObject.name}");
        }
    }
}
