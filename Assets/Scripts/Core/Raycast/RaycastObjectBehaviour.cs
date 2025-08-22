using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InteractionContextData
{
    [Header("Context")]
    public string dayContext = ""; // e.g., "Day2", "Day3"
    public string sequenceContext = ""; // e.g., "Night", "Midnight", "Morning"
    
    [Header("Dialog Settings")]
    public string dialogPath = ""; // e.g., "GameData/Dialog/Day2/Seq12AAyah"
    
    [Header("UI Spawn Settings")]
    public Vector3 spawnOffset = new Vector3(1f, 1f, 1f);
    public bool useWorldSpaceOffset = false;
    
    public InteractionContextData()
    {
        // Default constructor
    }
    
    public InteractionContextData(string day, string sequence, string path, Vector3 offset, bool worldSpace = false)
    {
        dayContext = day;
        sequenceContext = sequence;
        dialogPath = path;
        spawnOffset = offset;
        useWorldSpaceOffset = worldSpace;
    }
}

// Backward compatibility classes - marked as obsolete
[System.Serializable]
[System.Obsolete("Use InteractionContextData instead")]
public class DialogPathData
{
    public string dayContext = "";
    public string sequenceContext = "";
    public string dialogPath = "";
    
    public DialogPathData(string day, string sequence, string path)
    {
        dayContext = day;
        sequenceContext = sequence;
        dialogPath = path;
    }
}

[System.Serializable]
[System.Obsolete("Use InteractionContextData instead")]
public class SpawnOffsetData
{
    public string dayContext = "";
    public string sequenceContext = "";
    public Vector3 spawnOffset = new Vector3(1f, 1f, 1f);
    public bool useWorldSpaceOffset = false;
    
    public SpawnOffsetData(string day, string sequence, Vector3 offset, bool worldSpace = false)
    {
        dayContext = day;
        sequenceContext = sequence;
        spawnOffset = offset;
        useWorldSpaceOffset = worldSpace;
    }
}

public class RaycastObjectBehaviour : MonoBehaviour
{
    [Header("Character Identity")]
    [SerializeField] private string characterIdentity = ""; // e.g., "Mulyono", "Linda"
    
    [Header("Interaction Context Settings")]
    [SerializeField] private List<InteractionContextData> interactionContexts = new List<InteractionContextData>();
    
    [Header("Fallback Settings (Backward Compatibility)")]
    [SerializeField] private string fallbackDialogPath = ""; // Fallback for backward compatibility
    [SerializeField] private Vector3 fallbackSpawnOffset = new Vector3(1f, 1f, 1f); // Fallback for backward compatibility
    [SerializeField] private bool fallbackUseWorldSpaceOffset = false;
    
    [Header("Raycast Detection Settings")]
    [SerializeField] private string logMessage = "Raycast hit detected!";
    [SerializeField] private bool showRaycastInfo = true;
    
    [Header("UI Spawn Settings")]
    [SerializeField] private GameObject raycastUIPrefab;
    [SerializeField] private bool destroyPreviousUI = true;
    [SerializeField] private string targetCanvasName = "Canvas3D";
    [SerializeField] private float contactLostDelay = 0.1f; // Delay before destroying UI when contact is lost
    
    // Current context for automatic context detection
    private string currentDayContext = "";
    private string currentSequenceContext = "";
    
