using UnityEngine;
using UnityEngine.InputSystem;
using GenshinImpactMovementSystem;
/// <summary>
/// Input Handler untuk Pause Menu menggunakan Player Input Actions
/// Handles Escape untuk toggle pause dan Q untuk back to main menu
/// </summary>
public class PauseInputHandler : MonoBehaviour
{
    [Header("Input Actions")]
    private PlayerInputActions playerInputActions;
    
    [Header("Target Pause Manager")]
    public PausedScene pausedScene;
    
    // Events untuk button press
    public System.Action OnEscapePressed;
    public System.Action OnBackToMainMenuPressed;
    
    // Input state tracking
    private bool isEscapePressed = false;
    private bool isBackToMainMenuPressed = false;
    
    private void Awake()
    {
        // Auto-find PausedScene if not assigned
        if (pausedScene == null)
        {
            pausedScene = FindFirstObjectByType<PausedScene>();
        }
        
        // Create input actions
        playerInputActions = new PlayerInputActions();
    }
    
    private void OnEnable()
    {
        // Enable input actions
        playerInputActions.Enable();
        
        // Subscribe to input events
        playerInputActions.Player.Escape.performed += OnEscapeInput;
        playerInputActions.Player.BackToMainMenu.performed += OnBackToMainMenuInput;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from input events
        if (playerInputActions != null)
        {
            playerInputActions.Player.Escape.performed -= OnEscapeInput;
            playerInputActions.Player.BackToMainMenu.performed -= OnBackToMainMenuInput;
            
            playerInputActions.Disable();
        }
    }
    
    #region Input Event Handlers
    
    private void OnEscapeInput(InputAction.CallbackContext context)
    {
        isEscapePressed = true;
        OnEscapePressed?.Invoke();
        
        // DON'T auto-trigger here - let PausedScene handle it in Update to prevent double trigger
        Debug.Log("[PauseInputHandler] Escape input pressed via Input Actions");
    }
    
    private void OnBackToMainMenuInput(InputAction.CallbackContext context)
    {
        isBackToMainMenuPressed = true;
        OnBackToMainMenuPressed?.Invoke();
        
        // DON'T auto-trigger here - let PausedScene handle it in Update to prevent double trigger
        Debug.Log("[PauseInputHandler] BackToMainMenu input pressed via Input Actions");
    }
    
    #endregion
    
    #region Public Methods for Input State Check
    
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
    
    /// <summary>
    /// Replacement untuk Input.GetKeyDown(KeyCode.Q)
    /// </summary>
    public bool GetBackToMainMenuKeyDown()
    {
        if (isBackToMainMenuPressed)
        {
            isBackToMainMenuPressed = false;
            return true;
        }
        return false;
    }
    
    #endregion
}
