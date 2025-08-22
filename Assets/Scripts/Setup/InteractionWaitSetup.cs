using UnityEngine;

/// <summary>
/// Setup script to automatically create and configure the InteractionWaitManager
/// Add this script to any GameObject in your scene to ensure the InteractionWaitManager is available
/// </summary>
public class InteractionWaitSetup : MonoBehaviour
{
    [Header("Setup Settings")]
    [SerializeField] private bool createManagerOnAwake = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    void Awake()
    {
        if (createManagerOnAwake)
        {
            EnsureInteractionWaitManagerExists();
        }
    }
    
    /// <summary>
    /// Ensure that InteractionWaitManager exists in the scene
    /// </summary>
    public void EnsureInteractionWaitManagerExists()
    {
        if (InteractionWaitManager.Instance == null)
        {
            // Create a new GameObject for the InteractionWaitManager
            GameObject managerObject = new GameObject("InteractionWaitManager");
            InteractionWaitManager manager = managerObject.AddComponent<InteractionWaitManager>();
            
            // Enable debug logs if specified
            if (enableDebugLogs)
            {
                // Access the enableDebugLogs field using reflection since it's private
                var field = typeof(InteractionWaitManager).GetField("enableDebugLogs", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(manager, true);
                }
            }
            
            Debug.Log("InteractionWaitManager created automatically by InteractionWaitSetup");
        }
        else
        {
            Debug.Log("InteractionWaitManager already exists in the scene");
        }
    }
    
    /// <summary>
    /// Manual setup method that can be called from the inspector or other scripts
    /// </summary>
    [ContextMenu("Setup Interaction Wait Manager")]
    public void SetupManager()
    {
        EnsureInteractionWaitManagerExists();
    }
}
