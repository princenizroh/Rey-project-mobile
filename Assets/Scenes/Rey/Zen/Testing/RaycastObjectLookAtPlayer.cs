using UnityEngine;

public class RaycastObjectLookAtPlayer : MonoBehaviour
{
    [Header("Menu Settings")]
    public float lookAtSpeed = 5f;
    
    [Header("Outline Settings")]
    public Color outlineColor = Color.white;
    public float outlineWidth = 0.1f;
    
    private GameObject interactionMenu;
    private Camera playerCamera;
    private bool isBeingLookedAt = false;
    private bool wasBeingLookedAt = false; // Track previous state
    private Renderer objectRenderer;
    private Material[] originalMaterials;
    private Material outlineMaterial;
    private GameObject outlineObject; // Store reference to outline object
    
    void Start()
    {        
        // Find the main camera (assuming it's the player camera)
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }
        
        // Get the renderer and store original materials
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterials = objectRenderer.materials;
            CreateOutlineMaterial();
        }
        
        // Initially turn off the menu
        if (interactionMenu != null)
        {
            interactionMenu.SetActive(false);
        }
    }

    void Update()
    {
        // Check if we're being looked at by casting a ray from the camera
        CheckIfBeingLookedAt();
        
        // Check for interaction input
        CheckForInteraction();
        
        // Update menu state and rotation
        UpdateInteractionMenu();
        
        // Update outline effect
        UpdateOutline();
    }
    
    void CheckIfBeingLookedAt()
    {
        if (playerCamera == null) return;
        
        // Cast ray from camera forward
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        // Check if the ray hits this object
        if (Physics.Raycast(ray, out hit))
        {
            // Check if the hit object is this object and has the rayobject tag
            if (hit.collider.gameObject == this.gameObject && 
                hit.collider.CompareTag("rayobject"))
            {
                isBeingLookedAt = true;
                Debug.Log($"Player is looking at {gameObject.name}");
            }
            else
            {
                isBeingLookedAt = false;
            }
        }
        else
        {
            isBeingLookedAt = false;
        }
    }
    
    void CheckForInteraction()
    {
        // Check if player is looking at this object and pressed E
        if (isBeingLookedAt && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"Player interacted with {gameObject.name}! E key was pressed while looking at the object.");
        }
    }
    
    void CreateOutlineMaterial()
    {
        // Create a wireframe outline material
        outlineMaterial = new Material(Shader.Find("Unlit/Color"));
        outlineMaterial.color = outlineColor;
        
        // Set material properties for wireframe rendering
        outlineMaterial.SetFloat("_Mode", 0); // Opaque mode
        outlineMaterial.SetInt("_Cull", 0); // No culling to see wireframe from all angles
    }
    
    void UpdateOutline()
    {
        if (objectRenderer == null) return;
        
        // Only update when state changes
        if (isBeingLookedAt != wasBeingLookedAt)
        {
            if (isBeingLookedAt)
            {
                EnableOutline();
            }
            else
            {
                DisableOutline();
            }
            wasBeingLookedAt = isBeingLookedAt;
        }
    }
    
    void EnableOutline()
    {
        // Don't create if already exists
        if (outlineObject != null) return;
        
        if (objectRenderer != null && outlineMaterial != null)
        {
            // Create the wireframe outline object
            outlineObject = new GameObject("WireframeOutline");
            outlineObject.transform.parent = transform;
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one * (1f + outlineWidth);
            
            // Copy the mesh
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
                outlineMeshFilter.mesh = CreateWireframeMesh(meshFilter.mesh);
                
                MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
                outlineRenderer.material = outlineMaterial;
                outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
            }
        }
    }
    
    Mesh CreateWireframeMesh(Mesh originalMesh)
    {
        // Create a wireframe mesh from the original mesh
        Mesh wireframeMesh = new Mesh();
        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        
        // Create line indices for wireframe
        System.Collections.Generic.List<int> lineIndices = new System.Collections.Generic.List<int>();
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Add each edge of the triangle
            lineIndices.Add(triangles[i]);
            lineIndices.Add(triangles[i + 1]);
            
            lineIndices.Add(triangles[i + 1]);
            lineIndices.Add(triangles[i + 2]);
            
            lineIndices.Add(triangles[i + 2]);
            lineIndices.Add(triangles[i]);
        }
        
        wireframeMesh.vertices = vertices;
        wireframeMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);
        wireframeMesh.RecalculateBounds();
        
        return wireframeMesh;
    }
    
    void DisableOutline()
    {
        // Simply destroy the outline object if it exists
        if (outlineObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(outlineObject);
            }
            else
            {
                DestroyImmediate(outlineObject);
            }
            outlineObject = null;
        }
    }
    
    void UpdateInteractionMenu()
    {
        if (interactionMenu == null || playerCamera == null) return;
        
        if (isBeingLookedAt)
        {
            // Turn on the menu
            if (!interactionMenu.activeInHierarchy)
            {
                interactionMenu.SetActive(true);
            }
            
            // Make the menu face the player
            Vector3 directionToPlayer = playerCamera.transform.position - interactionMenu.transform.position;
            directionToPlayer.y = 0f; // Keep it horizontal (optional)
            
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                // Add 180 degrees to face forward instead of backwards
                targetRotation *= Quaternion.Euler(0, 180, 0);
                interactionMenu.transform.rotation = Quaternion.Slerp(
                    interactionMenu.transform.rotation, 
                    targetRotation, 
                    lookAtSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            // Turn off the menu
            if (interactionMenu.activeInHierarchy)
            {
                interactionMenu.SetActive(false);
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up outline object when this script is destroyed
        DisableOutline();
    }
    
    // Optional: Visual debugging in scene view
    void OnDrawGizmos()
    {
        if (isBeingLookedAt)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
