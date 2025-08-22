using UnityEngine;

/// <summary>
/// Component that handles interaction behavior for specific objects
/// Attach this to objects that should trigger dialogs when interacted with
/// </summary>
public class InteractiveObjectBehaviour : RaycastObjectBehaviour
{
    [Header("Dialog Settings")]
    [SerializeField] private string dialogResourcePath;
    [SerializeField] private bool hasBeenUsed = false;
    
    private CoreGameManager dialogManager;
    private System.Action onInteractionComplete;
    
    public void Setup(string dialogPath, CoreGameManager manager, System.Action onComplete)
    {
        dialogResourcePath = dialogPath;
        dialogManager = manager;
        onInteractionComplete = onComplete;
    }
    
    public void SetDialogPath(string newPath)
    {
        dialogResourcePath = newPath;
    }
    
    public override void OnInteraction()
    {
        if (hasBeenUsed)
        {
            Debug.Log($"Object {gameObject.name} has already been interacted with.");
            return;
        }
        
        if (string.IsNullOrEmpty(dialogResourcePath))
        {
            Debug.LogWarning($"No dialog path set for {gameObject.name}");
            return;
        }
        
        if (dialogManager == null)
        {
            Debug.LogError($"No dialog manager assigned for {gameObject.name}");
            return;
        }
        
        // Mark as used to prevent multiple interactions
        hasBeenUsed = true;
        
        Debug.Log($"Starting interactive dialog: {dialogResourcePath}");
        
        // Start the dialog
        dialogManager.StartCoreGame(dialogResourcePath, () => {
            Debug.Log($"Interactive dialog completed: {dialogResourcePath}");
            onInteractionComplete?.Invoke();
        });
        
        base.OnInteraction();
    }
    
    /// <summary>
    /// Reset the interaction state (useful for testing or replay scenarios)
    /// </summary>
    public void ResetInteraction()
    {
        hasBeenUsed = false;
    }
}
