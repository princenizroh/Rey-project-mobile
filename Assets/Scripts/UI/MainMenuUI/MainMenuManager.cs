using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.IO;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Button References")]
    public GameObject gameLogo;
    public GameObject startButton;
    public GameObject continueButton;
    public GameObject optionsButton;
    public GameObject creditsButton;
    public GameObject quitButton;
    public GameObject backButton;
    public GameObject creditsSprite;
    public GameObject optionsSprite;
    public TMP_Text startGameButtonText;
    public TMP_Text resolution;
    public TMP_Text volume;
    public CoreGameSaves targetScriptableObject;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float stairDelay = 0.1f; // Delay between each button animation
    [SerializeField] private float hoverMoveDistance = 20f;
    [SerializeField] private float hoverDuration = 0.3f;
    [SerializeField] private float clickMovePercentage = 150f; // Percentage to move all buttons when clicked
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType hoverEaseType = LeanTweenType.easeOutQuad;
    
    [Header("Logo Animation Settings")]
    [SerializeField] private float logoBreathingScale = 1.05f; // Scale multiplier for breathing effect
    [SerializeField] private float logoBreathingDuration = 2.5f; // Duration of one breathing cycle
    [SerializeField] private float logoRotationAngle = 2f; // Rotation angle in degrees
    [SerializeField] private float logoRotationDuration = 4f; // Duration of one rotation cycle
    [SerializeField] private bool enableLogoAnimation = true;
    
    [Header("Sprite Animation Settings")]
    [SerializeField] private float spriteAnimationDuration = 0.8f;
    [SerializeField] private float spriteDelayOffset = 0.3f;
    [SerializeField] private LeanTweenType spriteEnterEase = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType spriteExitEase = LeanTweenType.easeInBack;
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private bool useFadeAnimation = true;
    [SerializeField] private float scaleAnimationDuration = 0.6f;
    [SerializeField] private float fadeAnimationDuration = 0.5f;

    [Header("Save File Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Settings Control")]
    [SerializeField] private int currentResolutionIndex = 0;
    [SerializeField] private int currentVolumeLevel = 60;
    [SerializeField] private int volumeMin = 0;
    [SerializeField] private int volumeMax = 100;
    [SerializeField] private int volumeStep = 5;
    
    [Header("Keyboard Navigation")]
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;
    [SerializeField] private KeyCode selectKey = KeyCode.Return; // Enter key to select
    [SerializeField] private KeyCode spaceSelectKey = KeyCode.Space; // Space key to select
    [SerializeField] private KeyCode leftKey = KeyCode.A; // A key for left/decrease
    [SerializeField] private KeyCode rightKey = KeyCode.D; // D key for right/increase
    [SerializeField] private bool enableKeyboardNavigation = true;
    [SerializeField] private float navigationInputCooldown = 0.2f;
    [SerializeField] private bool enableSettingsNavigation = true;
    
    // Available resolutions
    private Resolution[] availableResolutions;
    private string[] resolutionStrings;

    private GameObject[] buttons;
    private Vector3[] originalPositions;
    private bool isAnimating = false;
    private bool buttonsVisible = true;
    
    // Sprite animation states
    private bool creditsVisible = false;
    private bool optionsVisible = false;
    private Vector3 creditsOffScreenPos;
    private Vector3 optionsOffScreenPos;

    // Keyboard navigation
    private int currentSelectedButtonIndex = 0;
    private float lastNavigationInput = 0f;
    private bool navigationActive = false;

    // Settings navigation
    private bool isInOptionsMenu = false;
    private int currentSettingsIndex = 0; // 0 = resolution, 1 = volume
    private GameObject[] settingsElements;
    private Vector3 resolutionOriginalPos;
    private Vector3 volumeOriginalPos;

    // Submenu navigation (when back button is visible)
    private bool isInSubmenu = false;
    private bool isInCreditsMenu = false;

    // Logo animation
    private Vector3 logoOriginalScale;
    private Vector3 logoOriginalRotation;

    // Save file detection
    [System.Serializable]
    public class SaveData
    {
        public int day;
        public int mother_stress_level;
        public TimeOfDay timeOfDay; // Add timeOfDay support
        
        public SaveData()
        {
            day = 0;
            mother_stress_level = 0;
            timeOfDay = TimeOfDay.Morning; // Default to Morning
        }
    }

    /// <summary>
    /// Temporary data structure for custom JSON parsing (handles string enums)
    /// </summary>
    [System.Serializable]
    private class TempSaveData
    {
        public int day = 0;
        public int mother_stress_level = 0;
        public string timeOfDay = "Morning"; // Parse as string first
    }

    /// <summary>
    /// Parse save data from JSON with support for both integer and string enum values
    /// </summary>
    private SaveData ParseSaveDataFromJSON(string jsonContent)
    {
        if (string.IsNullOrEmpty(jsonContent))
        {
            LogDebug("JSON content is null or empty");
            return null;
        }

        // Check if the JSON contains string enum values (contains quotes around timeOfDay value)
        bool hasStringEnum = jsonContent.Contains("\"timeOfDay\": \"");
        
        if (hasStringEnum)
        {
            LogDebug("Detected string enum format, using custom parser...");
            return ParseSaveDataWithCustomEnum(jsonContent);
        }

        try
        {
            // Try standard Unity JsonUtility parsing for integer enums
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonContent);
            if (saveData != null)
            {
                LogDebug("Successfully parsed with standard JsonUtility");
                return saveData;
            }
        }
        catch (System.Exception e)
        {
            LogDebug($"Standard JsonUtility parsing failed: {e.Message}, trying custom parser...");
        }

        // Fallback to custom parsing for string enum values
        return ParseSaveDataWithCustomEnum(jsonContent);
    }

    /// <summary>
    /// Parse save data with custom enum handling (supports string enum values)
    /// </summary>
    private SaveData ParseSaveDataWithCustomEnum(string jsonContent)
    {
        try
        {
            // Parse using TempSaveData which treats timeOfDay as string
            TempSaveData tempData = JsonUtility.FromJson<TempSaveData>(jsonContent);
            
            if (tempData != null)
            {
                SaveData finalData = new SaveData();
                finalData.day = tempData.day;
                finalData.mother_stress_level = tempData.mother_stress_level;
                
                // Convert string timeOfDay to enum
                if (System.Enum.TryParse<TimeOfDay>(tempData.timeOfDay, true, out TimeOfDay parsedTimeOfDay))
                {
                    finalData.timeOfDay = parsedTimeOfDay;
                    LogDebug($"Successfully parsed timeOfDay string '{tempData.timeOfDay}' to enum {parsedTimeOfDay}");
                }
                else
                {
                    finalData.timeOfDay = TimeOfDay.Morning; // Default fallback
                    LogDebug($"Failed to parse timeOfDay '{tempData.timeOfDay}', using default: {finalData.timeOfDay}");
                }
                
                LogDebug("Successfully parsed with custom enum parser");
                return finalData;
            }
        }
        catch (System.Exception e)
        {
            LogDebug($"Custom enum parsing also failed: {e.Message}");
        }
        
        LogDebug("All parsing attempts failed, returning null");
        return null;
    }

    void Start()
    {
        InitializeButtons();
        InitializeSprites();
        SetupButtonEvents();
        InitializeSettings();
        CheckSaveFileAndUpdateContinueButton();
        
        // Initialize logo animation
        InitializeLogoAnimation();
        
        // Start with buttons visible
        ShowButtons();
        
        // Initialize keyboard navigation
        InitializeKeyboardNavigation();
        
        // Debug: Check ScriptableObject assignment and current values
        DebugScriptableObjectState();
    }

    void Update()
    {
        HandleKeyboardNavigation();
    }

    void InitializeButtons()
    {
        // Store all buttons in array for easy iteration (excluding back button)
        buttons = new GameObject[] { startButton, continueButton, optionsButton, creditsButton, quitButton };
        
        // Store original positions
        originalPositions = new Vector3[buttons.Length];
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                originalPositions[i] = buttons[i].transform.localPosition;
            }
        }

        // Initialize back button (hidden initially)
        if (backButton != null)
        {
            backButton.SetActive(false);
        }
    }

    void InitializeSprites()
    {
        // Initialize credits sprite
        if (creditsSprite != null)
        {
            creditsOffScreenPos = new Vector3(1000f, creditsSprite.transform.localPosition.y, creditsSprite.transform.localPosition.z);
            creditsSprite.transform.localPosition = creditsOffScreenPos;
            creditsSprite.SetActive(false);
            
            // Initialize scale and alpha if using animations
            if (useScaleAnimation)
            {
                creditsSprite.transform.localScale = Vector3.zero;
            }
            if (useFadeAnimation)
            {
                CanvasGroup creditsCanvasGroup = creditsSprite.GetComponent<CanvasGroup>();
                if (creditsCanvasGroup == null)
                {
                    creditsCanvasGroup = creditsSprite.AddComponent<CanvasGroup>();
                }
                creditsCanvasGroup.alpha = 0f;
            }
        }

        // Initialize options sprite
        if (optionsSprite != null)
        {
            optionsOffScreenPos = new Vector3(3000f, optionsSprite.transform.localPosition.y, optionsSprite.transform.localPosition.z);
            optionsSprite.transform.localPosition = optionsOffScreenPos;
            optionsSprite.SetActive(false);
            
            // Initialize scale and alpha if using animations
            if (useScaleAnimation)
            {
                optionsSprite.transform.localScale = Vector3.zero;
            }
            if (useFadeAnimation)
            {
                CanvasGroup optionsCanvasGroup = optionsSprite.GetComponent<CanvasGroup>();
                if (optionsCanvasGroup == null)
                {
                    optionsCanvasGroup = optionsSprite.AddComponent<CanvasGroup>();
                }
                optionsCanvasGroup.alpha = 0f;
            }
        }
    }

    void InitializeSettings()
    {
        // Initialize available resolutions
        InitializeResolutions();
        
        // Initialize volume
        UpdateVolumeDisplay();
        
        // Setup interactive controls
        SetupVolumeScrollWheel();
        SetupResolutionClickDetection();
        SetupVolumeClickDetection();
        
        LogDebug("Settings initialized with interactive controls");
    }

    /// <summary>
    /// Initialize keyboard navigation
    /// </summary>
    void InitializeKeyboardNavigation()
    {
        if (!enableKeyboardNavigation) return;
        
        currentSelectedButtonIndex = 0;
        navigationActive = buttonsVisible;
        
        // Highlight the first button
        if (navigationActive && buttons != null && buttons.Length > 0)
        {
            HighlightSelectedButton();
        }
        
        LogDebug("Keyboard navigation initialized");
    }

    /// <summary>
    /// Initialize logo animation
    /// </summary>
    void InitializeLogoAnimation()
    {
        if (gameLogo == null || !enableLogoAnimation) return;
        
        // Store original transform values
        logoOriginalScale = gameLogo.transform.localScale;
        logoOriginalRotation = gameLogo.transform.localEulerAngles;
        
        // Start the breathing and rotation animations
        StartLogoBreathingAnimation();
        StartLogoRotationAnimation();
        
        LogDebug("Logo animation initialized");
    }

    /// <summary>
    /// Start the logo breathing animation (scale effect)
    /// </summary>
    void StartLogoBreathingAnimation()
    {
        if (gameLogo == null) return;
        
        // Scale up (inhale)
        LeanTween.scale(gameLogo, logoOriginalScale * logoBreathingScale, logoBreathingDuration)
            .setEase(LeanTweenType.easeInOutSine)
            .setOnComplete(() => {
                // Scale down (exhale)
                LeanTween.scale(gameLogo, logoOriginalScale, logoBreathingDuration)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setOnComplete(() => {
                        // Loop the breathing animation
                        StartLogoBreathingAnimation();
                    });
            });
    }

    /// <summary>
    /// Start the logo rotation animation (subtle rotation)
    /// </summary>
    void StartLogoRotationAnimation()
    {
        if (gameLogo == null) return;
        
        // Rotate to one side
        Vector3 rotateRight = logoOriginalRotation + Vector3.forward * logoRotationAngle;
        LeanTween.rotateLocal(gameLogo, rotateRight, logoRotationDuration)
            .setEase(LeanTweenType.easeInOutSine)
            .setOnComplete(() => {
                // Rotate to the other side
                Vector3 rotateLeft = logoOriginalRotation + Vector3.forward * -logoRotationAngle;
                LeanTween.rotateLocal(gameLogo, rotateLeft, logoRotationDuration)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setOnComplete(() => {
                        // Return to center and loop
                        LeanTween.rotateLocal(gameLogo, logoOriginalRotation, logoRotationDuration)
                            .setEase(LeanTweenType.easeInOutSine)
                            .setOnComplete(() => {
                                // Loop the rotation animation
                                StartLogoRotationAnimation();
                            });
                    });
            });
    }

    /// <summary>
    /// Stop logo animations
    /// </summary>
    void StopLogoAnimation()
    {
        if (gameLogo == null) return;
        
        // Cancel any existing animations
        LeanTween.cancel(gameLogo);
        
        // Reset to original transform
        gameLogo.transform.localScale = logoOriginalScale;
        gameLogo.transform.localEulerAngles = logoOriginalRotation;
        
        LogDebug("Logo animation stopped");
    }

    /// <summary>
    /// Handle keyboard navigation input
    /// </summary>
    void HandleKeyboardNavigation()
    {
        if (!enableKeyboardNavigation) return;
        
        // Check for cooldown
        if (Time.time - lastNavigationInput < navigationInputCooldown) return;
        
        bool inputDetected = false;
        
        // Handle settings navigation when in options menu
        if (isInOptionsMenu && enableSettingsNavigation)
        {
            if (Input.GetKeyDown(upKey))
            {
                NavigateSettingsUp();
                inputDetected = true;
            }
            else if (Input.GetKeyDown(downKey))
            {
                NavigateSettingsDown();
                inputDetected = true;
            }
            else if (Input.GetKeyDown(leftKey))
            {
                NavigateSettingsLeft();
                inputDetected = true;
            }
            else if (Input.GetKeyDown(rightKey))
            {
                NavigateSettingsRight();
                inputDetected = true;
            }
            // Allow selection of back button in options menu
            else if (Input.GetKeyDown(selectKey) || Input.GetKeyDown(spaceSelectKey))
            {
                SelectBackButton();
                inputDetected = true;
            }
        }
        // Handle back button navigation when in submenu (credits)
        else if (isInSubmenu && (isInCreditsMenu || optionsVisible))
        {
            if (Input.GetKeyDown(selectKey) || Input.GetKeyDown(spaceSelectKey))
            {
                SelectBackButton();
                inputDetected = true;
            }
        }
        // Handle regular button navigation
        else if (navigationActive && buttonsVisible && !isAnimating)
        {
            // Handle up navigation (W key)
            if (Input.GetKeyDown(upKey))
            {
                NavigateUp();
                inputDetected = true;
            }
            // Handle down navigation (S key)
            else if (Input.GetKeyDown(downKey))
            {
                NavigateDown();
                inputDetected = true;
            }
            // Handle selection (Enter key or Space key)
            else if (Input.GetKeyDown(selectKey) || Input.GetKeyDown(spaceSelectKey))
            {
                SelectCurrentButton();
                inputDetected = true;
            }
        }
        
        if (inputDetected)
        {
            lastNavigationInput = Time.time;
        }
    }

    /// <summary>
    /// Navigate to the previous button (up)
    /// </summary>
    void NavigateUp()
    {
        if (buttons == null || buttons.Length == 0) return;
        
        // Find previous valid button
        int originalIndex = currentSelectedButtonIndex;
        do
        {
            currentSelectedButtonIndex--;
            if (currentSelectedButtonIndex < 0)
            {
                currentSelectedButtonIndex = buttons.Length - 1;
            }
        } while (!IsButtonInteractable(currentSelectedButtonIndex) && currentSelectedButtonIndex != originalIndex);
        
        HighlightSelectedButton();
        LogDebug($"Navigated up to button {currentSelectedButtonIndex}");
    }

    /// <summary>
    /// Navigate to the next button (down)
    /// </summary>
    void NavigateDown()
    {
        if (buttons == null || buttons.Length == 0) return;
        
        // Find next valid button
        int originalIndex = currentSelectedButtonIndex;
        do
        {
            currentSelectedButtonIndex++;
            if (currentSelectedButtonIndex >= buttons.Length)
            {
                currentSelectedButtonIndex = 0;
            }
        } while (!IsButtonInteractable(currentSelectedButtonIndex) && currentSelectedButtonIndex != originalIndex);
        
        HighlightSelectedButton();
        LogDebug($"Navigated down to button {currentSelectedButtonIndex}");
    }

    /// <summary>
    /// Check if a button is interactable
    /// </summary>
    bool IsButtonInteractable(int buttonIndex)
    {
        if (buttons == null || buttonIndex < 0 || buttonIndex >= buttons.Length) return false;
        if (buttons[buttonIndex] == null) return false;
        
        Button buttonComponent = buttons[buttonIndex].GetComponent<Button>();
        if (buttonComponent == null) return false;
        
        return buttonComponent.interactable && buttons[buttonIndex].activeInHierarchy;
    }

    /// <summary>
    /// Highlight the currently selected button
    /// </summary>
    void HighlightSelectedButton()
    {
        if (buttons == null || currentSelectedButtonIndex < 0 || currentSelectedButtonIndex >= buttons.Length) return;
        
        // Remove highlight from all buttons first
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                OnButtonHover(i, false); // Remove hover effect
            }
        }
        
        // Highlight current button
        if (buttons[currentSelectedButtonIndex] != null && IsButtonInteractable(currentSelectedButtonIndex))
        {
            OnButtonHover(currentSelectedButtonIndex, true); // Apply hover effect
        }
    }

    /// <summary>
    /// Select the currently highlighted button
    /// </summary>
    void SelectCurrentButton()
    {
        if (buttons == null || currentSelectedButtonIndex < 0 || currentSelectedButtonIndex >= buttons.Length) return;
        
        GameObject selectedButton = buttons[currentSelectedButtonIndex];
        if (selectedButton != null && IsButtonInteractable(currentSelectedButtonIndex))
        {
            Button buttonComponent = selectedButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                // Trigger the button click
                buttonComponent.onClick.Invoke();
                LogDebug($"Selected button: {selectedButton.name}");
            }
        }
    }

    /// <summary>
    /// Select the back button when in submenus
    /// </summary>
    void SelectBackButton()
    {
        if (backButton == null) return;
        
        Button backButtonComponent = backButton.GetComponent<Button>();
        if (backButtonComponent != null && backButton.activeInHierarchy)
        {
            // Add visual feedback when selecting back button
            OnBackButtonHover(true);
            
            // Small delay before triggering to show selection feedback
            LeanTween.delayedCall(0.1f, () => {
                // Trigger the back button click
                backButtonComponent.onClick.Invoke();
                LogDebug("Selected back button");
            });
        }
    }

    /// <summary>
    /// Enable keyboard navigation
    /// </summary>
    void EnableKeyboardNavigation()
    {
        if (!enableKeyboardNavigation) return;
        
        navigationActive = true;
        currentSelectedButtonIndex = 0;
        
        // Find first interactable button
        if (buttons != null && buttons.Length > 0)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (IsButtonInteractable(i))
                {
                    currentSelectedButtonIndex = i;
                    break;
                }
            }
            HighlightSelectedButton();
        }
        
        LogDebug("Keyboard navigation enabled");
    }

    /// <summary>
    /// Disable keyboard navigation
    /// </summary>
    void DisableKeyboardNavigation()
    {
        navigationActive = false;
        
        // Remove highlight from all buttons
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    OnButtonHover(i, false);
                }
            }
        }
        
        LogDebug("Keyboard navigation disabled");
    }

    /// <summary>
    /// Toggle keyboard navigation on/off (for testing)
    /// </summary>
    [ContextMenu("Toggle Keyboard Navigation")]
    public void ToggleKeyboardNavigation()
    {
        enableKeyboardNavigation = !enableKeyboardNavigation;
        
        if (enableKeyboardNavigation && buttonsVisible)
        {
            EnableKeyboardNavigation();
        }
        else
        {
            DisableKeyboardNavigation();
        }
        
        LogDebug($"Keyboard navigation toggled: {enableKeyboardNavigation}");
    }

    /// <summary>
    /// Toggle logo animation on/off (for testing)
    /// </summary>
    [ContextMenu("Toggle Logo Animation")]
    public void ToggleLogoAnimation()
    {
        enableLogoAnimation = !enableLogoAnimation;
        
        if (enableLogoAnimation)
        {
            InitializeLogoAnimation();
        }
        else
        {
            StopLogoAnimation();
        }
        
        LogDebug($"Logo animation toggled: {enableLogoAnimation}");
    }

    #region Settings Navigation Methods

    /// <summary>
    /// Initialize settings navigation elements
    /// </summary>
    void InitializeSettingsNavigation()
    {
        if (resolution != null && volume != null)
        {
            settingsElements = new GameObject[] { resolution.gameObject, volume.gameObject };
            resolutionOriginalPos = resolution.transform.localPosition;
            volumeOriginalPos = volume.transform.localPosition;
        }
    }

    /// <summary>
    /// Navigate up in settings (previous setting)
    /// </summary>
    void NavigateSettingsUp()
    {
        currentSettingsIndex--;
        if (currentSettingsIndex < 0)
        {
            currentSettingsIndex = settingsElements != null ? settingsElements.Length - 1 : 0;
        }
        HighlightCurrentSetting();
        LogDebug($"Settings navigation up - index: {currentSettingsIndex}");
    }

    /// <summary>
    /// Navigate down in settings (next setting)
    /// </summary>
    void NavigateSettingsDown()
    {
        currentSettingsIndex++;
        if (settingsElements != null && currentSettingsIndex >= settingsElements.Length)
        {
            currentSettingsIndex = 0;
        }
        HighlightCurrentSetting();
        LogDebug($"Settings navigation down - index: {currentSettingsIndex}");
    }

    /// <summary>
    /// Navigate left in settings (decrease value)
    /// </summary>
    void NavigateSettingsLeft()
    {
        if (currentSettingsIndex == 0) // Resolution
        {
            PreviousResolution();
            AnimateSettingChange(resolution.gameObject);
        }
        else if (currentSettingsIndex == 1) // Volume
        {
            DecreaseVolume();
            AnimateSettingChange(volume.gameObject);
        }
        LogDebug($"Settings navigation left - decreased value for setting {currentSettingsIndex}");
    }

    /// <summary>
    /// Navigate right in settings (increase value)
    /// </summary>
    void NavigateSettingsRight()
    {
        if (currentSettingsIndex == 0) // Resolution
        {
            NextResolution();
            AnimateSettingChange(resolution.gameObject);
        }
        else if (currentSettingsIndex == 1) // Volume
        {
            IncreaseVolume();
            AnimateSettingChange(volume.gameObject);
        }
        LogDebug($"Settings navigation right - increased value for setting {currentSettingsIndex}");
    }

    /// <summary>
    /// Highlight the currently selected setting
    /// </summary>
    void HighlightCurrentSetting()
    {
        if (settingsElements == null) return;

        // Remove highlight from all settings
        for (int i = 0; i < settingsElements.Length; i++)
        {
            if (settingsElements[i] != null)
            {
                LeanTween.cancel(settingsElements[i]);
                Vector3 originalPos = (i == 0) ? resolutionOriginalPos : volumeOriginalPos;
                LeanTween.moveLocal(settingsElements[i], originalPos, hoverDuration * 0.5f)
                    .setEase(hoverEaseType);
            }
        }

        // Highlight current setting
        if (currentSettingsIndex >= 0 && currentSettingsIndex < settingsElements.Length && 
            settingsElements[currentSettingsIndex] != null)
        {
            Vector3 originalPos = (currentSettingsIndex == 0) ? resolutionOriginalPos : volumeOriginalPos;
            Vector3 highlightPos = originalPos + Vector3.right * hoverMoveDistance;
            
            LeanTween.cancel(settingsElements[currentSettingsIndex]);
            LeanTween.moveLocal(settingsElements[currentSettingsIndex], highlightPos, hoverDuration)
                .setEase(hoverEaseType);
        }
    }

    /// <summary>
    /// Animate setting change with a bounce effect
    /// </summary>
    void AnimateSettingChange(GameObject settingObject)
    {
        if (settingObject == null) return;

        // Small bounce animation to show value change
        LeanTween.cancel(settingObject);
        Vector3 currentPos = settingObject.transform.localPosition;
        Vector3 bouncePos = currentPos + Vector3.up * 10f;

        // Bounce up
        LeanTween.moveLocal(settingObject, bouncePos, 0.1f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() => {
                // Bounce back down
                LeanTween.moveLocal(settingObject, currentPos, 0.1f)
                    .setEase(LeanTweenType.easeInQuad);
            });
    }

    /// <summary>
    /// Enable settings navigation mode
    /// </summary>
    void EnableSettingsNavigation()
    {
        if (!enableSettingsNavigation) return;

        isInOptionsMenu = true;
        currentSettingsIndex = 0;
        InitializeSettingsNavigation();
        HighlightCurrentSetting();
        LogDebug("Settings navigation enabled");
    }

    /// <summary>
    /// Disable settings navigation mode
    /// </summary>
    void DisableSettingsNavigation()
    {
        isInOptionsMenu = false;

        // Remove highlights from all settings
        if (settingsElements != null)
        {
            for (int i = 0; i < settingsElements.Length; i++)
            {
                if (settingsElements[i] != null)
                {
                    LeanTween.cancel(settingsElements[i]);
                    Vector3 originalPos = (i == 0) ? resolutionOriginalPos : volumeOriginalPos;
                    LeanTween.moveLocal(settingsElements[i], originalPos, hoverDuration * 0.5f)
                        .setEase(hoverEaseType);
                }
            }
        }

        LogDebug("Settings navigation disabled");
    }

    #endregion

    void InitializeResolutions()
    {
        // Get all available resolutions
        availableResolutions = Screen.resolutions;
        resolutionStrings = new string[availableResolutions.Length];
        
        // Convert resolutions to strings
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution res = availableResolutions[i];
            resolutionStrings[i] = $"{res.width} x {res.height}";
        }
        
        // Find current resolution index
        Resolution currentRes = Screen.currentResolution;
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == currentRes.width && 
                availableResolutions[i].height == currentRes.height)
            {
                currentResolutionIndex = i;
                break;
            }
        }
        
        // Update resolution display
        UpdateResolutionDisplay();
        
        LogDebug($"Found {availableResolutions.Length} available resolutions, current: {resolutionStrings[currentResolutionIndex]}");
    }

    void SetupVolumeScrollWheel()
    {
        if (volume != null)
        {
            // We need to add EventTrigger to the volume text's GameObject, not the text component itself
            GameObject volumeObject = volume.gameObject;
            
            // Add EventTrigger component if it doesn't exist
            EventTrigger volumeEventTrigger = volumeObject.GetComponent<EventTrigger>();
            if (volumeEventTrigger == null)
            {
                volumeEventTrigger = volumeObject.AddComponent<EventTrigger>();
            }

            // Add scroll event
            EventTrigger.Entry scrollEntry = new EventTrigger.Entry();
            scrollEntry.eventID = EventTriggerType.Scroll;
            scrollEntry.callback.AddListener((data) => {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnVolumeScroll(pointerData.scrollDelta.y);
                }
            });
            volumeEventTrigger.triggers.Add(scrollEntry);
            
            LogDebug("Volume scroll wheel setup complete");
        }
    }

    void SetupButtonEvents()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                Button buttonComponent = buttons[i].GetComponent<Button>();
                if (buttonComponent != null)
                {
                    // Remove any existing listeners
                    buttonComponent.onClick.RemoveAllListeners();
                    
                    // Add specific click events based on button type
                    if (buttons[i] == startButton)
                    {
                        buttonComponent.onClick.AddListener(() => OnStartButtonClick());
                    }
                    else if (buttons[i] == continueButton)
                    {
                        buttonComponent.onClick.AddListener(() => OnContinueButtonClick());
                    }
                    else if (buttons[i] == creditsButton)
                    {
                        buttonComponent.onClick.AddListener(() => ShowCredits());
                    }
                    else if (buttons[i] == optionsButton)
                    {
                        buttonComponent.onClick.AddListener(() => ShowOptions());
                    }
                    else
                    {
                        // For other buttons, use the generic submenu behavior
                        buttonComponent.onClick.AddListener(() => MoveToSubmenu());
                    }
                }

                // Add hover events
                EventTrigger eventTrigger = buttons[i].GetComponent<EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = buttons[i].AddComponent<EventTrigger>();
                }

                // Mouse enter event
                EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
                pointerEnter.eventID = EventTriggerType.PointerEnter;
                int buttonIndex = i; // Capture the index for the lambda
                pointerEnter.callback.AddListener((data) => { OnButtonHover(buttonIndex, true); });
                eventTrigger.triggers.Add(pointerEnter);

                // Mouse exit event
                EventTrigger.Entry pointerExit = new EventTrigger.Entry();
                pointerExit.eventID = EventTriggerType.PointerExit;
                pointerExit.callback.AddListener((data) => { OnButtonHover(buttonIndex, false); });
                eventTrigger.triggers.Add(pointerExit);
            }
        }

        // Setup back button event
        if (backButton != null)
        {
            Button backButtonComponent = backButton.GetComponent<Button>();
            if (backButtonComponent != null)
            {
                backButtonComponent.onClick.RemoveAllListeners();
                backButtonComponent.onClick.AddListener(() => ReturnToMainMenu());
            }

            // Add hover events for back button
            EventTrigger backEventTrigger = backButton.GetComponent<EventTrigger>();
            if (backEventTrigger == null)
            {
                backEventTrigger = backButton.AddComponent<EventTrigger>();
            }

            // Mouse enter event for back button
            EventTrigger.Entry backPointerEnter = new EventTrigger.Entry();
            backPointerEnter.eventID = EventTriggerType.PointerEnter;
            backPointerEnter.callback.AddListener((data) => { OnBackButtonHover(true); });
            backEventTrigger.triggers.Add(backPointerEnter);

            // Mouse exit event for back button
            EventTrigger.Entry backPointerExit = new EventTrigger.Entry();
            backPointerExit.eventID = EventTriggerType.PointerExit;
            backPointerExit.callback.AddListener((data) => { OnBackButtonHover(false); });
            backEventTrigger.triggers.Add(backPointerExit);
        }
    }

    public void ShowButtons()
    {
        if (isAnimating || buttonsVisible) return;
        
        isAnimating = true;
        buttonsVisible = true;

        // Counter to track completed animations
        int completedAnimations = 0;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                // Set initial position (off-screen to the left)
                Vector3 startPos = originalPositions[i] + Vector3.left * 1000f;
                buttons[i].transform.localPosition = startPos;
                buttons[i].SetActive(true);

                // Animate to original position with stair delay
                float delay = i * stairDelay;
                
                LeanTween.moveLocal(buttons[i], originalPositions[i], animationDuration)
                    .setDelay(delay)
                    .setEase(easeType)
                    .setOnComplete(() => {
                        completedAnimations++;
                        if (completedAnimations >= buttons.Length)
                        {
                            isAnimating = false;
                            // Enable keyboard navigation when buttons are fully shown
                            EnableKeyboardNavigation();
                        }
                    });

                // Add a slight scale animation for extra polish
                buttons[i].transform.localScale = Vector3.zero;
                LeanTween.scale(buttons[i], Vector3.one, animationDuration * 0.8f)
                    .setDelay(delay + 0.1f)
                    .setEase(LeanTweenType.easeOutBack);
            }
            else
            {
                // Count null buttons as completed immediately
                completedAnimations++;
            }
        }

        // If all buttons are null, complete immediately
        if (completedAnimations >= buttons.Length)
        {
            isAnimating = false;
            EnableKeyboardNavigation();
        }
    }

    public void HideButtons()
    {
        if (isAnimating || !buttonsVisible) return;
        
        isAnimating = true;
        buttonsVisible = false;

        // Disable keyboard navigation when hiding buttons
        DisableKeyboardNavigation();

        // Counter to track completed animations
        int completedAnimations = 0;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                // Calculate delay - start button exits first, then cascade
                float delay = i * stairDelay;
                Vector3 endPos = originalPositions[i] + Vector3.left * 1000f;

                // Move animation
                LeanTween.moveLocal(buttons[i], endPos, animationDuration)
                    .setDelay(delay)
                    .setEase(LeanTweenType.easeInBack)
                    .setOnComplete(() => {
                        buttons[i].SetActive(false);
                        completedAnimations++;
                        if (completedAnimations >= buttons.Length)
                        {
                            isAnimating = false;
                            // Automatically show buttons again after a short delay
                            StartCoroutine(RestartAnimation());
                        }
                    });

                // Scale down animation
                LeanTween.scale(buttons[i], Vector3.zero, animationDuration * 0.8f)
                    .setDelay(delay + 0.1f)
                    .setEase(LeanTweenType.easeInBack);
            }
            else
            {
                // Count null buttons as completed immediately
                completedAnimations++;
            }
        }

        // If all buttons are null, complete immediately
        if (completedAnimations >= buttons.Length)
        {
            isAnimating = false;
            StartCoroutine(RestartAnimation());
        }
    }

    private IEnumerator RestartAnimation()
    {
        yield return new WaitForSeconds(1f);
        ShowButtons();
    }

    void OnButtonHover(int buttonIndex, bool isHovering)
    {
        if (isAnimating || buttonIndex >= buttons.Length || buttons[buttonIndex] == null || !buttonsVisible) return;

        // Cancel any existing hover animation for this button
        LeanTween.cancel(buttons[buttonIndex]);

        // Calculate target position - only change X position
        Vector3 currentPos = buttons[buttonIndex].transform.localPosition;
        Vector3 targetPos = isHovering ? 
            new Vector3(originalPositions[buttonIndex].x + hoverMoveDistance, currentPos.y, currentPos.z) : 
            new Vector3(originalPositions[buttonIndex].x, currentPos.y, currentPos.z);

        LeanTween.moveLocal(buttons[buttonIndex], targetPos, hoverDuration)
            .setEase(hoverEaseType);
    }

    void OnBackButtonHover(bool isHovering)
    {
        if (isAnimating || backButton == null) return;

        // Cancel any existing hover animation for back button
        LeanTween.cancel(backButton);

        // Get current position and calculate target
        Vector3 currentPos = backButton.transform.localPosition;
        Vector3 targetPos = isHovering ? 
            new Vector3(currentPos.x + hoverMoveDistance, currentPos.y, currentPos.z) : 
            new Vector3(50f, currentPos.y, currentPos.z); // Return to x=50

        LeanTween.moveLocal(backButton, targetPos, hoverDuration)
            .setEase(hoverEaseType);
    }

    public void ShowCredits()
    {
        if (isAnimating || creditsVisible) return;
        
        MoveToSubmenu(); // First move main buttons and show back button
        
        // Set submenu state
        isInSubmenu = true;
        isInCreditsMenu = true;
        
        // Show credits sprite animation
        if (creditsSprite != null)
        {
            creditsVisible = true;
            creditsSprite.SetActive(true);
            
            // Start from right side (x = 1000) and animate to x = 450
            Vector3 startPos = new Vector3(1000f, creditsSprite.transform.localPosition.y, creditsSprite.transform.localPosition.z);
            Vector3 targetPos = new Vector3(450f, 0, 0);
            
            creditsSprite.transform.localPosition = startPos;
            
            // Position animation
            LeanTween.moveLocal(creditsSprite, targetPos, spriteAnimationDuration)
                .setDelay(spriteDelayOffset) // Delay to let main menu buttons move first
                .setEase(spriteEnterEase);
            
            // Scale animation
            if (useScaleAnimation)
            {
                creditsSprite.transform.localScale = Vector3.zero;
                LeanTween.scale(creditsSprite, Vector3.one, scaleAnimationDuration)
                    .setDelay(spriteDelayOffset + 0.1f)
                    .setEase(LeanTweenType.easeOutBack);
            }
            
            // Fade animation
            if (useFadeAnimation)
            {
                CanvasGroup creditsCanvasGroup = creditsSprite.GetComponent<CanvasGroup>();
                if (creditsCanvasGroup != null)
                {
                    creditsCanvasGroup.alpha = 0f;
                    LeanTween.alphaCanvas(creditsCanvasGroup, 1f, fadeAnimationDuration)
                        .setDelay(spriteDelayOffset + 0.2f)
                        .setEase(LeanTweenType.easeOutQuad);
                }
            }
        }
    }

    public void ShowOptions()
    {
        if (isAnimating || optionsVisible) return;
        
        MoveToSubmenu(); // First move main buttons and show back button
        
        // Set submenu state
        isInSubmenu = true;
        isInCreditsMenu = false;
        
        // Enable settings navigation if keyboard navigation is active
        if (enableKeyboardNavigation)
        {
            EnableSettingsNavigation();
        }
        
        // Show options sprite animation
        if (optionsSprite != null)
        {
            optionsVisible = true;
            optionsSprite.SetActive(true);
            
            // Start from right side (x = 3000) and animate to x = 540
            Vector3 startPos = new Vector3(3000f, optionsSprite.transform.localPosition.y, optionsSprite.transform.localPosition.z);
            Vector3 targetPos = new Vector3(540f, optionsSprite.transform.localPosition.y, optionsSprite.transform.localPosition.z);
            
            optionsSprite.transform.localPosition = startPos;
            
            // Position animation
            LeanTween.moveLocal(optionsSprite, targetPos, spriteAnimationDuration)
                .setDelay(spriteDelayOffset) // Delay to let main menu buttons move first
                .setEase(spriteEnterEase);
            
            // Scale animation
            if (useScaleAnimation)
            {
                optionsSprite.transform.localScale = Vector3.zero;
                LeanTween.scale(optionsSprite, Vector3.one, scaleAnimationDuration)
                    .setDelay(spriteDelayOffset + 0.1f)
                    .setEase(LeanTweenType.easeOutBack);
            }
            
            // Fade animation
            if (useFadeAnimation)
            {
                CanvasGroup optionsCanvasGroup = optionsSprite.GetComponent<CanvasGroup>();
                if (optionsCanvasGroup != null)
                {
                    optionsCanvasGroup.alpha = 0f;
                    LeanTween.alphaCanvas(optionsCanvasGroup, 1f, fadeAnimationDuration)
                        .setDelay(spriteDelayOffset + 0.2f)
                        .setEase(LeanTweenType.easeOutQuad);
                }
            }
        }
    }

    public void HideCredits()
    {
        if (!creditsVisible || creditsSprite == null) return;
        
        creditsVisible = false;
        
        // Clear submenu state
        isInSubmenu = false;
        isInCreditsMenu = false;
        
        // Animate credits sprite back to x = 1000 (right side)
        Vector3 exitPos = new Vector3(1000f, creditsSprite.transform.localPosition.y, creditsSprite.transform.localPosition.z);
        
        // Position animation
        LeanTween.moveLocal(creditsSprite, exitPos, spriteAnimationDuration)
            .setEase(spriteExitEase)
            .setOnComplete(() => {
                creditsSprite.SetActive(false);
                // Reset transforms for next time
                if (useScaleAnimation)
                {
                    creditsSprite.transform.localScale = Vector3.zero;
                }
                if (useFadeAnimation)
                {
                    CanvasGroup creditsCanvasGroup = creditsSprite.GetComponent<CanvasGroup>();
                    if (creditsCanvasGroup != null)
                    {
                        creditsCanvasGroup.alpha = 0f;
                    }
                }
            });
        
        // Scale animation
        if (useScaleAnimation)
        {
            LeanTween.scale(creditsSprite, Vector3.zero, scaleAnimationDuration * 0.8f)
                .setDelay(0.1f)
                .setEase(LeanTweenType.easeInBack);
        }
        
        // Fade animation
        if (useFadeAnimation)
        {
            CanvasGroup creditsCanvasGroup = creditsSprite.GetComponent<CanvasGroup>();
            if (creditsCanvasGroup != null)
            {
                LeanTween.alphaCanvas(creditsCanvasGroup, 0f, fadeAnimationDuration * 0.7f)
                    .setEase(LeanTweenType.easeInQuad);
            }
        }
    }

    public void HideOptions()
    {
        if (!optionsVisible || optionsSprite == null) return;
        
        optionsVisible = false;
        
        // Clear submenu state
        isInSubmenu = false;
        isInCreditsMenu = false;
        
        // Disable settings navigation
        DisableSettingsNavigation();
        
        // Animate options sprite back to x = 3000 (right side)
        Vector3 exitPos = new Vector3(3000f, optionsSprite.transform.localPosition.y, optionsSprite.transform.localPosition.z);
        
        // Position animation
        LeanTween.moveLocal(optionsSprite, exitPos, spriteAnimationDuration)
            .setEase(spriteExitEase)
            .setOnComplete(() => {
                optionsSprite.SetActive(false);
                // Reset transforms for next time
                if (useScaleAnimation)
                {
                    optionsSprite.transform.localScale = Vector3.zero;
                }
                if (useFadeAnimation)
                {
                    CanvasGroup optionsCanvasGroup = optionsSprite.GetComponent<CanvasGroup>();
                    if (optionsCanvasGroup != null)
                    {
                        optionsCanvasGroup.alpha = 0f;
                    }
                }
            });
        
        // Scale animation
        if (useScaleAnimation)
        {
            LeanTween.scale(optionsSprite, Vector3.zero, scaleAnimationDuration * 0.8f)
                .setDelay(0.1f)
                .setEase(LeanTweenType.easeInBack);
        }
        
        // Fade animation
        if (useFadeAnimation)
        {
            CanvasGroup optionsCanvasGroup = optionsSprite.GetComponent<CanvasGroup>();
            if (optionsCanvasGroup != null)
            {
                LeanTween.alphaCanvas(optionsCanvasGroup, 0f, fadeAnimationDuration * 0.7f)
                    .setEase(LeanTweenType.easeInQuad);
            }
        }
    }

    public void MoveToSubmenu()
    {
        if (isAnimating) return;
        
        isAnimating = true;
        buttonsVisible = false; // Set buttons as not visible in main menu state

        // Disable keyboard navigation when moving to submenu
        DisableKeyboardNavigation();

        // Move all main buttons to x -900
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                Vector3 targetPos = new Vector3(-900f, originalPositions[i].y, originalPositions[i].z);

                LeanTween.moveLocal(buttons[i], targetPos, animationDuration)
                    .setEase(easeType);
            }
        }

        // Show and animate back button from left side to x 50
        if (backButton != null)
        {
            backButton.SetActive(true);
            
            // Start back button off-screen to the left
            Vector3 backStartPos = new Vector3(-1000f, backButton.transform.localPosition.y, backButton.transform.localPosition.z);
            backButton.transform.localPosition = backStartPos;

            // Animate back button to x=50
            Vector3 backTargetPos = new Vector3(50f, backButton.transform.localPosition.y, backButton.transform.localPosition.z);
            LeanTween.moveLocal(backButton, backTargetPos, animationDuration)
                .setEase(easeType)
                .setDelay(0.2f) // Small delay to let main buttons start moving first
                .setOnComplete(() => {
                    isAnimating = false;
                });
        }
        else
        {
            isAnimating = false;
        }
    }

    public void ReturnToMainMenu()
    {
        if (isAnimating) return;
        
        isAnimating = true;

        // Hide any visible sprites first
        if (creditsVisible)
        {
            HideCredits();
        }
        if (optionsVisible)
        {
            HideOptions();
        }

        // Hide back button by moving it off-screen to the left
        if (backButton != null)
        {
            Vector3 backExitPos = new Vector3(-1000f, backButton.transform.localPosition.y, backButton.transform.localPosition.z);
            LeanTween.moveLocal(backButton, backExitPos, animationDuration)
                .setEase(easeType)
                .setOnComplete(() => {
                    backButton.SetActive(false);
                });
        }

        // Counter to track completed animations
        int completedAnimations = 0;

        // Return all main buttons to their original positions
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                LeanTween.moveLocal(buttons[i], originalPositions[i], animationDuration)
                    .setEase(easeType)
                    .setDelay(0.1f) // Small delay to let back button start exiting first
                    .setOnComplete(() => {
                        completedAnimations++;
                        if (completedAnimations >= buttons.Length)
                        {
                            isAnimating = false;
                            buttonsVisible = true; // Reset the buttons visible state
                            // Re-enable keyboard navigation when returning to main menu
                            EnableKeyboardNavigation();
                        }
                    });
            }
            else
            {
                // Count null buttons as completed immediately
                completedAnimations++;
            }
        }

        // If all buttons are null, complete immediately
        if (completedAnimations >= buttons.Length)
        {
            isAnimating = false;
            buttonsVisible = true;
            EnableKeyboardNavigation();
        }
    }

    // Public methods to manually trigger animations
    public void TriggerShowAnimation()
    {
        ShowButtons();
    }

    public void TriggerMoveToSubmenu()
    {
        MoveToSubmenu();
    }

    public void TriggerReturnToMainMenu()
    {
        ReturnToMainMenu();
    }

    public void TriggerHideAnimation()
    {
        HideButtons();
    }

    // Method to reset all animations
    public void ResetAnimations()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                LeanTween.cancel(buttons[i]);
                buttons[i].transform.localPosition = originalPositions[i];
                buttons[i].transform.localScale = Vector3.one;
                buttons[i].SetActive(true);
            }
        }
        
        // Reset sprites
        if (creditsSprite != null)
        {
            LeanTween.cancel(creditsSprite);
            creditsSprite.transform.localPosition = creditsOffScreenPos;
            creditsSprite.SetActive(false);
            creditsVisible = false;
            
            // Reset scale and alpha
            if (useScaleAnimation)
            {
                creditsSprite.transform.localScale = Vector3.zero;
            }
            if (useFadeAnimation)
            {
                CanvasGroup creditsCanvasGroup = creditsSprite.GetComponent<CanvasGroup>();
                if (creditsCanvasGroup != null)
                {
                    creditsCanvasGroup.alpha = 0f;
                }
            }
        }
        
        if (optionsSprite != null)
        {
            LeanTween.cancel(optionsSprite);
            optionsSprite.transform.localPosition = optionsOffScreenPos;
            optionsSprite.SetActive(false);
            optionsVisible = false;
            
            // Reset scale and alpha
            if (useScaleAnimation)
            {
                optionsSprite.transform.localScale = Vector3.zero;
            }
            if (useFadeAnimation)
            {
                CanvasGroup optionsCanvasGroup = optionsSprite.GetComponent<CanvasGroup>();
                if (optionsCanvasGroup != null)
                {
                    optionsCanvasGroup.alpha = 0f;
                }
            }
        }
        
        if (backButton != null)
        {
            LeanTween.cancel(backButton);
            backButton.SetActive(false);
        }
        
        // Reset logo animation
        if (gameLogo != null)
        {
            StopLogoAnimation();
            if (enableLogoAnimation)
            {
                // Restart logo animation
                InitializeLogoAnimation();
            }
        }
        
        isAnimating = false;
        buttonsVisible = true;
    }

    /// <summary>
    /// Check for save file and update continue button state accordingly
    /// </summary>
    private void CheckSaveFileAndUpdateContinueButton()
    {
        if (continueButton == null)
        {
            LogDebug("Continue button is not assigned!");
            return;
        }

        try
        {
            string saveFilePath = GetSaveFilePath();
            bool hasSaveFile = false;
            
            if (File.Exists(saveFilePath))
            {
                LogDebug($"Save file found at: {saveFilePath}");
                
                // Read and parse the JSON file
                string jsonContent = File.ReadAllText(saveFilePath);
                SaveData saveData = ParseSaveDataFromJSON(jsonContent);
                
                if (saveData != null)
                {
                    LogDebug($"Save data loaded - Day: {saveData.day}, Mother Stress: {saveData.mother_stress_level}, Time of Day: {saveData.timeOfDay}");
                    
                    // Check if save data has meaningful progress (day > 0 or mother_stress_level > 0)
                    if (saveData.day > 0 || saveData.mother_stress_level > 0)
                    {
                        hasSaveFile = true;
                        LogDebug("Valid save data found - enabling continue button");
                    }
                    else
                    {
                        LogDebug("Save data has no progress - disabling continue button");
                    }
                }
                else
                {
                    LogDebug("Failed to parse save data - disabling continue button");
                }
            }
            else
            {
                LogDebug($"No save file found at: {saveFilePath} - disabling continue button");
            }
            
            // Update continue button state
            UpdateContinueButtonState(hasSaveFile);
        }
        catch (System.Exception e)
        {
            LogDebug($"Error checking save file: {e.Message} - disabling continue button");
            UpdateContinueButtonState(false);
        }
    }

    /// <summary>
    /// Update the continue button's interactivity and visual state
    /// </summary>
    private void UpdateContinueButtonState(bool isEnabled)
    {
        if (continueButton == null) return;

        // Get button component
        Button buttonComponent = continueButton.GetComponent<Button>();
        if (buttonComponent != null)
        {
            buttonComponent.interactable = isEnabled;
        }

        // Get CanvasGroup for opacity control
        CanvasGroup canvasGroup = continueButton.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = continueButton.AddComponent<CanvasGroup>();
        }

        // Set opacity based on state
        canvasGroup.alpha = isEnabled ? 1f : 0.5f;

        LogDebug($"Continue button state updated - Enabled: {isEnabled}, Opacity: {canvasGroup.alpha}");
    }

    /// <summary>
    /// Get the full path to the save file in MyGames/Rey/saves
    /// </summary>
    private string GetSaveFilePath()
    {
        // Get the user's Documents folder
        string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        
        // Build the path: Documents/My Games/Rey/saves/save_data.json
        string saveFilePath = Path.Combine(documentsPath, "My Games", "Rey", "saves", "save_data.json");
        
        return saveFilePath;
    }

    /// <summary>
    /// Public method to manually refresh the continue button state
    /// </summary>
    [ContextMenu("Refresh Continue Button State")]
    public void RefreshContinueButtonState()
    {
        CheckSaveFileAndUpdateContinueButton();
    }

    /// <summary>
    /// Create a test save file for testing purposes
    /// </summary>
    [ContextMenu("Create Test Save File")]
    public void CreateTestSaveFile()
    {
        try
        {
            string saveFilePath = GetSaveFilePath();
            string directoryPath = Path.GetDirectoryName(saveFilePath);
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                LogDebug($"Created save directory: {directoryPath}");
            }
            
            // Create test save data
            SaveData testSave = new SaveData();
            testSave.day = 3;
            testSave.mother_stress_level = 5;
            testSave.timeOfDay = TimeOfDay.Night;
            
            string jsonContent = JsonUtility.ToJson(testSave, true);
            File.WriteAllText(saveFilePath, jsonContent);
            
            LogDebug($"Test save file created at: {saveFilePath}");
            LogDebug($"Content: {jsonContent}");
            
            // Refresh the continue button state
            CheckSaveFileAndUpdateContinueButton();
        }
        catch (System.Exception e)
        {
            LogDebug($"Error creating test save file: {e.Message}");
        }
    }

    /// <summary>
    /// Create an empty save file for testing
    /// </summary>
    [ContextMenu("Create Empty Save File")]
    public void CreateEmptySaveFile()
    {
        try
        {
            string saveFilePath = GetSaveFilePath();
            string directoryPath = Path.GetDirectoryName(saveFilePath);
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                LogDebug($"Created save directory: {directoryPath}");
            }
            
            // Create empty save data (day=0, mother_stress_level=0)
            SaveData emptySave = new SaveData();
            
            string jsonContent = JsonUtility.ToJson(emptySave, true);
            File.WriteAllText(saveFilePath, jsonContent);
            
            LogDebug($"Empty save file created at: {saveFilePath}");
            LogDebug($"Content: {jsonContent}");
            
            // Refresh the continue button state
            CheckSaveFileAndUpdateContinueButton();
        }
        catch (System.Exception e)
        {
            LogDebug($"Error creating empty save file: {e.Message}");
        }
    }

    /// <summary>
    /// Delete the save file for testing
    /// </summary>
    [ContextMenu("Delete Save File")]
    public void DeleteSaveFile()
    {
        try
        {
            string saveFilePath = GetSaveFilePath();
            
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                LogDebug($"Save file deleted: {saveFilePath}");
                
                // Refresh the continue button state
                CheckSaveFileAndUpdateContinueButton();
            }
            else
            {
                LogDebug($"No save file to delete at: {saveFilePath}");
            }
        }
        catch (System.Exception e)
        {
            LogDebug($"Error deleting save file: {e.Message}");
        }
    }

    /// <summary>
    /// Check current save file status and log information
    /// </summary>
    [ContextMenu("Check Save File Status")]
    public void CheckSaveFileStatus()
    {
        string saveFilePath = GetSaveFilePath();
        
        LogDebug($"Save file path: {saveFilePath}");
        
        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(saveFilePath);
                SaveData saveData = ParseSaveDataFromJSON(jsonContent);
                
                LogDebug("✓ Save file exists and is readable");
                LogDebug($"Content: {jsonContent}");
                
                if (saveData != null)
                {
                    LogDebug($"Parsed data - Day: {saveData.day}, Mother Stress: {saveData.mother_stress_level}, Time of Day: {saveData.timeOfDay}");
                    
                    if (saveData.day > 0 || saveData.mother_stress_level > 0)
                    {
                        LogDebug("→ Would show 'Continue' button");
                    }
                    else
                    {
                        LogDebug("→ Would show 'Start Game' button");
                    }
                }
                else
                {
                    LogDebug("✗ Failed to parse save data");
                }
            }
            catch (System.Exception e)
            {
                LogDebug($"✗ Error reading save file: {e.Message}");
            }
        }
        else
        {
            LogDebug("✗ Save file does not exist");
            LogDebug("→ Would show 'Start Game' button");
        }
    }

    /// <summary>
    /// Test JSON parsing with different enum formats
    /// </summary>
    [ContextMenu("Test JSON Parsing")]
    public void TestJSONParsing()
    {
        LogDebug("=== Testing JSON Parsing ===");
        
        // Test 1: String enum format (your case)
        string stringEnumJSON = "{\n    \"day\": 4,\n    \"timeOfDay\": \"Night\",\n    \"mother_stress_level\": 250\n}";
        LogDebug($"Testing string enum JSON:\n{stringEnumJSON}");
        
        SaveData stringResult = ParseSaveDataFromJSON(stringEnumJSON);
        if (stringResult != null)
        {
            LogDebug($"✓ String parsing result - Day: {stringResult.day}, Stress: {stringResult.mother_stress_level}, Time: {stringResult.timeOfDay}");
        }
        else
        {
            LogDebug("✗ String parsing failed");
        }
        
        // Test 2: Integer enum format
        string intEnumJSON = "{\n    \"day\": 4,\n    \"timeOfDay\": 2,\n    \"mother_stress_level\": 250\n}";
        LogDebug($"\nTesting integer enum JSON:\n{intEnumJSON}");
        
        SaveData intResult = ParseSaveDataFromJSON(intEnumJSON);
        if (intResult != null)
        {
            LogDebug($"✓ Integer parsing result - Day: {intResult.day}, Stress: {intResult.mother_stress_level}, Time: {intResult.timeOfDay}");
        }
        else
        {
            LogDebug("✗ Integer parsing failed");
        }
        
        LogDebug("=== JSON Parsing Test Complete ===");
    }

    /// <summary>
    /// Manually load save data into ScriptableObject for testing
    /// </summary>
    [ContextMenu("Load Save Data into ScriptableObject")]
    public void LoadSaveDataIntoScriptableObject()
    {
        LogDebug("=== Loading Save Data into ScriptableObject ===");
        
        if (targetScriptableObject == null)
        {
            LogDebug("✗ Target ScriptableObject is not assigned!");
            return;
        }
        
        string saveFilePath = GetSaveFilePath();
        
        if (!File.Exists(saveFilePath))
        {
            LogDebug($"✗ No save file found at: {saveFilePath}");
            return;
        }
        
        try
        {
            string jsonContent = File.ReadAllText(saveFilePath);
            SaveData saveData = ParseSaveDataFromJSON(jsonContent);
            
            if (saveData != null)
            {
                LogDebug($"Before loading - ScriptableObject: Day={targetScriptableObject.day}, Stress={targetScriptableObject.mother_stress_level}, Time={targetScriptableObject.timeOfDay}");
                
                // Load save data into ScriptableObject
                targetScriptableObject.day = saveData.day;
                targetScriptableObject.mother_stress_level = saveData.mother_stress_level;
                targetScriptableObject.timeOfDay = saveData.timeOfDay;
                
                LogDebug($"After loading - ScriptableObject: Day={targetScriptableObject.day}, Stress={targetScriptableObject.mother_stress_level}, Time={targetScriptableObject.timeOfDay}");
                
                // Mark as dirty for Unity to save changes in editor
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(targetScriptableObject);
                #endif
                
                LogDebug("✓ Save data loaded into ScriptableObject successfully!");
            }
            else
            {
                LogDebug("✗ Failed to parse save data");
            }
        }
        catch (System.Exception e)
        {
            LogDebug($"✗ Error loading save data: {e.Message}");
        }
        
        LogDebug("=== Load Save Data Complete ===");
    }

    /// <summary>
    /// Debug ScriptableObject state and assignment
    /// </summary>
    private void DebugScriptableObjectState()
    {
        LogDebug("=== ScriptableObject Debug Info ===");
        
        if (targetScriptableObject == null)
        {
            LogDebug("✗ ERROR: targetScriptableObject is NOT assigned in Inspector!");
            LogDebug("→ Please assign the CoreGameSaves ScriptableObject in the MainMenuManager Inspector");
            return;
        }
        
        LogDebug($"✓ targetScriptableObject is assigned: {targetScriptableObject.name}");
        LogDebug($"Current ScriptableObject values:");
        LogDebug($"  - Day: {targetScriptableObject.day}");
        LogDebug($"  - Mother Stress: {targetScriptableObject.mother_stress_level}");
        LogDebug($"  - Time of Day: {targetScriptableObject.timeOfDay}");
        
        // Check if save file exists and could be loaded
        string saveFilePath = GetSaveFilePath();
        if (File.Exists(saveFilePath))
        {
            LogDebug($"✓ Save file exists at: {saveFilePath}");
            LogDebug("→ You can use the context menu 'Load Save Data into ScriptableObject' to load it");
            LogDebug("→ Or click the Continue button in-game to load it automatically");
        }
        else
        {
            LogDebug($"✗ No save file found at: {saveFilePath}");
        }
        
        LogDebug("=== ScriptableObject Debug Complete ===");
    }

    /// <summary>
    /// Automatically load save data on start (for testing)
    /// </summary>
    [ContextMenu("Auto Load Save Data on Start")]
    public void AutoLoadSaveDataOnStart()
    {
        LogDebug("=== Auto Loading Save Data ===");
        LoadSaveDataIntoScriptableObject();
    }

    /// <summary>
    /// Helper method for debug logging
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[MainMenuManager] {message}");
        }
    }

    #region Settings Control Methods

    /// <summary>
    /// Update the resolution display text
    /// </summary>
    private void UpdateResolutionDisplay()
    {
        if (resolution != null && resolutionStrings != null && currentResolutionIndex >= 0 && currentResolutionIndex < resolutionStrings.Length)
        {
            resolution.text = $"< {resolutionStrings[currentResolutionIndex]} >";
        }
    }

    /// <summary>
    /// Update the volume display text
    /// </summary>
    private void UpdateVolumeDisplay()
    {
        if (volume != null)
        {
            volume.text = $"< {currentVolumeLevel} >";
        }
    }

    /// <summary>
    /// Handle volume scroll wheel input
    /// </summary>
    private void OnVolumeScroll(float scrollDelta)
    {
        if (scrollDelta > 0)
        {
            IncreaseVolume();
        }
        else if (scrollDelta < 0)
        {
            DecreaseVolume();
        }
    }

    /// <summary>
    /// Handle resolution scroll wheel input
    /// </summary>
    private void OnResolutionScroll(float scrollDelta)
    {
        if (scrollDelta > 0)
        {
            NextResolution();
        }
        else if (scrollDelta < 0)
        {
            PreviousResolution();
        }
    }

    /// <summary>
    /// Cycle to the next resolution
    /// </summary>
    public void NextResolution()
    {
        if (availableResolutions != null && availableResolutions.Length > 0)
        {
            currentResolutionIndex = (currentResolutionIndex + 1) % availableResolutions.Length;
            UpdateResolutionDisplay();
            ApplyResolution();
            LogDebug($"Resolution changed to: {resolutionStrings[currentResolutionIndex]}");
        }
    }

    /// <summary>
    /// Cycle to the previous resolution
    /// </summary>
    public void PreviousResolution()
    {
        if (availableResolutions != null && availableResolutions.Length > 0)
        {
            currentResolutionIndex--;
            if (currentResolutionIndex < 0)
            {
                currentResolutionIndex = availableResolutions.Length - 1;
            }
            UpdateResolutionDisplay();
            ApplyResolution();
            LogDebug($"Resolution changed to: {resolutionStrings[currentResolutionIndex]}");
        }
    }

    /// <summary>
    /// Apply the currently selected resolution
    /// </summary>
    private void ApplyResolution()
    {
        if (availableResolutions != null && currentResolutionIndex >= 0 && currentResolutionIndex < availableResolutions.Length)
        {
            Resolution selectedResolution = availableResolutions[currentResolutionIndex];
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
            LogDebug($"Applied resolution: {selectedResolution.width}x{selectedResolution.height}");
        }
    }

    /// <summary>
    /// Increase volume level
    /// </summary>
    public void IncreaseVolume()
    {
        currentVolumeLevel = Mathf.Clamp(currentVolumeLevel + volumeStep, volumeMin, volumeMax);
        UpdateVolumeDisplay();
        ApplyVolume();
        LogDebug($"Volume increased to: {currentVolumeLevel}");
    }

    /// <summary>
    /// Decrease volume level
    /// </summary>
    public void DecreaseVolume()
    {
        currentVolumeLevel = Mathf.Clamp(currentVolumeLevel - volumeStep, volumeMin, volumeMax);
        UpdateVolumeDisplay();
        ApplyVolume();
        LogDebug($"Volume decreased to: {currentVolumeLevel}");
    }

    /// <summary>
    /// Apply the current volume level to AudioListener
    /// </summary>
    private void ApplyVolume()
    {
        // Convert 0-100 range to 0-1 range for AudioListener
        float normalizedVolume = currentVolumeLevel / 100f;
        AudioListener.volume = normalizedVolume;
        LogDebug($"Applied volume: {normalizedVolume:F2} (AudioListener.volume)");
    }

    /// <summary>
    /// Add click detection and scroll wheel support to resolution text
    /// </summary>
    public void SetupResolutionClickDetection()
    {
        if (resolution != null)
        {
            GameObject resolutionObject = resolution.gameObject;
            
            // Add EventTrigger if it doesn't exist
            EventTrigger resolutionEventTrigger = resolutionObject.GetComponent<EventTrigger>();
            if (resolutionEventTrigger == null)
            {
                resolutionEventTrigger = resolutionObject.AddComponent<EventTrigger>();
            }

            // Add click event
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnResolutionClick(pointerData);
                }
            });
            resolutionEventTrigger.triggers.Add(clickEntry);

            // Add scroll event for resolution
            EventTrigger.Entry scrollEntry = new EventTrigger.Entry();
            scrollEntry.eventID = EventTriggerType.Scroll;
            scrollEntry.callback.AddListener((data) => {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnResolutionScroll(pointerData.scrollDelta.y);
                }
            });
            resolutionEventTrigger.triggers.Add(scrollEntry);
            
            LogDebug("Resolution click detection and scroll wheel setup complete");
        }
    }

    /// <summary>
    /// Add click detection to volume text for arrow simulation
    /// </summary>
    public void SetupVolumeClickDetection()
    {
        if (volume != null)
        {
            GameObject volumeObject = volume.gameObject;
            
            // Add EventTrigger if it doesn't exist
            EventTrigger volumeEventTrigger = volumeObject.GetComponent<EventTrigger>();
            if (volumeEventTrigger == null)
            {
                volumeEventTrigger = volumeObject.AddComponent<EventTrigger>();
            }

            // Add click event
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => {
                PointerEventData pointerData = data as PointerEventData;
                if (pointerData != null)
                {
                    OnVolumeClick(pointerData);
                }
            });
            volumeEventTrigger.triggers.Add(clickEntry);
            
            LogDebug("Volume click detection setup complete");
        }
    }

    /// <summary>
    /// Handle resolution text clicks to simulate arrow buttons
    /// </summary>
    private void OnResolutionClick(PointerEventData pointerData)
    { 
        if (resolution == null) return;

        // Get the local position of the click relative to the text
        Vector2 localPoint;
        RectTransform rectTransform = resolution.rectTransform;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, pointerData.position, pointerData.pressEventCamera, out localPoint))
        {
            // Calculate if click was on left half or right half of the entire text
            float normalizedX = (localPoint.x + rectTransform.rect.width * 0.5f) / rectTransform.rect.width;
            
            if (normalizedX < 0.5f) // Left half = Previous
            {
                PreviousResolution();
            }
            else // Right half = Next
            {
                NextResolution();
            }
            
            LogDebug($"Resolution click at normalized X: {normalizedX:F2}");
        }
    }

    /// <summary>
    /// Handle volume text clicks to simulate arrow buttons
    /// </summary>
    private void OnVolumeClick(PointerEventData pointerData)
    {
        if (volume == null) return;

        // Get the local position of the click relative to the text
        Vector2 localPoint;
        RectTransform rectTransform = volume.rectTransform;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, pointerData.position, pointerData.pressEventCamera, out localPoint))
        {
            // Calculate if click was on left half or right half of the entire text
            float normalizedX = (localPoint.x + rectTransform.rect.width * 0.5f) / rectTransform.rect.width;
            
            if (normalizedX < 0.5f) // Left half = Decrease
            {
                DecreaseVolume();
            }
            else // Right half = Increase
            {
                IncreaseVolume();
            }
            
            LogDebug($"Volume click at normalized X: {normalizedX:F2}");
        }
    }

    /// <summary>
    /// Setup all interactive controls for settings
    /// </summary>
    [ContextMenu("Setup Settings Controls")]
    public void SetupSettingsControls()
    {
        SetupResolutionClickDetection();
        SetupVolumeClickDetection();
        LogDebug("All settings controls setup complete");
    }

    /// <summary>
    /// Reset settings to default values
    /// </summary>
    [ContextMenu("Reset Settings to Default")]
    public void ResetSettingsToDefault()
    {
        // Reset volume to default
        currentVolumeLevel = 60;
        UpdateVolumeDisplay();
        ApplyVolume();
        
        // Reset resolution to current screen resolution
        InitializeResolutions();
        
        LogDebug("Settings reset to default values");
    }

    #endregion

    public void onExitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// Handle start button click - resets ScriptableObject data if save file exists
    /// </summary>
    public void OnStartButtonClick()
    {
        LogDebug("Start button clicked");
        
        // Check if save data exists
        string saveFilePath = GetSaveFilePath();
        bool hasSaveData = false;
        
        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(saveFilePath);
                SaveData saveData = ParseSaveDataFromJSON(jsonContent);
                
                if (saveData != null && (saveData.day > 0 || saveData.mother_stress_level > 0))
                {
                    hasSaveData = true;
                    LogDebug("Save data found, will reset ScriptableObject to start fresh");
                }
            }
            catch (System.Exception e)
            {
                LogDebug($"Error reading save data: {e.Message}");
            }
        }
        
        // Reset ScriptableObject data if save data exists (to start fresh)
        if (hasSaveData && targetScriptableObject != null)
        {
            ResetScriptableObjectData();
        }
        else if (targetScriptableObject != null)
        {
            LogDebug("No save data found or already at default values");
        }
        else
        {
            LogDebug("Warning: targetScriptableObject is not assigned!");
        }
        
        // Continue with normal start game flow
        StartGame();
    }

    /// <summary>
    /// Reset the CoreGameSaves ScriptableObject to default values
    /// </summary>
    private void ResetScriptableObjectData()
    {
        if (targetScriptableObject == null)
        {
            LogDebug("Cannot reset ScriptableObject - targetScriptableObject is null!");
            return;
        }
        
        LogDebug($"Resetting ScriptableObject data - Before: Day={targetScriptableObject.day}, Stress={targetScriptableObject.mother_stress_level}, TimeOfDay={targetScriptableObject.timeOfDay}");
        
        // Reset all values as requested
        targetScriptableObject.day = 0;
        targetScriptableObject.timeOfDay = (TimeOfDay)3; // Index 3 = Night
        targetScriptableObject.mother_stress_level = 0;
        
        // Mark as dirty for Unity to save changes in editor
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(targetScriptableObject);
        #endif
        
        LogDebug($"ScriptableObject data reset - After: Day={targetScriptableObject.day}, TimeOfDay={targetScriptableObject.timeOfDay}, Stress={targetScriptableObject.mother_stress_level}");
    }

    /// <summary>
    /// Start the game (placeholder method - implement your game start logic here)
    /// </summary>
    private void StartGame()
    {
        LogDebug("Starting game...");
        // TODO: Add your game start logic here
        // For example: SceneManager.LoadScene("GameScene");
        
        // For now, just move to submenu to show the button worked
        MoveToSubmenu();
    }

    /// <summary>
    /// Context menu method to test start button behavior
    /// </summary>
    [ContextMenu("Test Start Button Click")]
    public void TestStartButtonClick()
    {
        OnStartButtonClick();
    }

    /// <summary>
    /// Handle continue button click - loads save data into ScriptableObject
    /// </summary>
    public void OnContinueButtonClick()
    {
        LogDebug("Continue button clicked");
        
        string saveFilePath = GetSaveFilePath();
        
        if (!File.Exists(saveFilePath))
        {
            LogDebug("No save file found for continue - this should not happen!");
            return;
        }
        
        try
        {
            string jsonContent = File.ReadAllText(saveFilePath);
            SaveData saveData = ParseSaveDataFromJSON(jsonContent);
            
            if (saveData != null && targetScriptableObject != null)
            {
                LogDebug($"Loading save data into ScriptableObject - Day: {saveData.day}, Stress: {saveData.mother_stress_level}, Time of Day: {saveData.timeOfDay}");
                
                // Load save data into ScriptableObject
                targetScriptableObject.day = saveData.day;
                targetScriptableObject.mother_stress_level = saveData.mother_stress_level;
                targetScriptableObject.timeOfDay = saveData.timeOfDay;
                
                // Mark as dirty for Unity to save changes in editor
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(targetScriptableObject);
                #endif
                
                LogDebug("Save data loaded into ScriptableObject successfully");
                
                // Continue the game with loaded data
                ContinueGame();
            }
            else
            {
                LogDebug("Failed to parse save data or targetScriptableObject is null!");
            }
        }
        catch (System.Exception e)
        {
            LogDebug($"Error loading save data: {e.Message}");
        }
    }

    /// <summary>
    /// Continue the game with loaded save data (placeholder method)
    /// </summary>
    private void ContinueGame()
    {
        LogDebug("Continuing game with loaded save data...");
        // TODO: Add your game continue logic here
        // For example: SceneManager.LoadScene("GameScene");
        
        // For now, just move to submenu to show the button worked
        MoveToSubmenu();
    }

    /// <summary>
    /// Context menu method to test continue button behavior
    /// </summary>
    [ContextMenu("Test Continue Button Click")]
    public void TestContinueButtonClick()
    {
        OnContinueButtonClick();
    }

    /// <summary>
    /// Show current ScriptableObject values for debugging
    /// </summary>
    [ContextMenu("Show ScriptableObject Values")]
    public void ShowScriptableObjectValues()
    {
        if (targetScriptableObject != null)
        {
            LogDebug($"Current ScriptableObject values - Day: {targetScriptableObject.day}, Mother Stress Level: {targetScriptableObject.mother_stress_level}");
        }
        else
        {
            LogDebug("targetScriptableObject is not assigned!");
        }
    }

    /// <summary>
    /// Manually reset ScriptableObject values (for testing)
    /// </summary>
    [ContextMenu("Reset ScriptableObject Values")]
    public void ManualResetScriptableObject()
    {
        ResetScriptableObjectData();
    }

    public void StartGameNew()
    {
        // Reset ScriptableObject data
        if (targetScriptableObject != null)
        {
            targetScriptableObject.day = 0;
            targetScriptableObject.timeOfDay = TimeOfDay.Night; // Reset to morning
            targetScriptableObject.mother_stress_level = 0;
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(targetScriptableObject);
            #endif
        }

        // Load the next scene (replace "GameScene" with your actual scene name)
        UnityEngine.SceneManagement.SceneManager.LoadScene("Builder House");
    }

    public void StartGameContinue()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Builder House");
    }

    void OnDestroy()
    {
        // Clean up any running tweens
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                LeanTween.cancel(buttons[i]);
            }
        }
        
        // Clean up sprite tweens
        if (creditsSprite != null)
        {
            LeanTween.cancel(creditsSprite);
        }
        
        if (optionsSprite != null)
        {
            LeanTween.cancel(optionsSprite);
        }
        
        if (backButton != null)
        {
            LeanTween.cancel(backButton);
        }
    }
}
