using UnityEngine;
using UnityEngine.InputSystem;
using GenshinImpactMovementSystem;

/// <summary>
/// Universal Input Handler untuk semua sistem input di game
/// Gabungan dari DialogInputHandler, PauseInputHandler, NarratorInputHandler, dan ChargeMeterInputHandler
/// Tidak perlu assign apapun - otomatis handle semua input dengan PlayerInputActions
/// </summary>
public class BaseInputHandler : MonoBehaviour
{
    [Header("Input Actions")]
    private PlayerInputActions playerInputActions;
    
    [Header("Auto-Detection")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Events untuk semua button press
    public System.Action OnDialogPressed;        // Space - Dialog progression
    public System.Action OnInteractionPressed;  // E - General interactions
    public System.Action OnChoiceQPressed;      // Q - Choice 1 / Debug / Back to main menu
    public System.Action OnChoiceWPressed;      // W - Choice 2 / Debug
    public System.Action OnChoiceEPressed;      // E - Choice 3 (reuse interaction)
    public System.Action OnEscapePressed;       // Escape - Pause toggle / Skip cutscenes
    
    // Input state tracking untuk semua keys
    private bool isDialogPressed = false;       // Space
    private bool isInteractionPressed = false;  // E
    private bool isChoiceQPressed = false;      // Q
    private bool isChoiceWPressed = false;      // W
    private bool isChoiceEPressed = false;      // E (same as interaction)
    private bool isEscapePressed = false;       // Escape
    
    // Singleton instance untuk easy access
    public static BaseInputHandler Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInputActions();
            LogDebug("[BaseInputHandler] Singleton instance created and initialized");
        }
        else
        {
            Destroy(gameObject);
            LogDebug("[BaseInputHandler] Duplicate instance destroyed");
        }
    }
    
    private void InitializeInputActions()
    {
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInputActions();
        }
        
        // Enable input actions
        playerInputActions.Enable();
        
        // Subscribe to all input events
        playerInputActions.Player.Dialog.performed += OnDialogInput;        // Space
        playerInputActions.Player.Interaksi.performed += OnInteractionInput; // E
        playerInputActions.Player.ChoiceQ.performed += OnChoiceQInput;      // Q
        playerInputActions.Player.ChoiceW.performed += OnChoiceWInput;      // W
        playerInputActions.Player.ChoiceE.performed += OnChoiceEInput;      // E (reuse)
        playerInputActions.Player.Escape.performed += OnEscapeInput;        // Escape
        playerInputActions.Player.BackToMainMenu.performed += OnChoiceQInput; // Q (reuse for back to main menu)
        
        LogDebug("[BaseInputHandler] All input actions subscribed");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from all input events
        if (playerInputActions != null)
        {
            playerInputActions.Player.Dialog.performed -= OnDialogInput;
            playerInputActions.Player.Interaksi.performed -= OnInteractionInput;
            playerInputActions.Player.ChoiceQ.performed -= OnChoiceQInput;
            playerInputActions.Player.ChoiceW.performed -= OnChoiceWInput;
            playerInputActions.Player.ChoiceE.performed -= OnChoiceEInput;
            playerInputActions.Player.Escape.performed -= OnEscapeInput;
            playerInputActions.Player.BackToMainMenu.performed -= OnChoiceQInput;
            
            playerInputActions.Disable();
            playerInputActions.Dispose();
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
        
        LogDebug("[BaseInputHandler] Input actions cleaned up");
    }
    
    #region Input Event Handlers
    
    private void OnDialogInput(InputAction.CallbackContext context)
    {
        isDialogPressed = true;
        OnDialogPressed?.Invoke();
        LogDebug("[BaseInputHandler] Dialog (Space) input pressed");
    }
    
    private void OnInteractionInput(InputAction.CallbackContext context)
    {
        isInteractionPressed = true;
        isChoiceEPressed = true; // E key serves dual purpose
        OnInteractionPressed?.Invoke();
        OnChoiceEPressed?.Invoke();
        LogDebug("[BaseInputHandler] Interaction (E) input pressed");
    }
    
    private void OnChoiceQInput(InputAction.CallbackContext context)
    {
        isChoiceQPressed = true;
        OnChoiceQPressed?.Invoke();
        LogDebug("[BaseInputHandler] Choice Q input pressed");
    }
    
    private void OnChoiceWInput(InputAction.CallbackContext context)
    {
        isChoiceWPressed = true;
        OnChoiceWPressed?.Invoke();
        LogDebug("[BaseInputHandler] Choice W input pressed");
    }
    
    private void OnChoiceEInput(InputAction.CallbackContext context)
    {
        isChoiceEPressed = true;
        OnChoiceEPressed?.Invoke();
        LogDebug("[BaseInputHandler] Choice E input pressed");
    }
    
    private void OnEscapeInput(InputAction.CallbackContext context)
    {
        isEscapePressed = true;
        OnEscapePressed?.Invoke();
        LogDebug("[BaseInputHandler] Escape input pressed");
    }
    
    #endregion
    
    #region Public Methods for Input State Check (Universal Replacements)
    
    /// <summary>
    /// Clear all input states - useful when changing scenes/states
    /// </summary>
    public void ClearAllInputStates()
    {
        isDialogPressed = false;
        isInteractionPressed = false;
        isChoiceQPressed = false;
        isChoiceWPressed = false;
        isChoiceEPressed = false;
        isEscapePressed = false;
        LogDebug("[BaseInputHandler] All input states cleared");
    }
    
    /// <summary>
    /// Clear only choice input states - use when showing new dialog choices
    /// </summary>
    public void ClearChoiceInputStates()
    {
        isChoiceQPressed = false;
        isChoiceWPressed = false;
        isChoiceEPressed = false;
        LogDebug("[BaseInputHandler] Choice input states cleared");
    }
    
    /// <summary>
    /// Universal replacement untuk Input.GetKeyDown(KeyCode.Space)
    /// Untuk dialog progression, charge meter, dll
    /// </summary>
    public bool GetDialogKeyDown()
    {
        if (isDialogPressed)
        {
            isDialogPressed = false;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Universal replacement untuk Input.GetKeyDown(KeyCode.E)
    /// Untuk interactions, choice E, dll
    /// </summary>
    public bool GetInteractionKeyDown()
    {
        if (isInteractionPressed)
        {
            isInteractionPressed = false;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Universal replacement untuk Input.GetKeyDown(KeyCode.Q)
    /// Untuk choice Q, debug, back to main menu
    /// </summary>
    public bool GetChoiceQKeyDown()
    {
        if (isChoiceQPressed)
        {
            isChoiceQPressed = false;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Universal replacement untuk Input.GetKeyDown(KeyCode.W)
    /// Untuk choice W, debug
    /// </summary>
    public bool GetChoiceWKeyDown()
    {
        if (isChoiceWPressed)
        {
            isChoiceWPressed = false;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Universal replacement untuk Input.GetKeyDown(KeyCode.E) for choices
    /// Same as GetInteractionKeyDown() but separate for clarity
    /// </summary>
    public bool GetChoiceEKeyDown()
    {
        if (isChoiceEPressed)
        {
            isChoiceEPressed = false;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Universal replacement untuk Input.GetKeyDown(KeyCode.Escape)
    /// Untuk pause toggle, skip cutscenes, dll
    /// </summary>
    public bool GetEscapeKeyDown()
    {
        if (isEscapePressed)
        {
            isEscapePressed = false;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Alias untuk GetChoiceQKeyDown() - untuk pause menu back to main
    /// </summary>
    public bool GetBackToMainMenuKeyDown()
    {
        return GetChoiceQKeyDown(); // Q key serves dual purpose
    }
    
    /// <summary>
    /// Alias untuk GetDialogKeyDown() - untuk charge meter space press
    /// </summary>
    public bool GetSpaceKeyDown()
    {
        return GetDialogKeyDown(); // Space key serves dual purpose
    }
    
    /// <summary>
    /// Fallback untuk Input.GetKeyDown(KeyCode.B) - until B key added to PlayerInputActions
    /// </summary>
    public bool GetDebugBKeyDown()
    {
        return Input.GetKeyDown(KeyCode.B); // Fallback for now
    }
    
    #endregion
    
    #region Static Access Methods (No need to assign anything!)
    
    /// <summary>
    /// Static access untuk dialog input - no assignment needed!
    /// </summary>
    public static bool DialogKeyDown => Instance?.GetDialogKeyDown() ?? false;
    
    /// <summary>
    /// Static access untuk interaction input - no assignment needed!
    /// </summary>
    public static bool InteractionKeyDown => Instance?.GetInteractionKeyDown() ?? false;
    
    /// <summary>
    /// Static access untuk choice Q input - no assignment needed!
    /// </summary>
    public static bool ChoiceQKeyDown => Instance?.GetChoiceQKeyDown() ?? false;
    
    /// <summary>
    /// Static access untuk choice W input - no assignment needed!
    /// </summary>
    public static bool ChoiceWKeyDown => Instance?.GetChoiceWKeyDown() ?? false;
    
    /// <summary>
    /// Static access untuk choice E input - no assignment needed!
    /// </summary>
    public static bool ChoiceEKeyDown => Instance?.GetChoiceEKeyDown() ?? false;
    
    /// <summary>
    /// Static access untuk escape input - no assignment needed!
    /// </summary>
    public static bool EscapeKeyDown => Instance?.GetEscapeKeyDown() ?? false;
    
    /// <summary>
    /// Static method untuk clear all input states - no assignment needed!
    /// </summary>
    public static void ClearAll() => Instance?.ClearAllInputStates();
    
    /// <summary>
    /// Static method untuk clear choice input states - no assignment needed!
    /// </summary>
    public static void ClearChoices() => Instance?.ClearChoiceInputStates();
    
    #endregion
    
    #region Debug Helper
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    #endregion
}

/*
 * USAGE EXAMPLES:
 * 
 * 1. DIALOG SYSTEM (CoreGameManager):
 *    if (BaseInputHandler.DialogKeyDown) { // Progress dialog }
 *    if (BaseInputHandler.ChoiceQKeyDown) { // Select choice 1 }
 * 
 * 2. PAUSE SYSTEM (PausedScene):
 *    if (BaseInputHandler.EscapeKeyDown) { // Toggle pause }
 *    if (BaseInputHandler.ChoiceQKeyDown) { // Back to main menu }
 * 
 * 3. NARRATOR SYSTEM (NarratorBase):
 *    if (BaseInputHandler.InteractionKeyDown) { // Interact with object }
 * 
 * 4. CHARGE METER (ChargeMeter):
 *    if (BaseInputHandler.DialogKeyDown) { // Space press for charging }
 * 
 * NO ASSIGNMENT REQUIRED! Just create one BaseInputHandler in scene and everything works!
 */