    // Runtime state
    private GameObject spawnedUI;
    private Canvas targetCanvas;
    private bool isCurrentlyBeingRaycast = false;
    private bool wasBeingRaycast = false;
    private float lastRaycastTime = 0f;
    private static bool hasShownMissingPrefabWarning = false; // Static to prevent spam across all instances
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Check if GameObject has a collider for raycast detection
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider == null)
        {
            Debug.LogError($"GameObject '{gameObject.name}' doesn't have a Collider component. Please add a Collider manually for raycast detection to work.");
            enabled = false; // Disable this component if no collider
            return;
        }
        else
        {
            Debug.Log($"GameObject '{gameObject.name}' has {existingCollider.GetType().Name}. Raycast detection ready.");
        }
        
        // Ensure the GameObject has the correct tag
        if (!gameObject.CompareTag("RaycastObject"))
        {
            Debug.LogWarning($"GameObject '{gameObject.name}' doesn't have 'raycast object' tag. Raycast detection may not work properly.");
        }
        
        // Try to find RaycastUI prefab if not assigned
        if (raycastUIPrefab == null)
        {
            FindRaycastUIPrefabInAssets();
        }
        
        // Find the target canvas
        FindTargetCanvas();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if contact with raycast has been lost
        CheckRaycastContact();
    }
    
    /// <summary>
    /// Called when a raycast hits this object's collider
    /// This method can be called by other scripts when they detect a raycast hit
    /// </summary>
    public void OnRaycastHit(RaycastHit hitInfo)
    {
        // Update raycast contact state
        isCurrentlyBeingRaycast = true;
        lastRaycastTime = Time.time;
        
        if (showRaycastInfo)
        {
            Debug.Log($"{logMessage} - GameObject: {gameObject.name}, Hit Point: {hitInfo.point}, Distance: {hitInfo.distance:F2}");
        }
        else
        {
            Debug.Log($"{logMessage} - GameObject: {gameObject.name}");
        }
        
        // Check if we need to spawn UI (only spawn when first detected)
        if (!wasBeingRaycast && spawnedUI == null)
        {
            SpawnRaycastUI();
        }
        
        wasBeingRaycast = true;
    }
    
    /// <summary>
    /// Simple version - just logs that this object was hit
    /// </summary>
    public void OnRaycastHit()
    {
        // Update raycast contact state
        isCurrentlyBeingRaycast = true;
        lastRaycastTime = Time.time;
        
        Debug.Log($"{logMessage} - GameObject: {gameObject.name}");
        
        // Check if we need to spawn UI (only spawn when first detected)
        if (!wasBeingRaycast && spawnedUI == null)
        {
            SpawnRaycastUI();
        }
        
        wasBeingRaycast = true;
    }
    
    /// <summary>
    /// Called when a raycast hits this object with custom message
    /// </summary>
    public void OnRaycastHit(string customMessage)
    {
        // Update raycast contact state
        isCurrentlyBeingRaycast = true;
        lastRaycastTime = Time.time;
        
        Debug.Log($"{customMessage} - GameObject: {gameObject.name}");
        
        // Check if we need to spawn UI (only spawn when first detected)
        if (!wasBeingRaycast && spawnedUI == null)
        {
            SpawnRaycastUI();
        }
        
        wasBeingRaycast = true;
    }
    
    /// <summary>
    /// Get the appropriate interaction context based on current context
    /// </summary>
    public InteractionContextData GetInteractionContext(string dayContext = "", string sequenceContext = "")
    {
        // If specific context provided, try to find matching context
        if (!string.IsNullOrEmpty(dayContext) || !string.IsNullOrEmpty(sequenceContext))
        {
            foreach (var contextData in interactionContexts)
            {
                bool dayMatch = string.IsNullOrEmpty(dayContext) || contextData.dayContext == dayContext;
                bool sequenceMatch = string.IsNullOrEmpty(sequenceContext) || contextData.sequenceContext == sequenceContext;
                
                if (dayMatch && sequenceMatch)
                {
                    Debug.Log($"[RaycastObjectBehaviour] Found interaction context for {characterIdentity}: {contextData.dialogPath} | {contextData.spawnOffset} (Day: {contextData.dayContext}, Sequence: {contextData.sequenceContext})");
                    return contextData;
                }
            }
        }
        
        // If no specific context found, return first available or create fallback
        if (interactionContexts.Count > 0)
        {
            Debug.Log($"[RaycastObjectBehaviour] Using default interaction context for {characterIdentity}: {interactionContexts[0].dialogPath} | {interactionContexts[0].spawnOffset}");
            return interactionContexts[0];
        }
        
        // Create fallback InteractionContextData for backward compatibility
        Debug.Log($"[RaycastObjectBehaviour] Using fallback interaction context for {characterIdentity}: {fallbackDialogPath} | {fallbackSpawnOffset}");
        return new InteractionContextData("", "", fallbackDialogPath, fallbackSpawnOffset, fallbackUseWorldSpaceOffset);
    }
    
    /// <summary>
    /// Spawns the RaycastUI prefab with context-aware positioning
    /// </summary>
    private void SpawnRaycastUI(string dayContext = "", string sequenceContext = "")
    {
        // Try to find prefab one more time if it's still null
        if (raycastUIPrefab == null)
        {
            FindRaycastUIPrefabInAssets();
        }
        
        // If still null after trying to find it, silently return (don't spam console)
        if (raycastUIPrefab == null)
        {
            return;
        }
        
        if (targetCanvas == null)
        {
            Debug.LogError($"Target canvas '{targetCanvasName}' not found! Please make sure the canvas exists in the scene.");
            return;
        }
        
        // Don't spawn if already exists
        if (spawnedUI != null)
        {
            return;
        }
        
        // Get context-appropriate interaction data
        InteractionContextData contextData = GetInteractionContext(dayContext, sequenceContext);
        
        // Calculate spawn position with context-aware offset
        Vector3 spawnPosition;
        if (contextData.useWorldSpaceOffset)
        {
            // World space offset - absolute position adjustment
            spawnPosition = transform.position + contextData.spawnOffset;
        }
        else
        {
            // Local space offset - relative to object's rotation (default)
            spawnPosition = transform.position + transform.TransformDirection(contextData.spawnOffset);
        }
        
        // Spawn the UI prefab as a child of the target canvas
        spawnedUI = Instantiate(raycastUIPrefab, targetCanvas.transform);
        
        // Set the world position while keeping it as a child of the canvas
        spawnedUI.transform.position = spawnPosition;
        spawnedUI.transform.rotation = transform.rotation;
        
        Debug.Log($"RaycastUI spawned at position: {spawnPosition} for object: {gameObject.name} under canvas: {targetCanvasName} (Context: {dayContext}/{sequenceContext})");
    }
    
    /// <summary>
    /// Spawns the RaycastUI prefab with current context or backward compatibility
    /// </summary>
    private void SpawnRaycastUI()
    {
        // Use current context if available, otherwise fallback to empty context
        SpawnRaycastUI(currentDayContext, currentSequenceContext);
    }
    
    /// <summary>
    /// Check if raycast contact has been lost and handle UI destruction
    /// </summary>
    private void CheckRaycastContact()
    {
        // Reset the current raycast state at the beginning of each frame
        isCurrentlyBeingRaycast = false;
        
        // Check if we've lost contact (no raycast hit for the specified delay)
        if (wasBeingRaycast && Time.time - lastRaycastTime > contactLostDelay)
        {
            // Contact lost - destroy the UI
            if (spawnedUI != null)
            {
                DestroySpawnedUI();
                Debug.Log($"Raycast contact lost. UI destroyed for object: {gameObject.name}");
            }
            
            wasBeingRaycast = false;
        }
    }
    
    /// <summary>
    /// Manually destroy the spawned UI
    /// </summary>
    public void DestroySpawnedUI()
    {
        if (spawnedUI != null)
        {
            Destroy(spawnedUI);
            spawnedUI = null;
            Debug.Log($"Spawned RaycastUI destroyed for object: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Force destroy spawned UI and reset raycast state
    /// </summary>
    public void ForceResetRaycastState()
    {
        DestroySpawnedUI();
        isCurrentlyBeingRaycast = false;
        wasBeingRaycast = false;
        lastRaycastTime = 0f;
    }
    
    /// <summary>
    /// Check if UI is currently spawned
    /// </summary>
    public bool HasSpawnedUI()
    {
        return spawnedUI != null;
    }
    
    /// <summary>
    /// Search for RaycastUI prefab in the project assets
    /// </summary>
    private void FindRaycastUIPrefabInAssets()
    {
#if UNITY_EDITOR
        // In Editor: Search for RaycastUI prefab in the project assets
        string[] guids = UnityEditor.AssetDatabase.FindAssets("RaycastUI t:GameObject");
        
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            
            if (prefab != null && prefab.name.Contains("RaycastUI"))
            {
                raycastUIPrefab = prefab;
                Debug.Log($"RaycastUI prefab found and assigned automatically: {assetPath}");
                return;
            }
        }
        
        Debug.LogWarning("RaycastUI prefab not found in project assets. Please assign it manually in the inspector.");
#else
        // In Build: Try to load from Resources folder
        GameObject resourcePrefab = Resources.Load<GameObject>("RaycastUI");
        if (resourcePrefab != null)
        {
            raycastUIPrefab = resourcePrefab;
            Debug.Log("RaycastUI prefab loaded from Resources folder.");
            return;
        }
        
        // If not found in Resources, try alternative names
        string[] possibleNames = { "RaycastUI", "Raycast UI", "RaycastInteractionUI", "InteractionUI" };
        foreach (string name in possibleNames)
        {
            resourcePrefab = Resources.Load<GameObject>(name);
            if (resourcePrefab != null)
            {
                raycastUIPrefab = resourcePrefab;
                Debug.Log($"RaycastUI prefab loaded from Resources folder with name: {name}");
                return;
            }
        }
        
        // Only show warning if we really can't find anything, but don't spam the console
        if (!hasShownMissingPrefabWarning)
        {
            Debug.LogWarning($"RaycastUI prefab not found in Resources folder. Please either:\n" +
                           "1. Assign the prefab manually in the inspector, or\n" +
                           "2. Place the RaycastUI prefab in a 'Resources' folder", this);
            hasShownMissingPrefabWarning = true;
        }
#endif
    }
    
    /// <summary>
    /// Find the target canvas by name in the scene
    /// </summary>
    private void FindTargetCanvas()
    {
        // Search for canvas by name
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.name == targetCanvasName)
            {
                targetCanvas = canvas;
                Debug.Log($"Target canvas '{targetCanvasName}' found and assigned.");
                return;
            }
        }
        
        Debug.LogError($"Target canvas '{targetCanvasName}' not found in the scene! Please make sure a canvas with this name exists.");
    }
    
    /// <summary>
    /// Called when the player interacts with this object (presses E while looking at it)
    /// </summary>
    public virtual void OnInteraction()
    {
        Debug.Log($"Player interacted with {gameObject.name} (Identity: {characterIdentity})");
        // Override this method in derived classes for custom interaction behavior
    }
    
    /// <summary>
    /// Get the character identity for this raycast object
    /// </summary>
    public string GetCharacterIdentity()
    {
        return characterIdentity;
    }
    
    /// <summary>
    /// Get the dialog path for this character's interaction based on current context
    /// </summary>
    public string GetInteractionDialogPath(string dayContext = "", string sequenceContext = "")
    {
        // If specific context provided, try to find matching path
        if (!string.IsNullOrEmpty(dayContext) || !string.IsNullOrEmpty(sequenceContext))
        {
            foreach (var contextData in interactionContexts)
            {
                bool dayMatch = string.IsNullOrEmpty(dayContext) || contextData.dayContext == dayContext;
                bool sequenceMatch = string.IsNullOrEmpty(sequenceContext) || contextData.sequenceContext == sequenceContext;
                
                if (dayMatch && sequenceMatch)
                {
                    Debug.Log($"[RaycastObjectBehaviour] Found dialog path for {characterIdentity}: {contextData.dialogPath} (Day: {contextData.dayContext}, Sequence: {contextData.sequenceContext})");
                    return contextData.dialogPath;
                }
            }
        }
        
        // If no specific path found, return first available or fallback
        if (interactionContexts.Count > 0)
        {
            Debug.Log($"[RaycastObjectBehaviour] Using default dialog path for {characterIdentity}: {interactionContexts[0].dialogPath}");
            return interactionContexts[0].dialogPath;
        }
        
        // Use fallback for backward compatibility
        Debug.Log($"[RaycastObjectBehaviour] Using fallback dialog path for {characterIdentity}: {fallbackDialogPath}");
        return fallbackDialogPath;
    }
    
    /// <summary>
    /// Get the dialog path for this character's interaction (backward compatibility)
    /// </summary>
    public string GetInteractionDialogPath()
    {
        return GetInteractionDialogPath("", "");
    }
    
    /// <summary>
    /// Add a new interaction context for specific day/sequence
    /// </summary>
    public void AddInteractionContext(string dayContext, string sequenceContext, string dialogPath, Vector3 spawnOffset, bool useWorldSpace = false)
    {
        var newContext = new InteractionContextData(dayContext, sequenceContext, dialogPath, spawnOffset, useWorldSpace);
        interactionContexts.Add(newContext);
        Debug.Log($"[RaycastObjectBehaviour] Added interaction context for {characterIdentity}: {dialogPath} | {spawnOffset} (Day: {dayContext}, Sequence: {sequenceContext}, WorldSpace: {useWorldSpace})");
    }
    
    /// <summary>
    /// Add a new dialog path for specific day/sequence context (backward compatibility)
    /// </summary>
    public void AddDialogPath(string dayContext, string sequenceContext, string dialogPath)
    {
        // Find existing context or create new one
        var existingContext = interactionContexts.Find(c => c.dayContext == dayContext && c.sequenceContext == sequenceContext);
        if (existingContext != null)
        {
            existingContext.dialogPath = dialogPath;
            Debug.Log($"[RaycastObjectBehaviour] Updated dialog path for existing context {characterIdentity}: {dialogPath} (Day: {dayContext}, Sequence: {sequenceContext})");
        }
        else
        {
            AddInteractionContext(dayContext, sequenceContext, dialogPath, fallbackSpawnOffset, fallbackUseWorldSpaceOffset);
        }
    }
    
    /// <summary>
    /// Add a new spawn offset for specific day/sequence context (backward compatibility)
    /// </summary>
    public void AddSpawnOffset(string dayContext, string sequenceContext, Vector3 spawnOffset, bool useWorldSpace = false)
    {
        // Find existing context or create new one
        var existingContext = interactionContexts.Find(c => c.dayContext == dayContext && c.sequenceContext == sequenceContext);
        if (existingContext != null)
        {
            existingContext.spawnOffset = spawnOffset;
            existingContext.useWorldSpaceOffset = useWorldSpace;
            Debug.Log($"[RaycastObjectBehaviour] Updated spawn offset for existing context {characterIdentity}: {spawnOffset} (Day: {dayContext}, Sequence: {sequenceContext}, WorldSpace: {useWorldSpace})");
        }
        else
        {
            AddInteractionContext(dayContext, sequenceContext, fallbackDialogPath, spawnOffset, useWorldSpace);
        }
    }
    
    /// <summary>
    /// Set the current context for automatic UI positioning
    /// </summary>
    public void SetCurrentContext(string dayContext, string sequenceContext)
    {
        currentDayContext = dayContext;
        currentSequenceContext = sequenceContext;
        Debug.Log($"[RaycastObjectBehaviour] Context set for {characterIdentity}: Day={dayContext}, Sequence={sequenceContext}");
    }
    
    /// <summary>
    /// Get the current context
    /// </summary>
    public (string day, string sequence) GetCurrentContext()
    {
        return (currentDayContext, currentSequenceContext);
    }
    
    /// <summary>
    /// Set the raycast UI prefab programmatically (useful for runtime setup)
    /// </summary>
    public void SetRaycastUIPrefab(GameObject prefab)
    {
        raycastUIPrefab = prefab;
        Debug.Log($"[RaycastObjectBehaviour] Raycast UI prefab set programmatically for {characterIdentity}: {prefab?.name}");
    }
    
    /// <summary>
    /// Static method to set raycast UI prefab for all RaycastObjectBehaviour instances
    /// </summary>
    public static void SetGlobalRaycastUIPrefab(GameObject prefab)
    {
        RaycastObjectBehaviour[] allInstances = FindObjectsByType<RaycastObjectBehaviour>(FindObjectsSortMode.None);
        foreach (var instance in allInstances)
        {
            if (instance.raycastUIPrefab == null)
            {
                instance.SetRaycastUIPrefab(prefab);
            }
        }
        Debug.Log($"[RaycastObjectBehaviour] Global raycast UI prefab set for {allInstances.Length} instances: {prefab?.name}");
    }
    
    /// <summary>
    /// Set character identity and dialog path programmatically (backward compatibility)
    /// </summary>
    public void SetCharacterData(string identity, string dialogPath)
    {
        characterIdentity = identity;
        fallbackDialogPath = dialogPath;
    }
    
    /// <summary>
    /// Set character identity and multiple interaction contexts
    /// </summary>
    public void SetCharacterData(string identity, List<InteractionContextData> contexts)
    {
        characterIdentity = identity;
        interactionContexts = new List<InteractionContextData>(contexts);
    }
    
    /// <summary>
    /// Set character identity and multiple dialog paths (backward compatibility - deprecated)
    /// </summary>
    [System.Obsolete("Use SetCharacterData with InteractionContextData instead")]
    public void SetCharacterData(string identity, List<DialogPathData> paths)
    {
        characterIdentity = identity;
        // Convert old DialogPathData to new InteractionContextData
        interactionContexts.Clear();
        foreach (var path in paths)
        {
            var contextData = new InteractionContextData(path.dayContext, path.sequenceContext, path.dialogPath, fallbackSpawnOffset, fallbackUseWorldSpaceOffset);
            interactionContexts.Add(contextData);
        }
    }
    
    /// <summary>
    /// Debug method to validate setup - can be called from context menu
    /// </summary>
    [ContextMenu("Debug Raycast Setup")]
    private void DebugRaycastSetup()
    {
        Debug.Log("=== RAYCAST SETUP DEBUG ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Tag: {gameObject.tag}");
        Debug.Log($"Layer: {LayerMask.LayerToName(gameObject.layer)}");
        
        // Check colliders
        Collider[] colliders = GetComponents<Collider>();
        Debug.Log($"Colliders found: {colliders.Length}");
        for (int i = 0; i < colliders.Length; i++)
        {
            Debug.Log($"  - {colliders[i].GetType().Name} (IsTrigger: {colliders[i].isTrigger}, Enabled: {colliders[i].enabled})");
        }
        
        // Check character identity
        Debug.Log($"Character Identity: {characterIdentity}");
        Debug.Log($"Interaction Contexts: {interactionContexts.Count}");
        foreach (var context in interactionContexts)
        {
            Debug.Log($"  - {context.dayContext}/{context.sequenceContext}: {context.dialogPath}");
        }
        
        // Check UI setup
        Debug.Log($"UI Prefab: {(raycastUIPrefab != null ? raycastUIPrefab.name : "NULL")}");
        Debug.Log($"Target Canvas: {(targetCanvas != null ? targetCanvas.name : "NULL")}");
        Debug.Log("========================");
    }
    
    [ContextMenu("Clean Up Unwanted Colliders")]
    public void CleanUpUnwantedColliders()
    {
        Collider[] allColliders = GetComponents<Collider>();
        if (allColliders.Length <= 1)
        {
            Debug.Log("Only one or no colliders found. No cleanup needed.");
            return;
        }
        
        Debug.Log($"Found {allColliders.Length} colliders. Cleaning up...");
        
        // Keep only the first BoxCollider if multiple colliders exist
        BoxCollider keepBoxCollider = null;
        List<Collider> toRemove = new List<Collider>();
        
        foreach (Collider col in allColliders)
        {
            if (col is BoxCollider && keepBoxCollider == null)
            {
                keepBoxCollider = col as BoxCollider;
                Debug.Log($"Keeping BoxCollider: {keepBoxCollider}");
            }
            else
            {
                toRemove.Add(col);
                Debug.Log($"Marking for removal: {col.GetType().Name}");
            }
        }
        
        // Remove unwanted colliders
        foreach (Collider col in toRemove)
        {
            Debug.Log($"Removing {col.GetType().Name}");
            DestroyImmediate(col);
        }
        
        Debug.Log("Collider cleanup completed!");
    }
}

