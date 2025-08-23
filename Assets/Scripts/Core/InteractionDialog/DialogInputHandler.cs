using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using GenshinImpactMovementSystem;
/// <summary>
/// Simple Input Handler untuk migrasi dari Input.GetKeyDown ke Player Input Actions
/// Attach script ini ke CoreGameManager dan assign PlayerInputActions
/// </summary>
public class DialogInputHandler : MonoBehaviour
{
    [Header("Input Actions")]
    private PlayerInputActions playerInputActions;
    
    [Header("Target Manager")]
    public CoreGameManager coreGameManager;
    
    // Events untuk button press
    public System.Action OnDialogPressed;
    public System.Action OnChoiceQPressed;
    public System.Action OnChoiceWPressed;
    public System.Action OnChoiceEPressed;
    public System.Action OnEscapePressed;
    
    // Input state tracking
    private bool isDialogPressed = false;
    private bool isChoiceQPressed = false;
    private bool isChoiceWPressed = false;
    private bool isChoiceEPressed = false;
    private bool isEscapePressed = false;
    
    private void Awake()
    {
        // Auto-find CoreGameManager if not assigned
        if (coreGameManager == null)
        {
            coreGameManager = FindFirstObjectByType<CoreGameManager>();
        }
        
        // Create input actions
        playerInputActions = new PlayerInputActions();
    }
    
    private void OnEnable()
    {
        // Enable input actions
        playerInputActions.Enable();
        
        // Subscribe to input events
        playerInputActions.Player.Dialog.performed += OnDialogInput;
        playerInputActions.Player.ChoiceQ.performed += OnChoiceQInput;
        playerInputActions.Player.ChoiceW.performed += OnChoiceWInput;
        playerInputActions.Player.ChoiceE.performed += OnChoiceEInput;
        playerInputActions.Player.Escape.performed += OnEscapeInput; // Add Escape input
    }
    
    private void OnDisable()
    {
        // Unsubscribe from input events
        if (playerInputActions != null)
        {
            playerInputActions.Player.Dialog.performed -= OnDialogInput;
            playerInputActions.Player.ChoiceQ.performed -= OnChoiceQInput;
            playerInputActions.Player.ChoiceW.performed -= OnChoiceWInput;
            playerInputActions.Player.ChoiceE.performed -= OnChoiceEInput;
            playerInputActions.Player.Escape.performed -= OnEscapeInput; // Add Escape unsubscribe
            
            playerInputActions.Disable();
        }
    }
    
    private void Update()
    {
        // All input handled via PlayerInputActions now, including Escape
        // If Unity hasn't regenerated PlayerInputActions.cs yet, the Escape action will appear after build
    }
    
    #region Input Event Handlers
    
    private void OnDialogInput(InputAction.CallbackContext context)
    {
        isDialogPressed = true;
        OnDialogPressed?.Invoke();
        Debug.Log("[DialogInputHandler] Dialog input pressed via Input Actions");
    }
    
    private void OnChoiceQInput(InputAction.CallbackContext context)
    {
        isChoiceQPressed = true;
        OnChoiceQPressed?.Invoke();
        Debug.Log("[DialogInputHandler] Choice Q input pressed via Input Actions");
    }
    
    private void OnChoiceWInput(InputAction.CallbackContext context)
    {
        isChoiceWPressed = true;
        OnChoiceWPressed?.Invoke();
        Debug.Log("[DialogInputHandler] Choice W input pressed via Input Actions");
    }
    
    private void OnChoiceEInput(InputAction.CallbackContext context)
    {
        isChoiceEPressed = true;
        OnChoiceEPressed?.Invoke();
        Debug.Log("[DialogInputHandler] Choice E input pressed via Input Actions");
    }
    
    private void OnEscapeInput(InputAction.CallbackContext context)
    {
        isEscapePressed = true;
        OnEscapePressed?.Invoke();
        Debug.Log("[DialogInputHandler] Escape input pressed via Input Actions");
    }
    
    #endregion
    
    #region Public Methods for Input State Check (Replacement for Input.GetKeyDown)
    
    /// <summary>
    /// Clear all input states - useful when showing new dialog choices
    /// </summary>
    public void ClearAllInputStates()
    {
        isDialogPressed = false;
        isChoiceQPressed = false;
        isChoiceWPressed = false;
        isChoiceEPressed = false;
        isEscapePressed = false;
        Debug.Log("[DialogInputHandler] All input states cleared");
    }
    
    /// <summary>
    /// Clear only choice input states - use when showing new choices
    /// </summary>
    public void ClearChoiceInputStates()
    {
        isChoiceQPressed = false;
        isChoiceWPressed = false;
        isChoiceEPressed = false;
        Debug.Log("[DialogInputHandler] Choice input states cleared");
    }
    
    /// <summary>
    /// Replacement untuk Input.GetKeyDown(KeyCode.Space)
    /// Call ini di Update loop, akan return true sekali per press
    /// </summary>
    public bool GetDialogKeyDown()
    {
        if (isDialogPressed)
        {
            isDialogPressed = false; // Reset setelah digunakan
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Replacement untuk Input.GetKeyDown(KeyCode.Q)
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
    /// Replacement untuk Input.GetKeyDown(KeyCode.W)
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
    /// Replacement untuk Input.GetKeyDown(KeyCode.E)
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
    /// Replacement untuk Input.GetKeyDown(KeyCode.Escape)
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
    
    #endregion
}
