using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PausedScene : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuUI;
    public Image pauseMenuImage; // Optional, if you want to use an image instead of a panel
    public TMP_Text textUi1;
    public TMP_Text textUi2;
    public TMP_Text textUi3;
    
    [Header("Animation Settings")]
    [SerializeField] private float menuFadeDuration = 0.5f;
    [SerializeField] private float textFadeDuration = 0.3f;
    [SerializeField] private float textDelayBetween = 0.1f;
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeOutQuad;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private bool isPaused = false;
    private CanvasGroup menuCanvasGroup;
    private CanvasGroup textCanvasGroup1;
    private CanvasGroup textCanvasGroup2;
    private CanvasGroup textCanvasGroup3;
    
    void Start()
    {
        InitializePauseMenu();
    }
    
    void Update()
    {
        // Check for Escape key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
        
        // Check for Q key press when paused (go to main menu)
        if (isPaused && Input.GetKeyDown(KeyCode.Q))
        {
            GoToMainMenu();
        }
    }
    
    /// <summary>
    /// Initialize the pause menu and setup canvas groups
    /// </summary>
    private void InitializePauseMenu()
    {
        // Setup canvas group for pause menu
        if (pauseMenuUI != null)
        {
            menuCanvasGroup = pauseMenuUI.GetComponent<CanvasGroup>();
            if (menuCanvasGroup == null)
            {
                menuCanvasGroup = pauseMenuUI.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Start with menu hidden
            menuCanvasGroup.alpha = 0f;
            pauseMenuUI.gameObject.SetActive(false);
        }
        
        // Setup canvas groups for text elements
        SetupTextCanvasGroup(textUi1, ref textCanvasGroup1);
        SetupTextCanvasGroup(textUi2, ref textCanvasGroup2);
        SetupTextCanvasGroup(textUi3, ref textCanvasGroup3);
        
        LogDebug("Pause menu initialized");
    }
    
    /// <summary>
    /// Setup canvas group for a text element
    /// </summary>
    private void SetupTextCanvasGroup(TMP_Text textElement, ref CanvasGroup canvasGroup)
    {
        if (textElement != null)
        {
            canvasGroup = textElement.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = textElement.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Start with text hidden
            canvasGroup.alpha = 0f;
        }
    }
    
    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumePause();
        }
        else
        {
            ShowPause();
        }
    }
    
    /// <summary>
    /// Show pause menu with animations
    /// </summary>
    public void ShowPause()
    {
        if (isPaused) return;
        
        isPaused = true;
        LogDebug("Showing pause menu");
        
        // Pause the game
        Time.timeScale = 0f;
        
        // Step 1: Set pauseMenuUI gameobject to active
        if (pauseMenuUI != null)
        {
            pauseMenuUI.gameObject.SetActive(true);
            
            // Step 2: Set pause menu image opacity to 255 (full opacity)
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 0f;
                LeanTween.alphaCanvas(menuCanvasGroup, 1f, menuFadeDuration) // 255/255 = 1f
                    .setEase(easeType)
                    .setIgnoreTimeScale(true) // Important for pause menus
                    .setOnComplete(() => {
                        // Step 3: After menu reaches full opacity, set text elements to 255 opacity
                        AnimateTextsToFullOpacity();
                    });
            }
            else
            {
                // If no canvas group, just animate texts
                AnimateTextsToFullOpacity();
            }
        }
        else
        {
            // If no menu, just animate texts
            AnimateTextsToFullOpacity();
        }
    }
    
    /// <summary>
    /// Hide pause menu with animations
    /// </summary>
    public void ResumePause()
    {
        if (!isPaused) return;
        
        isPaused = false;
        LogDebug("Hiding pause menu");
        
        // Animate texts out first
        AnimateTextsOut(() => {
            // After texts are hidden, animate menu out
            if (pauseMenuUI != null && menuCanvasGroup != null)
            {
                LeanTween.alphaCanvas(menuCanvasGroup, 0f, menuFadeDuration)
                    .setEase(easeType)
                    .setIgnoreTimeScale(true)
                    .setOnComplete(() => {
                        pauseMenuUI.gameObject.SetActive(false);
                        // Resume the game
                        Time.timeScale = 1f;
                    });
            }
            else
            {
                // Resume the game immediately if no menu
                Time.timeScale = 1f;
            }
        });
    }
    
    /// <summary>
    /// Animate text elements to full opacity (255)
    /// </summary>
    private void AnimateTextsToFullOpacity()
    {
        LogDebug("Animating texts to full opacity");
        
        // Animate textUi1 to full opacity (255/255 = 1f)
        if (textCanvasGroup1 != null)
        {
            textCanvasGroup1.alpha = 0f;
            LeanTween.alphaCanvas(textCanvasGroup1, 1f, textFadeDuration)
                .setEase(easeType)
                .setIgnoreTimeScale(true)
                .setDelay(0f);
        }
        
        // Animate textUi2 with delay to full opacity
        if (textCanvasGroup2 != null)
        {
            textCanvasGroup2.alpha = 0f;
            LeanTween.alphaCanvas(textCanvasGroup2, 1f, textFadeDuration)
                .setEase(easeType)
                .setIgnoreTimeScale(true)
                .setDelay(textDelayBetween);
        }
        
        // Animate textUi3 with more delay to full opacity
        if (textCanvasGroup3 != null)
        {
            textCanvasGroup3.alpha = 0f;
            LeanTween.alphaCanvas(textCanvasGroup3, 1f, textFadeDuration)
                .setEase(easeType)
                .setIgnoreTimeScale(true)
                .setDelay(textDelayBetween * 2f);
        }
    }
    
    /// <summary>
    /// Animate text elements out (fade to 0 opacity)
    /// </summary>
    private void AnimateTextsOut(System.Action onComplete = null)
    {
        LogDebug("Animating texts out");
        
        int completedAnimations = 0;
        int totalAnimations = 0;
        
        // Count valid text elements
        if (textCanvasGroup1 != null) totalAnimations++;
        if (textCanvasGroup2 != null) totalAnimations++;
        if (textCanvasGroup3 != null) totalAnimations++;
        
        // If no text elements, call completion immediately
        if (totalAnimations == 0)
        {
            onComplete?.Invoke();
            return;
        }
        
        System.Action checkCompletion = () => {
            completedAnimations++;
            if (completedAnimations >= totalAnimations)
            {
                onComplete?.Invoke();
            }
        };
        
        // Animate textUi3 first (reverse order)
        if (textCanvasGroup3 != null)
        {
            LeanTween.alphaCanvas(textCanvasGroup3, 0f, textFadeDuration * 0.8f)
                .setEase(easeType)
                .setIgnoreTimeScale(true)
                .setDelay(0f)
                .setOnComplete(() => checkCompletion());
        }
        
        // Animate textUi2
        if (textCanvasGroup2 != null)
        {
            LeanTween.alphaCanvas(textCanvasGroup2, 0f, textFadeDuration * 0.8f)
                .setEase(easeType)
                .setIgnoreTimeScale(true)
                .setDelay(textDelayBetween * 0.5f)
                .setOnComplete(() => checkCompletion());
        }
        
        // Animate textUi1 last
        if (textCanvasGroup1 != null)
        {
            LeanTween.alphaCanvas(textCanvasGroup1, 0f, textFadeDuration * 0.8f)
                .setEase(easeType)
                .setIgnoreTimeScale(true)
                .setDelay(textDelayBetween)
                .setOnComplete(() => checkCompletion());
        }
    }
    
    /// <summary>
    /// Public method to show pause (can be called from other scripts)
    /// </summary>
    [ContextMenu("Show Pause Menu")]
    public void ShowPauseMenu()
    {
        ShowPause();
    }
    
    /// <summary>
    /// Public method to hide pause (can be called from other scripts)
    /// </summary>
    [ContextMenu("Hide Pause Menu")]
    public void HidePauseMenu()
    {
        ResumePause();
    }
    
    /// <summary>
    /// Go to Main Menu scene when Q is pressed during pause
    /// </summary>
    public void GoToMainMenu()
    {
        LogDebug("Going to Main Menu scene");
        
        // Resume time scale before changing scenes
        Time.timeScale = 1f;
        
        // Load the Main Menu scene
        SceneManager.LoadScene("MainMenu");
    }
    
    /// <summary>
    /// Context menu method to test going to main menu
    /// </summary>
    [ContextMenu("Test Go To Main Menu")]
    public void TestGoToMainMenu()
    {
        GoToMainMenu();
    }
    
    /// <summary>
    /// Check if game is currently paused
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
    
    /// <summary>
    /// Force resume game (useful for scene transitions)
    /// </summary>
    public void ForceResume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.gameObject.SetActive(false);
        }
        
        LogDebug("Game force resumed");
    }
    
    /// <summary>
    /// Helper method for debug logging
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PausedScene] {message}");
        }
    }
    
    void OnDestroy()
    {
        // Clean up any running tweens
        if (menuCanvasGroup != null)
        {
            LeanTween.cancel(menuCanvasGroup.gameObject);
        }
        
        if (textCanvasGroup1 != null)
        {
            LeanTween.cancel(textCanvasGroup1.gameObject);
        }
        
        if (textCanvasGroup2 != null)
        {
            LeanTween.cancel(textCanvasGroup2.gameObject);
        }
        
        if (textCanvasGroup3 != null)
        {
            LeanTween.cancel(textCanvasGroup3.gameObject);
        }
        
        // Ensure game is not left paused
        Time.timeScale = 1f;
    }
}
