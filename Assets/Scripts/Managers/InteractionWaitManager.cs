using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Manages waiting for player interactions during narrative sequences
/// </summary>
public class InteractionWaitManager : MonoBehaviour
{
    public static InteractionWaitManager Instance { get; private set; }
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Dictionary to store active wait conditions
    private Dictionary<string, InteractionWaitData> activeWaits = new Dictionary<string, InteractionWaitData>();
    
    // Dictionary to store dialog paths for objects
    private Dictionary<string, string> objectDialogPaths = new Dictionary<string, string>();
    
    // Class to store wait condition data
    [System.Serializable]
    private class InteractionWaitData
    {
        public string waitId;
        public string targetObjectName;
        public string requiredInteractionType;
        public bool isCompleted;
        public Action onCompleted;
        public float timeoutDuration;
        public float startTime;
        
        public InteractionWaitData(string id, string objectName, string interactionType, Action callback, float timeout = 0f)
        {
            waitId = id;
            targetObjectName = objectName;
            requiredInteractionType = interactionType;
            isCompleted = false;
            onCompleted = callback;
            timeoutDuration = timeout;
            startTime = Time.time;
        }
        
        public bool HasTimedOut()
        {
            return timeoutDuration > 0f && (Time.time - startTime) >= timeoutDuration;
        }
    }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        CheckForTimeouts();
    }
    
    /// <summary>
    /// Register a wait condition for a specific object interaction
    /// </summary>
    /// <param name="waitId">Unique identifier for this wait condition</param>
    /// <param name="targetObjectName">Name of the GameObject that needs to be interacted with</param>
    /// <param name="interactionType">Type of interaction required (optional)</param>
    /// <param name="onCompleted">Callback to execute when interaction is completed</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds (0 = no timeout)</param>
    public void RegisterWaitCondition(string waitId, string targetObjectName, string interactionType = "default", Action onCompleted = null, float timeoutSeconds = 0f)
    {
        if (string.IsNullOrEmpty(waitId))
        {
            Debug.LogError("InteractionWaitManager: Wait ID cannot be null or empty!");
            return;
        }
        
        if (activeWaits.ContainsKey(waitId))
        {
            Debug.LogWarning($"InteractionWaitManager: Wait condition '{waitId}' already exists. Overwriting...");
            activeWaits.Remove(waitId);
        }
        
        InteractionWaitData waitData = new InteractionWaitData(waitId, targetObjectName, interactionType, onCompleted, timeoutSeconds);
        activeWaits.Add(waitId, waitData);
        
        if (enableDebugLogs)
        {
            Debug.Log($"InteractionWaitManager: Registered wait condition '{waitId}' for object '{targetObjectName}'");
        }
    }
    
    /// <summary>
    /// Complete a wait condition (called when player interacts with an object)
    /// </summary>
    /// <param name="objectName">Name of the interacted object</param>
    /// <param name="interactionType">Type of interaction performed</param>
    public void CompleteInteraction(string objectName, string interactionType = "default")
    {
        if (string.IsNullOrEmpty(objectName))
        {
            Debug.LogError("InteractionWaitManager: Object name cannot be null or empty!");
            return;
        }
        
        // Find matching wait conditions
        List<string> completedWaits = new List<string>();
        
        foreach (var kvp in activeWaits)
        {
            InteractionWaitData waitData = kvp.Value;
            
            // Check if this interaction matches the wait condition
            if ((string.IsNullOrEmpty(waitData.targetObjectName) || waitData.targetObjectName == objectName) &&
                (string.IsNullOrEmpty(waitData.requiredInteractionType) || waitData.requiredInteractionType == interactionType || waitData.requiredInteractionType == "default"))
            {
                waitData.isCompleted = true;
                completedWaits.Add(kvp.Key);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"InteractionWaitManager: Completed wait condition '{waitData.waitId}' for object '{objectName}'");
                }
                
                // Execute callback if provided
                try
                {
                    waitData.onCompleted?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"InteractionWaitManager: Error executing callback for '{waitData.waitId}': {e.Message}");
                }
            }
        }
        
        // Remove completed waits
        foreach (string waitId in completedWaits)
        {
            activeWaits.Remove(waitId);
        }
        
        if (completedWaits.Count == 0 && enableDebugLogs)
        {
            Debug.Log($"InteractionWaitManager: No matching wait conditions found for object '{objectName}' with interaction '{interactionType}'");
        }
    }
    
    /// <summary>
    /// Check if a specific wait condition is completed
    /// </summary>
    /// <param name="waitId">The wait condition ID to check</param>
    /// <returns>True if the condition is completed or doesn't exist</returns>
    public bool IsWaitCompleted(string waitId)
    {
        if (!activeWaits.ContainsKey(waitId))
        {
            // If the wait doesn't exist, consider it completed
            return true;
        }
        
        return activeWaits[waitId].isCompleted;
    }
    
    /// <summary>
    /// Cancel a wait condition
    /// </summary>
    /// <param name="waitId">The wait condition ID to cancel</param>
    public void CancelWaitCondition(string waitId)
    {
        if (activeWaits.ContainsKey(waitId))
        {
            activeWaits.Remove(waitId);
            
            if (enableDebugLogs)
            {
                Debug.Log($"InteractionWaitManager: Cancelled wait condition '{waitId}'");
            }
        }
    }
    
    /// <summary>
    /// Clear all active wait conditions
    /// </summary>
    public void ClearAllWaitConditions()
    {
        int count = activeWaits.Count;
        activeWaits.Clear();
        
        if (enableDebugLogs)
        {
            Debug.Log($"InteractionWaitManager: Cleared {count} wait conditions");
        }
    }
    
    /// <summary>
    /// Get the number of active wait conditions
    /// </summary>
    public int GetActiveWaitCount()
    {
        return activeWaits.Count;
    }
    
    /// <summary>
    /// Check for timed out wait conditions
    /// </summary>
    private void CheckForTimeouts()
    {
        List<string> timedOutWaits = new List<string>();
        
        foreach (var kvp in activeWaits)
        {
            InteractionWaitData waitData = kvp.Value;
            
            if (waitData.HasTimedOut())
            {
                timedOutWaits.Add(kvp.Key);
                
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"InteractionWaitManager: Wait condition '{waitData.waitId}' timed out after {waitData.timeoutDuration} seconds");
                }
                
                // Execute callback even on timeout
                try
                {
                    waitData.onCompleted?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"InteractionWaitManager: Error executing timeout callback for '{waitData.waitId}': {e.Message}");
                }
            }
        }
        
        // Remove timed out waits
        foreach (string waitId in timedOutWaits)
        {
            activeWaits.Remove(waitId);
        }
    }
    
    /// <summary>
    /// Coroutine to wait for a specific interaction
    /// </summary>
    /// <param name="waitId">Unique identifier for this wait</param>
    /// <param name="targetObjectName">Name of the target object</param>
    /// <param name="interactionType">Type of interaction required</param>
    /// <param name="timeoutSeconds">Optional timeout</param>
    /// <returns>IEnumerator for coroutine usage</returns>
    public IEnumerator WaitForInteraction(string waitId, string targetObjectName, string interactionType = "default", float timeoutSeconds = 0f)
    {
        bool interactionCompleted = false;
        
        // Register the wait condition with a callback
        RegisterWaitCondition(waitId, targetObjectName, interactionType, () => { interactionCompleted = true; }, timeoutSeconds);
        
        // Wait until the interaction is completed or times out
        yield return new WaitUntil(() => interactionCompleted || !activeWaits.ContainsKey(waitId));
        
        if (enableDebugLogs)
        {
            Debug.Log($"InteractionWaitManager: Wait for interaction '{waitId}' completed");
        }
    }
    
    /// <summary>
    /// Register a dialog path for an object - when player interacts with this object, this dialog will play
    /// </summary>
    /// <param name="objectName">Name of the GameObject</param>
    /// <param name="dialogPath">Path to the dialog file</param>
    public void RegisterDialogForObject(string objectName, string dialogPath)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            Debug.LogError("InteractionWaitManager: Object name cannot be null or empty!");
            return;
        }
        
        if (string.IsNullOrEmpty(dialogPath))
        {
            Debug.LogError("InteractionWaitManager: Dialog path cannot be null or empty!");
            return;
        }
        
        objectDialogPaths[objectName] = dialogPath;
        
        if (enableDebugLogs)
        {
            Debug.Log($"InteractionWaitManager: Registered dialog '{dialogPath}' for object '{objectName}'");
        }
    }
    
    /// <summary>
    /// Get the dialog path for a specific object
    /// </summary>
    /// <param name="objectName">Name of the GameObject</param>
    /// <returns>Dialog path if registered, empty string otherwise</returns>
    public string GetDialogPathForObject(string objectName)
    {
        if (objectDialogPaths.ContainsKey(objectName))
        {
            return objectDialogPaths[objectName];
        }
        return "";
    }
    
    /// <summary>
    /// Remove dialog registration for an object
    /// </summary>
    /// <param name="objectName">Name of the GameObject</param>
    public void UnregisterDialogForObject(string objectName)
    {
        if (objectDialogPaths.ContainsKey(objectName))
        {
            objectDialogPaths.Remove(objectName);
            
            if (enableDebugLogs)
            {
                Debug.Log($"InteractionWaitManager: Unregistered dialog for object '{objectName}'");
            }
        }
    }
    
    /// <summary>
    /// Clear all dialog registrations
    /// </summary>
    public void ClearAllDialogRegistrations()
    {
        int count = objectDialogPaths.Count;
        objectDialogPaths.Clear();
        
        if (enableDebugLogs)
        {
            Debug.Log($"InteractionWaitManager: Cleared {count} dialog registrations");
        }
    }
}
