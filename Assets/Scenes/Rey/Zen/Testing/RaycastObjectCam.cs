using UnityEngine;

public class RaycastObject : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float rayDistance = 10f;
    public LayerMask layerMask = -1; // All layers by default
    
    [Header("Visual Settings")]
    public LineRenderer lineRenderer;
    public Color hitColor = Color.green;
    public Color missColor = Color.red;
    public float lineWidth = 0.1f;
    
    private Camera playerCamera;
    private bool isHitting = false;
    
    void Start()
    {
        // Get the camera component
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Setup LineRenderer if not assigned
        if (lineRenderer == null)
        {
            SetupLineRenderer();
        }
        
        // Configure LineRenderer
        ConfigureLineRenderer();
    }
    
    void Update()
    {
        PerformRaycast();
        UpdateVisual();
    }
    
    void PerformRaycast()
    {
        // Get ray from camera center
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        // Perform raycast
        if (Physics.Raycast(ray, out hit, rayDistance, layerMask))
        {
            // Check if hit object has the correct tag
            if (hit.collider.CompareTag("rayobject"))
            {
                isHitting = true;
                Debug.Log($"Raycast hit object: {hit.collider.name} at distance: {hit.distance:F2}");
            }
            else
            {
                isHitting = false;
            }
        }
        else
        {
            isHitting = false;
        }
    }
    
    void UpdateVisual()
    {
        if (lineRenderer != null)
        {
            // Set line color based on hit status
            Color currentColor = isHitting ? hitColor : missColor;
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
            lineRenderer.material.color = currentColor;
            
            // Set line positions
            Vector3 startPoint = transform.position;
            Vector3 endPoint = transform.position + (transform.forward * rayDistance);
            
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
        }
    }
    
    void SetupLineRenderer()
    {
        // Create LineRenderer component if it doesn't exist
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
    }
    
    void ConfigureLineRenderer()
    {
        if (lineRenderer != null)
        {
            // Configure LineRenderer properties
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;
            
            // Create a simple material if none exists
            if (lineRenderer.material == null)
            {
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            // Set initial color
            lineRenderer.startColor = missColor;
            lineRenderer.endColor = missColor;
        }
    }
    
    // Draw gizmos in scene view for debugging
    void OnDrawGizmos()
    {
        Gizmos.color = isHitting ? hitColor : missColor;
        Gizmos.DrawRay(transform.position, transform.forward * rayDistance);
    }
}
