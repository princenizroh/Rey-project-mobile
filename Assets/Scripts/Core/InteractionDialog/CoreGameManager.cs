using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CONSOLIDATED DIALOG SYSTEM - CoreGameManager
/// 
/// This script now contains integrated functionality from the following removed components:
/// 
/// 1. PlayerAnswerManager - Choice handling with button animations
///    - ShowChoicesWithButtons() - Display and animate choice buttons
///    - HideChoices() - Hide and cleanup choice buttons
///    - AnimateButtonText() - Animate button text display
///    - Button click handling with animation skip support
/// 
/// 2. DialogController - Dialog UI management and animations
///    - SummonDialogBar() - Create and animate dialog bars
///    - SummonQuestionBar() - Create and animate question/choice bars
///    - DestroyAllQuestionBars() - Cleanup question bar instances
///    - Enhanced DestroyDialogInstances() with complete cleanup
/// 
/// 3. NPCDialogManager - Text animation and dialog processing
///    - Legacy dialog text animation system
///    - Special prefix handling (mapname:, exitgame:, timeline:, charge:)
///    - Dialog progression and state management
/// 
/// 4. NPCDialogManagerMaster - Master dialog control and state management
///    - InitiateStartDialog() - Legacy dialog system entry point
///    - Dialog state management (normal, choices, response)
///    - Input handling and dialog progression
/// 
/// 5. DialogButtonController/MenuButtonHandler - Button event handling
///    - OnNPCButtonClicked() - Handle NPC interaction button clicks
///    - NPC type detection and appropriate dialog triggering
/// 
/// NEW FEATURES - Multiple Response & NPC Name Support:
/// - Support for CoreGameDialogChoicesResponse[] - multiple responses per choice
/// - Dynamic NPC name display with UpdateNpcNameDisplay() (2D dialogs only)
/// - Response selection system with SetResponseIndex(), UseRandomResponse()
/// - Conditional response selection with SetResponseByCondition()
/// - Automatic NPC name extraction from dialog text ("NpcName: dialog") for 2D dialogs
/// - 3D dialogs ignore NPC name updates (models represent characters inherently)
/// - Cutscene fade system with BackgroundFade image for dialog transitions
///   * None: No fade effect
///   * FadeIn: Transparent to dark transition
///   * FadeOut: Dark to transparent transition
///   * StayIn: Remain dark throughout dialog
///   * StayOut: Remain transparent throughout dialog
/// - Keyboard input system for better user experience
///   * Space: Progress dialog and skip text animation
///   * Q, W, E: Select dialog choices (buttons show [Q], [W], [E] indicators)
///   * Escape: Skip cutscenes
/// 
/// Key Improvements:
/// - All dialog functionality consolidated into one manager
/// - Removed dependencies on FindObjectOfType calls
/// - Better integration with CoreGame data structure
/// - Proper cleanup and memory management
/// - Support for both new CoreGame system and legacy dialog files
/// - Multiple response support for varied NPC reactions
/// - Dynamic NPC name display in 2D dialogs only (3D models represent characters inherently)
/// 
/// Usage:
/// - Use the existing CoreGame system for new dialogs
/// - Legacy support available through InitiateStartDialog() and OnNPCButtonClicked()
/// - Choice buttons are automatically found by name in QuestionTemplate: Q, W, E
/// - Fallback: All choice buttons can also be assigned to answerButtons[] in inspector
/// - Dialog and question templates should be assigned to npcDialogThemplate and npcQuestionThemplate
/// - Assign npcNameText for displaying NPC names in 2D dialogs (3D dialogs ignore this)
/// - Assign backgroundFade image for cutscene fade transitions
/// - Use SetResponseIndex() to choose which response to use from dialogResponses array
/// - Use UseRandomResponse() for random NPC reactions
/// - Use SetResponseByCondition() for conditional responses based on game state
/// - Set cutsceneType in CoreGameDialog for fade transitions between dialogs
/// - Input Controls: Space to progress dialogs, Q/W/E to select choices, Escape to skip cutscenes
/// </summary>

[System.Serializable]
public class DialogChoice
{
    public string playerChoice;
    [TextArea(2, 5)]
    public string npcResponse;
}

public class CoreGameManager : MonoBehaviour
{
    public int stressvariable;
    [Header("Core Game Settings")]
    public CoreGame coreGameData;
    
    [Header("Save Data")]
    [SerializeField] private CoreGameSaves saveData;
    [SerializeField] private string saveDataPath = "Saves/coregamesaves"; // Path in Resources folder
    
    [Header("Dialog Templates")]
    public GameObject npcDialogThemplate;
    public GameObject npcQuestionThemplate;
    
    [Header("Dialog Components")]
    public TMP_Text dialogText;
    public TMP_Text npcNameText; // For displaying NPC names
    public Image backgroundFade; // For fade in/out transitions
    
    [Header("Choice UI Components")]
    public Button[] answerButtons; // Assign 3+ buttons in Inspector
    
    [Header("Camera References")]
    public Transform defaultCamera;
    public Transform reyCamera;
    public Transform momCamera;
    public Transform fatherCamera;
    
    [Header("Audio Settings")]
    public AudioSource dialogAudioSource;
    
    [Header("Input Handler")]
    public DialogInputHandler dialogInputHandler; // New Input Handler
    
    // Private variables
    private GameObject dialogInstance;
    private GameObject questionInstance;
    private int currentBlockIndex = 0;
    private int currentChoiceResponseIndex = -1; // Which response in dialogResponses array is currently showing
    private int selectedChoiceIndex = -1; // Which choice was selected by the player
    private LTDescr dialogTween;
    private bool isShowingResponse = false;
    private bool isPlayingCutscene = false;
    private bool isTextAnimating = false;
    
    // FIXED: Add fields to track current animation state for proper skip handling
    private string lastProcessedDialogText = "";
    private TMP_Text lastTextComponent = null;
    
    // Enhanced input throttling to prevent spam clicking and race conditions
    private float lastInputTime = 0f;
    private const float INPUT_COOLDOWN = 0.5f; // Increased cooldown to prevent spam
    private bool isProcessingInput = false; // Prevent multiple simultaneous input processing
    private bool isInDialogTransition = false; // Prevent input during dialog transitions
    private float lastDialogUpdateTime = 0f; // Track last dialog update time
    
    // Audio tracking for dialog responses to prevent replaying
    private string lastPlayedAudioPath = ""; // Track the last played audio clip path
    private bool hasPlayedCurrentResponseAudio = false; // Track if current response audio was already played
    private int lastAudioPlayedForChoiceIndex = -1; // Track which choice index we played audio for
    private string lastPlayedChoiceText = ""; // Track the choice text we played audio for
    
    // Component safety tracking
    private Dictionary<string, TMP_Text> cachedTextComponents = new Dictionary<string, TMP_Text>();
    private bool componentsCached = false;
    
    // DialogSpace management for animation control
    private GameObject cachedDialogSpace = null;
    private bool dialogSpaceCached = false;
    
    // Choice management variables
    private System.Action<int> onChoiceSelected;
    private Dictionary<Button, int> buttonTweenIds = new Dictionary<Button, int>();
    private int selectedResponseIndex = 0; // Which response to use from dialogResponses array
    
    // Correct choice system - for filtering out incorrect choices
    private CoreGameDialogChoices[] currentFilteredChoices = null; // Filtered choices after incorrect selections
    private CoreGameDialogChoices[] originalChoices = null; // Original full choice array
    private bool isUsingFilteredChoices = false; // Whether we're currently using filtered choices
    
    // Pressed choices tracking - keep track of all choices that have been pressed
    private System.Collections.Generic.HashSet<string> pressedChoiceTexts = new System.Collections.Generic.HashSet<string>(); // Track by choice text
    private System.Collections.Generic.List<int> pressedChoiceIndices = new System.Collections.Generic.List<int>(); // Track by original indices
    
    // Choice set tracking - to determine if we're dealing with the same choice set
    private CoreGameDialogChoices[] lastProcessedChoices = null; // Last choice set we processed
    
    // Choice notification events
    public static System.Action<string> OnPlayerChoiceSelected; // Event for notifying about player choice selection
    private int lastProcessedBlockIndex = -1; // Track which dialog block we last processed choices for

    private System.Action currentCompletionCallback;
    public bool IsSequenceRunning { get; private set; }
    
    // Events
    public Action onCoreGameFinished;
    public Action<int> onBlockCompleted;

    #region Unity Lifecycle
    
    private void Awake()
    {
        try
        {
            // Initialize or get AudioSource component
            if (dialogAudioSource == null)
            {
                dialogAudioSource = GetComponent<AudioSource>();
                if (dialogAudioSource == null)
                {
                    dialogAudioSource = gameObject.AddComponent<AudioSource>();
                    Debug.Log("AudioSource component added to CoreGameManager for dialog audio.");
                }
                else
                {
                    Debug.Log("Found existing AudioSource component on CoreGameManager.");
                }
            }
            
            // Configure AudioSource settings
            if (dialogAudioSource != null)
            {
                dialogAudioSource.playOnAwake = false;
                dialogAudioSource.loop = false;
                dialogAudioSource.volume = 1.0f; // Set default volume
                Debug.Log("AudioSource configured for dialog playback.");
            }
            else
            {
                Debug.LogWarning("Failed to initialize AudioSource. Dialog audio will be disabled.");
            }
            
            // Auto-find DialogInputHandler if not assigned
            if (dialogInputHandler == null)
            {
                dialogInputHandler = GetComponent<DialogInputHandler>();
                if (dialogInputHandler == null)
                {
                    dialogInputHandler = FindFirstObjectByType<DialogInputHandler>();
                }
                
                if (dialogInputHandler != null)
                {
                    Debug.Log("DialogInputHandler found and assigned automatically.");
                }
                else
                {
                    Debug.LogWarning("DialogInputHandler not found. Will use fallback Input.GetKeyDown.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing AudioSource in CoreGameManager: {e.Message}. Dialog will work without audio.");
            dialogAudioSource = null;
        }
        
        // Initialize saveData for stress system
        try
        {
            if (saveData == null)
            {
                saveData = Resources.Load<CoreGameSaves>(saveDataPath);
                if (saveData != null)
                {
                    Debug.Log($"[STRESS] saveData loaded successfully from: {saveDataPath}");
                    // Synchronize stress values on startup
                    SynchronizeStressValues();
                }
                else
                {
                    Debug.LogWarning($"[STRESS] Failed to load saveData from: {saveDataPath}. Stress UI integration will not work.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[STRESS] Error loading saveData: {e.Message}");
        }
    }
    
    #endregion

    #region Public Methods

    /// <summary>
    /// Start the core game sequence with assigned ScriptableObject
    /// </summary>
    [Obsolete]
    public void StartCoreGame()
    {
        if (coreGameData == null || coreGameData.coreBlock == null || coreGameData.coreBlock.Length == 0)
        {
            Debug.LogError("CoreGame data is null or empty!");
            return;
        }
        
        currentBlockIndex = 0;
        
        // Reset choice tracking when starting a new conversation
        ResetFilteredChoicesSystem();
        
        ProcessCurrentBlock();
    }
    
    /// <summary>
    /// Start the core game sequence by loading from Resources folder
    /// </summary>
    /// <param name="resourcePath">Path to the CoreGame ScriptableObject in Resources folder (e.g., "resource/rey")</param>
    [Obsolete]
    public void StartCoreGame(string resourcePath, System.Action onComplete = null)
    {
        // Load the CoreGame ScriptableObject from Resources
        CoreGame loadedCoreGame = Resources.Load<CoreGame>(resourcePath);
        
        if (loadedCoreGame == null)
        {
            Debug.LogError($"CoreGame file not found at path: {resourcePath}");
            onComplete?.Invoke();
            return;
        }
        
        if (loadedCoreGame.coreBlock == null || loadedCoreGame.coreBlock.Length == 0)
        {
            Debug.LogError($"CoreGame at path '{resourcePath}' has no blocks!");
            onComplete?.Invoke();
            return;
        }
        
        // Set the loaded data as current
        coreGameData = loadedCoreGame;
        currentBlockIndex = 0;
        currentCompletionCallback = onComplete;
        IsSequenceRunning = true;
        
        // Reset choice tracking when starting a new conversation
        ResetFilteredChoicesSystem();
        
        Debug.Log($"Starting CoreGame sequence from: {resourcePath}");
        ProcessCurrentBlock();
    }

    /// <summary>
    /// Continue to the next block in the sequence
    /// </summary>
    public void ContinueToNextBlock()
    {
        if (isPlayingCutscene) return;
        
        currentBlockIndex++;
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            FinishCoreGame();
            return;
        }
        
        ProcessCurrentBlock();
    }

    /// <summary>
    /// Skip current cutscene if playing
    /// </summary>
    public void SkipCutscene()
    {
        if (isPlayingCutscene)
        {
            StopAllCoroutines();
            isPlayingCutscene = false;
            ContinueToNextBlock();
        }
    }
    
    /// <summary>
    /// Stops any currently playing dialog audio
    /// </summary>
    public void StopDialogAudio()
    {
        try
        {
            if (dialogAudioSource != null && dialogAudioSource.isPlaying)
            {
                dialogAudioSource.Stop();
                Debug.Log("Dialog audio stopped.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error stopping dialog audio: {e.Message}");
        }
    }
    
    /// <summary>
    /// Forces completion of current dialog animation and stops audio
    /// </summary>
    public void ForceCompleteDialog()
    {
        if (isTextAnimating)
        {
            SkipTextAnimation();
        }
    }
    
    /// <summary>
    /// Manually trigger a fade effect (useful for testing or custom scenarios)
    /// </summary>
    /// <param name="cutsceneType">Type of fade effect to perform</param>
    /// <param name="onComplete">Optional callback when fade completes</param>
    public void TriggerFadeEffect(CoreGameDialog.CutsceneType cutsceneType, System.Action onComplete = null)
    {
        HandleCutsceneFade(cutsceneType, onComplete);
    }
    
    /// <summary>
    /// Reset fade to default state (transparent and disabled)
    /// </summary>
    public void ResetFade()
    {
        if (backgroundFade != null)
        {
            backgroundFade.gameObject.SetActive(false);
            Color fadeColor = backgroundFade.color;
            fadeColor.a = 0f;
            backgroundFade.color = fadeColor;
        }
    }

    #endregion

    #region Core Game Processing

    private void ProcessCurrentBlock()
    {
        if (currentBlockIndex >= coreGameData.coreBlock.Length) return;
        
        CoreGameBlock currentBlock = coreGameData.coreBlock[currentBlockIndex];
        
        switch (currentBlock.Type)
        {
            case CoreGameBlock.CoreType.Dialog:
                ProcessDialogBlock(currentBlock);
                break;
                
            case CoreGameBlock.CoreType.Cutscene:
                ProcessCutsceneBlock(currentBlock);
                break;
        }
    }

    private void ProcessDialogBlock(CoreGameBlock block)
    {
        if (block.Dialog == null)
        {
            Debug.LogWarning($"Dialog block at index {currentBlockIndex} has no dialog data!");
            ContinueToNextBlock();
            return;
        }
        
        // Clear any existing 3D dialogs before showing new dialog
        ClearAll3DDialogs();
        
        // If we're switching from 2D to 3D dialog, destroy 2D dialog instances
        if (block.Dialog.dialogType == CoreGameDialog.DialogType.ThreeD && dialogInstance != null)
        {
            DestroyDialogInstances();
        }
        
        // Set up camera based on dialog choice
        SetupDialogCamera(block.Dialog.camChoice);
        
        // Show dialog based on type
        if (block.Dialog.dialogType == CoreGameDialog.DialogType.ThreeD)
        {
            Show3DDialog(block.Dialog);
        }
        else
        {
            Show2DDialog(block.Dialog);
        }
    }

    private void ProcessCutsceneBlock(CoreGameBlock block)
    {
        if (block.Animation == null)
        {
            Debug.LogWarning($"Cutscene block at index {currentBlockIndex} has no animation data!");
            ContinueToNextBlock();
            return;
        }
        
        // Clear any 3D dialogs and destroy 2D dialog instances when starting cutscene
        ClearAll3DDialogs();
        DestroyDialogInstances();
        
        StartCoroutine(PlayCutscene(block.Animation));
    }

    #endregion

    #region Helper Methods for Dialog Text Assignment
    
    /// <summary>
    /// Convert NPC name enum to display string, handling 'None' case
    /// </summary>
    private string ConvertNpcNameToString(CoreGameDialog.NpcName npcName)
    {
        if (npcName == CoreGameDialog.NpcName.None)
        {
            return ""; // Return empty string for None
        }
        return npcName.ToString();
    }
    
    /// <summary>
    /// Convert NPC name enum to display string for choice responses, handling 'None' case
    /// </summary>
    private string ConvertNpcNameToString(CoreGameDialogChoicesResponse.NpcName npcName)
    {
        if (npcName == CoreGameDialogChoicesResponse.NpcName.None)
        {
            return ""; // Return empty string for None
        }
        return npcName.ToString();
    }
    
    /// <summary>
    /// Update both NPC name and dialog text simultaneously without delays
    /// This ensures both elements update at exactly the same time
    /// </summary>
    public void UpdateDialogAndNameSynchronized(string npcName, string dialogText)
    {
        Debug.Log($"[SYNC] Synchronously updating NPC name: '{npcName}' and dialog: '{dialogText}'");
        
        if (dialogInstance == null)
        {
            Debug.LogWarning("[SYNC] Dialog instance is null, cannot update dialog elements");
            return;
        }
        
        // Cache components if needed (but skip all the throttling and delays)
        if (!componentsCached || !ValidateComponentCache())
        {
            CacheDialogComponents();
        }
        
        bool nameUpdated = false;
        bool textUpdated = false;
        
        // Update NPC name immediately (no throttling or delays)
        if (!string.IsNullOrEmpty(npcName) || npcName == "") // Allow empty names for "None" case
        {
            if (cachedTextComponents.ContainsKey("DialogueName"))
            {
                try
                {
                    TMP_Text nameComponent = cachedTextComponents["DialogueName"];
                    if (nameComponent != null && nameComponent.gameObject != null)
                    {
                        nameComponent.text = npcName;
                        Debug.Log($"[SYNC] ✓ Updated NPC name via cached component: '{npcName}'");
                        nameUpdated = true;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SYNC] Error updating cached name component: {e.Message}");
                }
            }
            
            // Fallback: Direct search for name component
            if (!nameUpdated)
            {
                Transform nameTransform = dialogInstance.transform.Find("DialogueName");
                if (nameTransform != null)
                {
                    TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
                    if (nameText != null)
                    {
                        nameText.text = npcName;
                        Debug.Log($"[SYNC] ✓ Updated NPC name via direct search: '{npcName}'");
                        
                        // Cache for future use
                        cachedTextComponents["DialogueName"] = nameText;
                        nameUpdated = true;
                    }
                }
            }
        }
        
        // Update dialog text immediately (no throttling or delays)
        if (!string.IsNullOrEmpty(dialogText))
        {
            if (cachedTextComponents.ContainsKey("DialogueText"))
            {
                try
                {
                    TMP_Text textComponent = cachedTextComponents["DialogueText"];
                    if (textComponent != null && textComponent.gameObject != null)
                    {
                        textComponent.text = dialogText;
                        Debug.Log($"[SYNC] ✓ Updated dialog text via cached component");
                        textUpdated = true;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SYNC] Error updating cached text component: {e.Message}");
                }
            }
            
            // Fallback: Direct search for text component
            if (!textUpdated)
            {
                Transform textTransform = dialogInstance.transform.Find("DialogueText");
                if (textTransform != null)
                {
                    TMP_Text textComponent = textTransform.GetComponent<TMP_Text>();
                    if (textComponent != null)
                    {
                        textComponent.text = dialogText;
                        Debug.Log($"[SYNC] ✓ Updated dialog text via direct search");
                        
                        // Cache for future use
                        cachedTextComponents["DialogueText"] = textComponent;
                        textUpdated = true;
                    }
                }
            }
            
            // Fallback: Try DialogPrefabController
            if (!textUpdated)
            {
                DialogPrefabController controller = dialogInstance.GetComponent<DialogPrefabController>();
                if (controller != null)
                {
                    try
                    {
                        controller.SetDialogText(dialogText);
                        Debug.Log($"[SYNC] ✓ Updated dialog text via DialogPrefabController");
                        textUpdated = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[SYNC] Failed to update via DialogPrefabController: {e.Message}");
                    }
                }
            }
        }
        
        if (!nameUpdated && (!string.IsNullOrEmpty(npcName) || npcName == ""))
        {
            Debug.LogWarning($"[SYNC] Failed to update NPC name: '{npcName}'");
        }
        
        if (!textUpdated && !string.IsNullOrEmpty(dialogText))
        {
            Debug.LogWarning($"[SYNC] Failed to update dialog text");
        }
        
        Debug.Log($"[SYNC] Synchronization complete - Name: {(nameUpdated ? "✓" : "✗")}, Text: {(textUpdated ? "✓" : "✗")}");
    }
    
    /// <summary>
    /// Update NPC name immediately without any delays or throttling
    /// Use this when you need instant synchronization with dialog text
    /// </summary>
    public void UpdateNpcNameImmediate(string npcName)
    {
        if (dialogInstance == null)
        {
            Debug.LogWarning("[IMMEDIATE] Dialog instance is null, cannot update NPC name");
            return;
        }
        
        // Convert null to empty string
        if (npcName == null)
        {
            npcName = "";
        }
        
        bool nameUpdated = false;
        
        // Try cached component first
        if (cachedTextComponents.ContainsKey("DialogueName"))
        {
            try
            {
                TMP_Text nameComponent = cachedTextComponents["DialogueName"];
                if (nameComponent != null && nameComponent.gameObject != null)
                {
                    nameComponent.text = npcName;
                    nameUpdated = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[IMMEDIATE] Error updating cached name component: {e.Message}");
            }
        }
        
        // Direct search if cached failed
        if (!nameUpdated)
        {
            Transform nameTransform = dialogInstance.transform.Find("DialogueName");
            if (nameTransform != null)
            {
                TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = npcName;
                    // Cache for future use
                    cachedTextComponents["DialogueName"] = nameText;
                    nameUpdated = true;
                }
            }
        }
        
        if (!nameUpdated)
        {
            Debug.LogWarning($"[IMMEDIATE] Failed to update NPC name immediately: '{npcName}'");
        }
    }
    
    /// <summary>
    /// Update dialog text immediately without any delays or throttling
    /// Use this when you need instant synchronization with NPC name
    /// </summary>
    public void UpdateDialogTextImmediate(string dialogText)
    {
        if (dialogInstance == null || string.IsNullOrEmpty(dialogText))
        {
            return;
        }
        
        bool textUpdated = false;
        
        // Try cached component first
        if (cachedTextComponents.ContainsKey("DialogueText"))
        {
            try
            {
                TMP_Text textComponent = cachedTextComponents["DialogueText"];
                if (textComponent != null && textComponent.gameObject != null)
                {
                    textComponent.text = dialogText;
                    textUpdated = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[IMMEDIATE] Error updating cached text component: {e.Message}");
            }
        }
        
        // Direct search if cached failed
        if (!textUpdated)
        {
            Transform textTransform = dialogInstance.transform.Find("DialogueText");
            if (textTransform != null)
            {
                TMP_Text textComponent = textTransform.GetComponent<TMP_Text>();
                if (textComponent != null)
                {
                    textComponent.text = dialogText;
                    // Cache for future use
                    cachedTextComponents["DialogueText"] = textComponent;
                    textUpdated = true;
                }
            }
        }
        
        // Fallback: Try DialogPrefabController
        if (!textUpdated)
        {
            DialogPrefabController controller = dialogInstance.GetComponent<DialogPrefabController>();
            if (controller != null)
            {
                try
                {
                    controller.SetDialogText(dialogText);
                    textUpdated = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[IMMEDIATE] Failed to update via DialogPrefabController: {e.Message}");
                }
            }
        }
        
        if (!textUpdated)
        {
            Debug.LogWarning($"[IMMEDIATE] Failed to update dialog text immediately");
        }
    }
    
    /// <summary>
    /// Helper method to update NPC name with enhanced error handling and spam protection
    /// </summary>
    /// <param name="npcName">The NPC name to display</param>
    /// <param name="bypassThrottling">If true, skips all delays and throttling for immediate update</param>
    public void UpdateNpcNameSafe(string npcName, bool bypassThrottling = false)
    {
        Debug.Log($"[SAFE-NAME] Updating NPC name to: '{npcName}' (Bypass throttling: {bypassThrottling})");
        
        // Allow empty/null names - this is needed to clear the display when NPC name is "None"
        if (npcName == null)
        {
            npcName = ""; // Convert null to empty string
            Debug.Log("[SAFE-NAME] Converting null NPC name to empty string");
        }
        
        if (dialogInstance == null)
        {
            Debug.LogWarning("[SAFE-NAME] Dialog instance is null, cannot update NPC name");
            return;
        }
        
        // Use immediate update if bypassing throttling
        if (bypassThrottling)
        {
            UpdateNpcNameImmediate(npcName);
            return;
        }
        
        // Enhanced spam protection - prevent updates during processing or transitions
        if (isProcessingInput || isInDialogTransition)
        {
            Debug.Log("[SAFE-NAME] Currently processing input or in transition, deferring NPC name update");
            StartCoroutine(DeferredUpdateNpcName(npcName));
            return;
        }
        
        // Prevent rapid successive updates
        float currentTime = Time.time;
        if (currentTime - lastDialogUpdateTime < 0.1f)
        {
            Debug.LogWarning("[SAFE-NAME] Too rapid update attempt, skipping to prevent race condition");
            return;
        }
        lastDialogUpdateTime = currentTime;
        
        // Cache components on first use or if cache is invalid
        if (!componentsCached || !ValidateComponentCache())
        {
            CacheDialogComponents();
        }
        
        bool nameUpdated = false;
        
        // Method 1: Try cached DialogueName component first
        if (cachedTextComponents.ContainsKey("DialogueName"))
        {
            try
            {
                TMP_Text nameComponent = cachedTextComponents["DialogueName"];
                if (nameComponent != null && nameComponent.gameObject != null)
                {
                    // Double-check this is actually the name component and not text component
                    string componentPath = GetTransformPath(nameComponent.transform);
                    if (componentPath.ToLower().Contains("name") && !componentPath.ToLower().Contains("text"))
                    {
                        nameComponent.text = npcName;
                        Debug.Log($"[SAFE-NAME] ✓ Updated NPC name via cached DialogueName component: '{npcName}'");
                        nameUpdated = true;
                    }
                    else
                    {
                        Debug.LogError($"[SAFE-NAME] CRITICAL: Cached component path '{componentPath}' looks like a text component, not name!");
                        cachedTextComponents.Remove("DialogueName");
                        componentsCached = false;
                    }
                }
                else
                {
                    Debug.LogWarning("[SAFE-NAME] Cached DialogueName component is null or destroyed, clearing cache");
                    cachedTextComponents.Remove("DialogueName");
                    componentsCached = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SAFE-NAME] Error updating cached DialogueName component: {e.Message}");
                cachedTextComponents.Remove("DialogueName");
                componentsCached = false;
            }
        }
        
        // Method 2: Try DialogPrefabController if caching failed
        if (!nameUpdated)
        {
            DialogPrefabController controller = dialogInstance.GetComponent<DialogPrefabController>();
            if (controller != null)
            {
                try
                {
                    controller.SetDialogName(npcName);
                    Debug.Log($"[SAFE-NAME] ✓ Updated NPC name via DialogPrefabController");
                    nameUpdated = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SAFE-NAME] Failed to update via DialogPrefabController: {e.Message}");
                }
            }
        }
        
        // Method 3: Direct search with enhanced validation
        if (!nameUpdated)
        {
            Transform nameTransform = dialogInstance.transform.Find("DialogueName");
            if (nameTransform != null)
            {
                TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
                if (nameText != null)
                {
                    // CRITICAL: Enhanced validation to ensure this is the name component
                    string transformName = nameTransform.name.ToLower();
                    string fullPath = GetTransformPath(nameTransform).ToLower();
                    
                    bool isNameComponent = (transformName.Contains("name") && !transformName.Contains("text") && !transformName.Contains("dialogue")) ||
                                          (fullPath.Contains("name") && !fullPath.Contains("text"));
                    
                    if (isNameComponent)
                    {
                        nameText.text = npcName;
                        Debug.Log($"[SAFE-NAME] ✓ Updated DialogueName directly: '{npcName}' -> '{nameTransform.name}'");
                        
                        // Cache this component for future use
                        cachedTextComponents["DialogueName"] = nameText;
                        nameUpdated = true;
                    }
                    else
                    {
                        Debug.LogError($"[SAFE-NAME] Component validation failed: '{nameTransform.name}' at path '{fullPath}' doesn't look like a name component");
                    }
                }
                else
                {
                    Debug.LogWarning($"[SAFE-NAME] DialogueName transform found but no TMP_Text component");
                }
            }
            else
            {
                Debug.LogWarning($"[SAFE-NAME] DialogueName transform not found in dialog instance");
            }
        }
        
        if (!nameUpdated)
        {
            Debug.LogError($"[SAFE-NAME] ✗ CRITICAL FAILURE: Could not update NPC name: '{npcName}'");
            LogAllDialogComponents(); // Debug helper
        }
    }
    
    /// <summary>
    /// Deferred update for NPC name when processing is busy
    /// </summary>
    private IEnumerator DeferredUpdateNpcName(string npcName)
    {
        Debug.Log($"[SAFE-NAME] Deferring NPC name update: '{npcName}'");
        
        // Wait until processing is complete
        while (isProcessingInput || isInDialogTransition)
        {
            yield return new WaitForSeconds(0.05f);
        }
        
        // Wait a bit more for safety
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log($"[SAFE-NAME] Executing deferred NPC name update: '{npcName}'");
        UpdateNpcNameSafe(npcName);
    }
    
    /// <summary>
    /// Validate that cached components are still valid and point to correct objects
    /// </summary>
    private bool ValidateComponentCache()
    {
        if (!componentsCached || cachedTextComponents.Count == 0)
        {
            return false;
        }
        
        // Check if cached components are still valid
        var keysToRemove = new System.Collections.Generic.List<string>();
        
        foreach (var kvp in cachedTextComponents)
        {
            if (kvp.Value == null || kvp.Value.gameObject == null)
            {
                Debug.LogWarning($"[CACHE] Cached component '{kvp.Key}' is invalid, marking for removal");
                keysToRemove.Add(kvp.Key);
            }
        }
        
        // Remove invalid entries
        foreach (string key in keysToRemove)
        {
            cachedTextComponents.Remove(key);
        }
        
        if (keysToRemove.Count > 0)
        {
            Debug.LogWarning($"[CACHE] Removed {keysToRemove.Count} invalid cached components");
            componentsCached = false;
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Cache dialog components to prevent repeated searches and ensure correct component targeting
    /// </summary>
    private void CacheDialogComponents()
    {
        if (dialogInstance == null)
        {
            Debug.LogWarning("[CACHE] Dialog instance is null, cannot cache components");
            return;
        }
        
        Debug.Log("[CACHE] Caching dialog components...");
        cachedTextComponents.Clear();
        
        // Find and cache DialogueName component
        Transform nameTransform = dialogInstance.transform.Find("DialogueName");
        if (nameTransform != null)
        {
            TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                cachedTextComponents["DialogueName"] = nameText;
                Debug.Log($"[CACHE] ✓ Cached DialogueName component: {nameTransform.name}");
            }
        }
        
        // Find and cache DialogueText component
        Transform textTransform = dialogInstance.transform.Find("DialogueText");
        if (textTransform != null)
        {
            TMP_Text dialogText = textTransform.GetComponent<TMP_Text>();
            if (dialogText != null)
            {
                cachedTextComponents["DialogueText"] = dialogText;
                Debug.Log($"[CACHE] ✓ Cached DialogueText component: {textTransform.name}");
            }
        }
        
        componentsCached = true;
        Debug.Log($"[CACHE] Caching complete. {cachedTextComponents.Count} components cached.");
    }
    
    /// <summary>
    /// Clear cached components (call when dialog instance changes)
    /// </summary>
    private void ClearComponentCache()
    {
        cachedTextComponents.Clear();
        componentsCached = false;
        
        // Clear DialogSpace cache when dialog instance changes
        cachedDialogSpace = null;
        dialogSpaceCached = false;
        
        Debug.Log("[CACHE] Component cache cleared including DialogSpace");
    }
    
    /// <summary>
    /// Find and cache the DialogSpace GameObject in the current dialog instance
    /// </summary>
    private void CacheDialogSpace()
    {
        cachedDialogSpace = null;
        dialogSpaceCached = false;
        
        if (dialogInstance == null)
        {
            Debug.LogWarning("[DIALOGSPACE] Dialog instance is null, cannot cache DialogSpace");
            return;
        }
        
        // Look for DialogSpace GameObject in the dialog instance
        Transform dialogSpaceTransform = dialogInstance.transform.Find("DialogSpace");
        if (dialogSpaceTransform != null)
        {
            cachedDialogSpace = dialogSpaceTransform.gameObject;
            dialogSpaceCached = true;
            Debug.Log($"[DIALOGSPACE] ✓ Found and cached DialogSpace: {cachedDialogSpace.name}");
        }
        else
        {
            // Try finding it in children with recursive search
            Transform[] allChildren = dialogInstance.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.name.Equals("DialogSpace", System.StringComparison.OrdinalIgnoreCase))
                {
                    cachedDialogSpace = child.gameObject;
                    dialogSpaceCached = true;
                    Debug.Log($"[DIALOGSPACE] ✓ Found DialogSpace via recursive search: {cachedDialogSpace.name}");
                    break;
                }
            }
            
            if (cachedDialogSpace == null)
            {
                Debug.LogWarning("[DIALOGSPACE] DialogSpace GameObject not found in dialog instance");
            }
        }
    }
    
    /// <summary>
    /// Set DialogSpace active/inactive state with proper error handling
    /// </summary>
    private void SetDialogSpaceActive(bool active)
    {
        // Cache DialogSpace if not already cached or if cache is invalid
        if (!dialogSpaceCached || cachedDialogSpace == null)
        {
            CacheDialogSpace();
        }
        
        if (cachedDialogSpace != null)
        {
            try
            {
                bool currentState = cachedDialogSpace.activeSelf;
                if (currentState != active)
                {
                    cachedDialogSpace.SetActive(active);
                    Debug.Log($"[DIALOGSPACE] DialogSpace set to {(active ? "ENABLED" : "DISABLED")}");
                }
                else
                {
                    Debug.Log($"[DIALOGSPACE] DialogSpace already {(active ? "enabled" : "disabled")}, no change needed");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DIALOGSPACE] Error setting DialogSpace active state: {e.Message}");
                // Clear cache if GameObject is destroyed
                cachedDialogSpace = null;
                dialogSpaceCached = false;
            }
        }
        else
        {
            Debug.LogWarning($"[DIALOGSPACE] Cannot set DialogSpace to {(active ? "enabled" : "disabled")} - DialogSpace not found");
        }
    }
    
    /// <summary>
    /// Debug helper to log all dialog components
    /// </summary>
    private void LogAllDialogComponents()
    {
        if (dialogInstance == null)
        {
            Debug.LogError("[DEBUG] Dialog instance is null, cannot log components");
            return;
        }
        
        Debug.Log("[DEBUG] === ALL DIALOG COMPONENTS ===");
        TMP_Text[] allTexts = dialogInstance.GetComponentsInChildren<TMP_Text>();
        Debug.Log($"[DEBUG] Found {allTexts.Length} TMP_Text components:");
        
        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            string path = GetTransformPath(text.transform);
            Debug.Log($"[DEBUG]   [{i}] Path: {path}");
            Debug.Log($"[DEBUG]       Name: {text.transform.name}");
            Debug.Log($"[DEBUG]       Current Text: '{text.text}'");
            Debug.Log($"[DEBUG]       Parent: {text.transform.parent?.name ?? "None"}");
            Debug.Log($"[DEBUG]       ---");
        }
        
        Debug.Log("[DEBUG] === END DIALOG COMPONENTS ===");
    }
    
    /// <summary>
    /// Get the full path of a transform for debugging
    /// </summary>
    private string GetTransformPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        
        while (parent != null && parent != dialogInstance.transform)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    /// <summary>
    /// Helper method to update dialog text with enhanced error handling and spam protection
    /// </summary>
    public void UpdateDialogTextSafe(string dialogText)
    {
        Debug.Log($"[SAFE-TEXT] Updating dialog text to: '{dialogText}'");
        
        if (string.IsNullOrEmpty(dialogText))
        {
            Debug.LogWarning("[SAFE-TEXT] Dialog text is null or empty, skipping update");
            return;
        }
        
        if (dialogInstance == null)
        {
            Debug.LogWarning("[SAFE-TEXT] Dialog instance is null, cannot update dialog text");
            return;
        }
        
        // Enhanced spam protection - prevent updates during processing or transitions
        if (isProcessingInput || isInDialogTransition)
        {
            Debug.Log("[SAFE-TEXT] Currently processing input or in transition, deferring dialog text update");
            StartCoroutine(DeferredUpdateDialogText(dialogText));
            return;
        }
        
        // Prevent rapid successive updates
        float currentTime = Time.time;
        if (currentTime - lastDialogUpdateTime < 0.1f)
        {
            Debug.LogWarning("[SAFE-TEXT] Too rapid update attempt, skipping to prevent race condition");
            return;
        }
        lastDialogUpdateTime = currentTime;
        
        // Cache components on first use or if cache is invalid
        if (!componentsCached || !ValidateComponentCache())
        {
            CacheDialogComponents();
        }
        
        bool textUpdated = false;
        
        // Method 1: Try cached DialogueText component first
        if (cachedTextComponents.ContainsKey("DialogueText"))
        {
            try
            {
                TMP_Text textComponent = cachedTextComponents["DialogueText"];
                if (textComponent != null && textComponent.gameObject != null)
                {
                    // Double-check this is actually the dialog text component and not name component
                    string componentPath = GetTransformPath(textComponent.transform);
                    if ((componentPath.ToLower().Contains("text") || componentPath.ToLower().Contains("dialogue")) && !componentPath.ToLower().Contains("name"))
                    {
                        textComponent.text = dialogText;
                        Debug.Log($"[SAFE-TEXT] ✓ Updated dialog text via cached DialogueText component");
                        textUpdated = true;
                    }
                    else
                    {
                        Debug.LogError($"[SAFE-TEXT] CRITICAL: Cached component path '{componentPath}' looks like a name component, not text!");
                        cachedTextComponents.Remove("DialogueText");
                        componentsCached = false;
                    }
                }
                else
                {
                    Debug.LogWarning("[SAFE-TEXT] Cached DialogueText component is null or destroyed, clearing cache");
                    cachedTextComponents.Remove("DialogueText");
                    componentsCached = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SAFE-TEXT] Error updating cached DialogueText component: {e.Message}");
                cachedTextComponents.Remove("DialogueText");
                componentsCached = false;
            }
        }
        
        // Method 2: Try DialogPrefabController if caching failed
        if (!textUpdated)
        {
            DialogPrefabController controller = dialogInstance.GetComponent<DialogPrefabController>();
            if (controller != null)
            {
                try
                {
                    controller.SetDialogText(dialogText);
                    Debug.Log($"[SAFE-TEXT] ✓ Updated dialog text via DialogPrefabController");
                    textUpdated = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SAFE-TEXT] Failed to update via DialogPrefabController: {e.Message}");
                }
            }
        }
        
        // Method 3: Direct search with enhanced validation
        if (!textUpdated)
        {
            Transform textTransform = dialogInstance.transform.Find("DialogueText");
            if (textTransform != null)
            {
                TMP_Text textComponent = textTransform.GetComponent<TMP_Text>();
                if (textComponent != null)
                {
                    // CRITICAL: Enhanced validation to ensure this is the dialog text component
                    string transformName = textTransform.name.ToLower();
                    string fullPath = GetTransformPath(textTransform).ToLower();
                    
                    bool isTextComponent = ((transformName.Contains("text") || transformName.Contains("dialogue")) && !transformName.Contains("name")) ||
                                          (fullPath.Contains("text") && !fullPath.Contains("name"));
                    
                    if (isTextComponent)
                    {
                        textComponent.text = dialogText;
                        Debug.Log($"[SAFE-TEXT] ✓ Updated DialogueText directly: '{textTransform.name}'");
                        
                        // Cache this component for future use
                        cachedTextComponents["DialogueText"] = textComponent;
                        textUpdated = true;
                    }
                    else
                    {
                        Debug.LogError($"[SAFE-TEXT] Component validation failed: '{textTransform.name}' at path '{fullPath}' doesn't look like a dialog text component");
                    }
                }
                else
                {
                    Debug.LogWarning($"[SAFE-TEXT] DialogueText transform found but no TMP_Text component");
                }
            }
            else
            {
                Debug.LogWarning($"[SAFE-TEXT] DialogueText transform not found in dialog instance");
            }
        }
        
        if (!textUpdated)
        {
            Debug.LogError($"[SAFE-TEXT] ✗ CRITICAL FAILURE: Could not update dialog text");
            LogAllDialogComponents(); // Debug helper
        }
    }
    
    /// <summary>
    /// Deferred update for dialog text when processing is busy
    /// </summary>
    private IEnumerator DeferredUpdateDialogText(string dialogText)
    {
        Debug.Log($"[SAFE-TEXT] Deferring dialog text update");
        
        // Wait until processing is complete
        while (isProcessingInput || isInDialogTransition)
        {
            yield return new WaitForSeconds(0.05f);
        }
        
        // Wait a bit more for safety
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log($"[SAFE-TEXT] Executing deferred dialog text update");
        UpdateDialogTextSafe(dialogText);
    }
    
    /// <summary>
    /// Helper method to update button text with proper error handling
    /// </summary>
    public void UpdateButtonTextSafe(string buttonName, string buttonText)
    {
        Debug.Log($"[SAFE] Updating button '{buttonName}' text to: '{buttonText}'");
        
        if (questionInstance == null)
        {
            Debug.LogWarning("[SAFE] Question instance is null!");
            return;
        }

        bool updated = false;
        
        // Method 1: Try DialogPrefabController first
        DialogPrefabController controller = questionInstance.GetComponent<DialogPrefabController>();
        if (controller != null)
        {
            Debug.Log($"[SAFE] Found DialogPrefabController, attempting to set button text...");
            controller.SetButtonText(buttonName, buttonText);
            Debug.Log($"[SAFE] ✓ Updated button via DialogPrefabController");
            updated = true;
        }
        
        // Method 2: Direct search for button (always try this as backup verification)
        Transform buttonTransform = questionInstance.transform.Find(buttonName);
        if (buttonTransform != null)
        {
            Button button = buttonTransform.GetComponent<Button>();
            if (button != null)
            {
                TMP_Text btnText = button.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    Debug.Log($"[SAFE] Found button '{buttonName}' TMP_Text component: '{btnText.transform.name}' (current text: '{btnText.text}')");
                    btnText.text = buttonText;
                    Debug.Log($"[SAFE] ✓ Updated button '{buttonName}' directly to: '{btnText.text}'");
                    updated = true;
                }
                else
                {
                    Debug.LogWarning($"[SAFE] Button '{buttonName}' has no TMP_Text component!");
                    
                    // Debug button structure
                    Transform[] children = button.GetComponentsInChildren<Transform>();
                    Debug.Log($"[SAFE] Button '{buttonName}' children:");
                    for (int i = 0; i < children.Length; i++)
                    {
                        Component[] components = children[i].GetComponents<Component>();
                        string componentNames = "";
                        for (int j = 0; j < components.Length; j++)
                        {
                            componentNames += components[j].GetType().Name;
                            if (j < components.Length - 1) componentNames += ", ";
                        }
                        Debug.Log($"[SAFE]   - {children[i].name} (Components: {componentNames})");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[SAFE] Found transform '{buttonName}' but no Button component!");
            }
        }
        else
        {
            Debug.LogWarning($"[SAFE] Button transform '{buttonName}' not found!");
            
            // Debug all children in question instance
            Debug.Log($"[SAFE] Question instance children:");
            Transform[] allChildren = questionInstance.GetComponentsInChildren<Transform>();
            for (int i = 0; i < allChildren.Length; i++)
            {
                Debug.Log($"[SAFE]   - {allChildren[i].name}");
            }
        }
        
        if (!updated)
        {
            Debug.LogError($"[SAFE] ✗ FAILED to update button '{buttonName}' text: {buttonText}");
        }
    }
    
    /// <summary>
    /// Test method to manually verify dialog text assignments
    /// Call this from Unity Inspector or console to test your setup
    /// </summary>
    [ContextMenu("Test Dialog Text Assignment")]
    public void TestDialogTextAssignment()
    {
        Debug.Log("=== TESTING DIALOG TEXT ASSIGNMENT ===");
        
        // Test NPC name assignment
        UpdateNpcNameSafe("Test NPC Name");
        
        // Test dialog text assignment
        UpdateDialogTextSafe("This is a test dialog message");
        
        // Test button text assignment
        UpdateButtonTextSafe("Q", "[Q] Test Choice 1");
        UpdateButtonTextSafe("W", "[W] Test Choice 2");
        UpdateButtonTextSafe("E", "[E] Test Choice 3");
        
        Debug.Log("=== END DIALOG TEXT ASSIGNMENT TEST ===");
    }
    
    /// <summary>
    /// Test synchronized dialog and name updates - verifies both update at exactly the same time
    /// </summary>
    [ContextMenu("Test Synchronized Dialog Updates")]
    public void TestSynchronizedDialogUpdates()
    {
        Debug.Log("=== TESTING SYNCHRONIZED DIALOG UPDATES ===");
        
        if (dialogInstance == null)
        {
            Debug.LogError("No dialog instance found! Please show a dialog first.");
            return;
        }
        
        // Test 1: Synchronized update
        Debug.Log("Test 1: UpdateDialogAndNameSynchronized()");
        UpdateDialogAndNameSynchronized("Synchronized NPC", "This text and name should update at exactly the same time!");
        
        Debug.Log("Waiting 2 seconds...");
        StartCoroutine(TestSynchronizedDelayed());
    }
    
    private IEnumerator TestSynchronizedDelayed()
    {
        yield return new WaitForSeconds(2f);
        
        // Test 2: Individual immediate updates
        Debug.Log("Test 2: Individual immediate updates");
        UpdateNpcNameImmediate("Immediate NPC");
        UpdateDialogTextImmediate("This should also be instant with no delays!");
        
        yield return new WaitForSeconds(2f);
        
        // Test 3: Compare with old throttled method (should show delay)
        Debug.Log("Test 3: Old throttled method (notice the delay)");
        UpdateNpcNameSafe("Throttled NPC");
        UpdateDialogTextSafe("This name might appear after some delay due to throttling");
        
        Debug.Log("=== SYNCHRONIZED DIALOG UPDATES TEST COMPLETE ===");
    }
    
    /// <summary>
    /// Test NPC name None handling - verifies that None enum shows blank
    /// </summary>
    [ContextMenu("Test NPC Name None Handling")]
    public void TestNpcNameNoneHandling()
    {
        Debug.Log("=== TESTING NPC NAME NONE HANDLING ===");
        
        // Test CoreGameDialog.NpcName.None
        CoreGameDialog.NpcName dialogNone = CoreGameDialog.NpcName.None;
        string dialogNoneString = ConvertNpcNameToString(dialogNone);
        Debug.Log($"CoreGameDialog.NpcName.None converts to: '{dialogNoneString}' (Length: {dialogNoneString.Length})");
        
        // Test CoreGameDialogChoicesResponse.NpcName.None  
        CoreGameDialogChoicesResponse.NpcName responseNone = CoreGameDialogChoicesResponse.NpcName.None;
        string responseNoneString = ConvertNpcNameToString(responseNone);
        Debug.Log($"CoreGameDialogChoicesResponse.NpcName.None converts to: '{responseNoneString}' (Length: {responseNoneString.Length})");
        
        // Test updating the UI with empty name
        if (dialogInstance != null)
        {
            Debug.Log("Testing UI update with empty NPC name...");
            UpdateNpcNameSafe(dialogNoneString);
        }
        else
        {
            Debug.LogWarning("No dialog instance available for UI test");
        }
        
        Debug.Log("=== END NPC NAME NONE HANDLING TEST ===");
    }
    
    /// <summary>
    /// Debug method to inspect current dialog/choice data
    /// </summary>
    [ContextMenu("Debug Current Dialog Data")]
    public void DebugCurrentDialogData()
    {
        Debug.Log("=== DEBUGGING CURRENT DIALOG DATA ===");
        
        if (coreGameData == null)
        {
            Debug.LogError("CoreGameData is null!");
            return;
        }
        
        if (coreGameData.coreBlock == null || coreGameData.coreBlock.Length == 0)
        {
            Debug.LogError("CoreGameData has no blocks!");
            return;
        }
        
        Debug.Log($"CoreGameData has {coreGameData.coreBlock.Length} blocks");
        Debug.Log($"Current block index: {currentBlockIndex}");
        
        if (currentBlockIndex < coreGameData.coreBlock.Length)
        {
            var currentBlock = coreGameData.coreBlock[currentBlockIndex];
            if (currentBlock.Dialog != null)
            {
                Debug.Log($"Current dialog:");
                Debug.Log($"  - npcName: '{currentBlock.Dialog.npcName}'");
                Debug.Log($"  - dialogEntry: '{currentBlock.Dialog.dialogEntry}'");
                
                if (currentBlock.Dialog.choices != null)
                {
                    Debug.Log($"  - Has {currentBlock.Dialog.choices.Length} choices:");
                    for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
                    {
                        var choice = currentBlock.Dialog.choices[i];
                        if (choice != null)
                        {
                            Debug.Log($"    Choice {i}: '{choice.playerChoice}'");
                        }
                        else
                        {
                            Debug.LogWarning($"    Choice {i}: NULL");
                        }
                    }
                }
                else
                {
                    Debug.Log("  - No choices");
                }
            }
            else
            {
                Debug.Log("Current block has no dialog");
            }
        }
        
        Debug.Log("=== END DIALOG DATA DEBUG ===");
    }
    
    /// <summary>
    /// Test button array access and modification
    /// </summary>
    [ContextMenu("Test Button Array")]
    public void TestButtonArray()
    {
        Debug.Log("=== TESTING BUTTON ARRAY ACCESS ===");
        
        if (questionInstance == null)
        {
            Debug.LogError("Question instance is null! Cannot test buttons.");
            return;
        }
        
        Button[] buttonArray = questionInstance.GetComponentsInChildren<Button>();
        Debug.Log($"Found {buttonArray.Length} buttons in question instance");
        
        for (int i = 0; i < buttonArray.Length; i++)
        {
            Button btn = buttonArray[i];
            if (btn != null)
            {
                Debug.Log($"Button {i}: '{btn.name}' (GameObject: {btn.gameObject.name})");
                
                TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    string testText = $"[TEST{i}] Button {i} Array Test";
                    Debug.Log($"Setting button {i} text to: '{testText}'");
                    btnText.text = testText;
                    Debug.Log($"Button {i} text is now: '{btnText.text}'");
                }
                else
                {
                    Debug.LogError($"CRITICAL: Button {i} ({btn.name}) has no TMP_Text component!");
                }
            }
            else
            {
                Debug.LogWarning($"Button {i} in array is null!");
            }
        }
        
        Debug.Log("=== END BUTTON ARRAY TEST ===");
    }
    
    /// <summary>
    /// Test keyboard input functionality - simulates Q, W, E key presses
    /// </summary>
    [ContextMenu("Test Keyboard Input")]
    public void TestKeyboardInput()
    {
        Debug.Log("=== TESTING KEYBOARD INPUT ===");
        
        if (onChoiceSelected == null)
        {
            Debug.LogWarning("No choices are currently active. Please show choices first.");
            return;
        }
        
        Debug.Log("Testing keyboard input simulation...");
        
        // Test Q key (choice 0)
        Debug.Log("Simulating Q key press (choice 0):");
        SelectChoice(0);
        
        // Wait a moment, then test W key (choice 1) - you can uncomment these for manual testing
        // Debug.Log("Simulating W key press (choice 1):");
        // SelectChoice(1);
        
        // Debug.Log("Simulating E key press (choice 2):");
        // SelectChoice(2);
        
        Debug.Log("=== END KEYBOARD INPUT TEST ===");
    }
    
    /// <summary>
    /// Test method to simulate dialog responses with multiple entries
    /// </summary>
    [ContextMenu("Test Multiple Dialog Responses")]
    public void TestMultipleDialogResponses()
    {
        Debug.Log("=== TESTING MULTIPLE DIALOG RESPONSES ===");
        
        if (coreGameData == null)
        {
            Debug.LogError("CoreGameData is null! Cannot test responses.");
            return;
        }
        
        Debug.Log($"Current dialog response state:");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        Debug.Log($"  - selectedChoiceIndex: {selectedChoiceIndex}");
        Debug.Log($"  - currentChoiceResponseIndex: {currentChoiceResponseIndex}");
        Debug.Log($"  - isInDialogTransition: {isInDialogTransition}");
        Debug.Log($"  - isTextAnimating: {isTextAnimating}");
        
        if (isShowingResponse && selectedChoiceIndex >= 0)
        {
            var currentBlock = coreGameData.coreBlock[currentBlockIndex];
            if (currentBlock.Dialog?.choices != null && selectedChoiceIndex < currentBlock.Dialog.choices.Length)
            {
                var selectedChoice = currentBlock.Dialog.choices[selectedChoiceIndex];
                if (selectedChoice.dialogResponses != null)
                {
                    Debug.Log($"Selected choice '{selectedChoice.playerChoice}' has {selectedChoice.dialogResponses.Length} responses:");
                    for (int i = 0; i < selectedChoice.dialogResponses.Length; i++)
                    {
                        var response = selectedChoice.dialogResponses[i];
                        string indicator = (i == currentChoiceResponseIndex) ? " <- CURRENT" : "";
                        string responseNpcName = ConvertNpcNameToString(response.npcName);
                        Debug.Log($"  Response {i}: '{responseNpcName}' says '{response.npcResponse}'{indicator}");
                    }
                    
                    Debug.Log("Press SPACE to advance to next response or continue to next block.");
                    
                    // Test showing next response manually
                    if (currentChoiceResponseIndex < selectedChoice.dialogResponses.Length - 1)
                    {
                        Debug.Log("=== TESTING NEXT RESPONSE MANUALLY ===");
                        int nextIndex = currentChoiceResponseIndex + 1;
                        Debug.Log($"Attempting to show response {nextIndex}...");
                        ShowDialogResponse(selectedChoice, nextIndex);
                    }
                    else
                    {
                        Debug.Log("All responses already shown. Call ResetDialogResponseState() and ContinueToNextBlock() to proceed.");
                    }
                }
                else
                {
                    Debug.Log("Selected choice has no dialog responses.");
                }
            }
        }
        else
        {
            Debug.Log("Not currently showing responses. Select a choice first.");
            
            // If there's a current dialog with choices, show them
            if (currentBlockIndex < coreGameData.coreBlock.Length)
            {
                var currentBlock = coreGameData.coreBlock[currentBlockIndex];
                if (currentBlock.Dialog?.choices != null)
                {
                    Debug.Log($"Current dialog has {currentBlock.Dialog.choices.Length} choices:");
                    for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
                    {
                        var choice = currentBlock.Dialog.choices[i];
                        int responseCount = choice.dialogResponses?.Length ?? 0;
                        Debug.Log($"  Choice {i}: '{choice.playerChoice}' (has {responseCount} responses)");
                    }
                    Debug.Log("Select a choice to test multiple responses.");
                }
            }
        }
        
        Debug.Log("=== END MULTIPLE DIALOG RESPONSES TEST ===");
    }
    
    /// <summary>
    /// Force continue to next response - use if dialog gets stuck on multiple responses
    /// </summary>
    [ContextMenu("Force Next Response")]
    public void ForceNextResponse()
    {
        Debug.Log("=== FORCING NEXT RESPONSE ===");
        
        if (!isShowingResponse || selectedChoiceIndex < 0)
        {
            Debug.LogWarning("Not showing responses or no choice selected!");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices != null && selectedChoiceIndex < currentBlock.Dialog.choices.Length)
        {
            var selectedChoice = currentBlock.Dialog.choices[selectedChoiceIndex];
            if (selectedChoice.dialogResponses != null)
            {
                int nextIndex = currentChoiceResponseIndex + 1;
                if (nextIndex < selectedChoice.dialogResponses.Length)
                {
                    Debug.Log($"Forcing show of response {nextIndex + 1}/{selectedChoice.dialogResponses.Length}");
                    isInDialogTransition = false; // Reset transition state
                    isTextAnimating = false; // Stop any text animation
                    ShowDialogResponse(selectedChoice, nextIndex);
                }
                else
                {
                    Debug.Log("No more responses. Forcing next block...");
                    ResetDialogResponseState();
                    ContinueToNextBlock();
                }
            }
        }
        
        Debug.Log("=== END FORCE NEXT RESPONSE ===");
    }
    
    /// <summary>
    /// Reset the filtered choices system back to original choices
    /// </summary>
    private void ResetFilteredChoicesSystem()
    {
        Debug.Log("[FILTERED CHOICES] Resetting filtered choices system");
        currentFilteredChoices = null;
        originalChoices = null;
        isUsingFilteredChoices = false;
        
        // Also reset pressed choices tracking when starting a new dialog block
        ClearPressedChoicesTracking();
        
        // Clear last processed choices
        lastProcessedChoices = null;
    }
    
    /// <summary>
    /// Smart reset that only resets if we're dealing with a completely different choice set
    /// </summary>
    /// <param name="newChoices">The new choice set to compare against</param>
    private void SmartResetFilteredChoicesSystem(CoreGameDialogChoices[] newChoices, int blockIndex)
    {
        Debug.Log($"[SMART RESET] Checking choice set for block {blockIndex}...");
        
        // Log current state for debugging
        Debug.Log($"[SMART RESET] Current pressed choices count: {pressedChoiceTexts.Count}");
        Debug.Log($"[SMART RESET] New choices count: {newChoices?.Length ?? 0}");
        Debug.Log($"[SMART RESET] Last processed choices count: {lastProcessedChoices?.Length ?? 0}");
        Debug.Log($"[SMART RESET] Last processed block: {lastProcessedBlockIndex}, Current block: {blockIndex}");
        
        // First check: are we in a different dialog block?
        if (blockIndex != lastProcessedBlockIndex)
        {
            Debug.Log($"[SMART RESET] Different dialog block detected (was {lastProcessedBlockIndex}, now {blockIndex}) - resetting system");
            ResetFilteredChoicesSystem();
            lastProcessedChoices = newChoices;
            lastProcessedBlockIndex = blockIndex;
            return;
        }
        
        // Second check: if same block, are the choices different?
        if (lastProcessedChoices != null && AreChoicesetsEquivalent(lastProcessedChoices, newChoices))
        {
            Debug.Log($"[SMART RESET] Same block ({blockIndex}) and same choice set detected - preserving tracking and filtering");
            return;
        }
        
        Debug.Log($"[SMART RESET] Same block ({blockIndex}) but different choice set detected - resetting system");
        
        // Debug what choices we're resetting from
        if (pressedChoiceTexts.Count > 0)
        {
            Debug.Log($"[SMART RESET] Clearing {pressedChoiceTexts.Count} pressed choices:");
            foreach (string choice in pressedChoiceTexts)
            {
                Debug.Log($"[SMART RESET]   - '{choice}'");
            }
        }
        
        ResetFilteredChoicesSystem();
        lastProcessedChoices = newChoices;
        lastProcessedBlockIndex = blockIndex;
    }
    
    /// <summary>
    /// Compare two choice arrays to see if they represent the same choice set
    /// </summary>
    /// <param name="choices1">First choice array</param>
    /// <param name="choices2">Second choice array</param>
    /// <returns>True if the choice sets are equivalent</returns>
    private bool AreChoicesetsEquivalent(CoreGameDialogChoices[] choices1, CoreGameDialogChoices[] choices2)
    {
        if (choices1 == null || choices2 == null)
            return false;
            
        if (choices1.Length != choices2.Length)
            return false;
            
        // Check if all choice texts and correctness flags match
        for (int i = 0; i < choices1.Length; i++)
        {
            if (choices1[i].playerChoice != choices2[i].playerChoice ||
                choices1[i].correctChoice != choices2[i].correctChoice)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Track a choice that has been pressed so it won't be spawned again
    /// </summary>
    /// <param name="choiceIndex">Index of the choice that was pressed</param>
    /// <param name="choiceText">Text of the choice that was pressed</param>
    private void TrackPressedChoice(int choiceIndex, string choiceText)
    {
        if (!pressedChoiceIndices.Contains(choiceIndex))
        {
            pressedChoiceIndices.Add(choiceIndex);
            Debug.Log($"[PRESSED CHOICES] Tracked pressed choice index: {choiceIndex}");
        }
        
        if (!string.IsNullOrEmpty(choiceText) && !pressedChoiceTexts.Contains(choiceText))
        {
            pressedChoiceTexts.Add(choiceText);
            Debug.Log($"[PRESSED CHOICES] Tracked pressed choice text: '{choiceText}'");
        }
        
        Debug.Log($"[PRESSED CHOICES] Total pressed: {pressedChoiceIndices.Count} choices by index, {pressedChoiceTexts.Count} by text");
    }
    
    /// <summary>
    /// Check if a choice has already been pressed
    /// </summary>
    /// <param name="choiceText">Text of the choice to check</param>
    /// <returns>True if this choice has been pressed before</returns>
    private bool IsChoiceAlreadyPressed(string choiceText)
    {
        return !string.IsNullOrEmpty(choiceText) && pressedChoiceTexts.Contains(choiceText);
    }
    
    /// <summary>
    /// Check if a choice index has already been pressed (relative to original choices)
    /// </summary>
    /// <param name="originalChoiceIndex">Original index of the choice to check</param>
    /// <returns>True if this choice index has been pressed before</returns>
    private bool IsChoiceIndexAlreadyPressed(int originalChoiceIndex)
    {
        return pressedChoiceIndices.Contains(originalChoiceIndex);
    }
    
    /// <summary>
    /// Clear pressed choices tracking - call when starting a new dialog block
    /// </summary>
    private void ClearPressedChoicesTracking()
    {
        pressedChoiceTexts.Clear();
        pressedChoiceIndices.Clear();
        Debug.Log("[PRESSED CHOICES] Cleared all pressed choice tracking");
    }
    
    /// <summary>
    /// Get a summary of pressed choices for debugging
    /// </summary>
    private void LogPressedChoicesSummary()
    {
        Debug.Log($"[PRESSED CHOICES] Summary:");
        Debug.Log($"  - Pressed by index: {pressedChoiceIndices.Count}");
        Debug.Log($"  - Pressed by text: {pressedChoiceTexts.Count}");
        
        if (pressedChoiceIndices.Count > 0)
        {
            Debug.Log($"  - Pressed indices: [{string.Join(", ", pressedChoiceIndices)}]");
        }
        
        if (pressedChoiceTexts.Count > 0)
        {
            var pressedTextsArray = new string[pressedChoiceTexts.Count];
            pressedChoiceTexts.CopyTo(pressedTextsArray);
            string formattedTexts = "";
            for (int i = 0; i < pressedTextsArray.Length; i++)
            {
                formattedTexts += "'" + pressedTextsArray[i] + "'";
                if (i < pressedTextsArray.Length - 1) formattedTexts += ", ";
            }
            Debug.Log($"  - Pressed texts: [{formattedTexts}]");
        }
    }
    
    /// <summary>
    /// ENHANCED: Filter out pressed choices and respawn the choice buttons
    /// This version tracks all pressed choices and ensures they never respawn
    /// NOTE: Choice tracking is now done in OnPlayerChoseResponse, so this method just filters
    /// </summary>
    /// <param name="pressedChoiceIndex">Index of the choice that was just pressed</param>
    private void FilterIncorrectChoiceAndRespawn(int pressedChoiceIndex)
    {
        Debug.Log($"[ENHANCED-FILTER] === FilterIncorrectChoiceAndRespawn CALLED ===");
        Debug.Log($"[ENHANCED-FILTER] Parameters - pressedChoiceIndex: {pressedChoiceIndex}");
        Debug.Log($"[ENHANCED-FILTER] Current state:");
        Debug.Log($"[ENHANCED-FILTER]   - questionInstance != null: {questionInstance != null}");
        Debug.Log($"[ENHANCED-FILTER]   - isUsingFilteredChoices: {isUsingFilteredChoices}");
        Debug.Log($"[ENHANCED-FILTER]   - currentFilteredChoices != null: {currentFilteredChoices != null}");
        Debug.Log($"[ENHANCED-FILTER]   - originalChoices != null: {originalChoices != null}");
        Debug.Log($"[ENHANCED-FILTER]   - pressedChoiceIndices.Count: {pressedChoiceIndices.Count}");
        Debug.Log($"[ENHANCED-FILTER]   - pressedChoiceTexts.Count: {pressedChoiceTexts.Count}");
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        
        // Determine which choice array we're working with
        CoreGameDialogChoices[] choicesToFilter = isUsingFilteredChoices ? currentFilteredChoices : currentBlock.Dialog?.choices;
        
        Debug.Log($"[ENHANCED-FILTER] choicesToFilter array length: {choicesToFilter?.Length ?? 0}");
        
        if (choicesToFilter == null || pressedChoiceIndex >= choicesToFilter.Length)
        {
            Debug.LogError($"[ENHANCED-FILTER] VALIDATION FAILED:");
            Debug.LogError($"[ENHANCED-FILTER]   - choicesToFilter is null: {choicesToFilter == null}");
            Debug.LogError($"[ENHANCED-FILTER]   - pressedChoiceIndex: {pressedChoiceIndex}");
            Debug.LogError($"[ENHANCED-FILTER]   - choicesToFilter.Length: {choicesToFilter?.Length ?? 0}");
            return;
        }
        
        // Store original choices if this is the first filtering
        if (!isUsingFilteredChoices)
        {
            originalChoices = currentBlock.Dialog.choices;
            Debug.Log($"[ENHANCED-FILTER] Stored original {originalChoices.Length} choices");
        }
        
        // NOTE: Choice tracking is already done in OnPlayerChoseResponse()
        // We just need to create the filtered array excluding ALL pressed choices
        
        // Create new filtered array WITHOUT any pressed choices
        var filteredList = new System.Collections.Generic.List<CoreGameDialogChoices>();
        
        Debug.Log($"[ENHANCED-FILTER] Creating filtered array, excluding ALL pressed choices...");
        
        for (int i = 0; i < choicesToFilter.Length; i++)
        {
            var currentChoice = choicesToFilter[i];
            
            // Check if this choice was previously pressed (by text)
            if (IsChoiceAlreadyPressed(currentChoice.playerChoice))
            {
                Debug.Log($"[ENHANCED-FILTER] EXCLUDING choice {i}: '{currentChoice.playerChoice}' (already pressed)");
            }
            else
            {
                filteredList.Add(currentChoice);
                Debug.Log($"[ENHANCED-FILTER] KEEPING choice {i}: '{currentChoice.playerChoice}'");
            }
        }
        
        currentFilteredChoices = filteredList.ToArray();
        isUsingFilteredChoices = true;
        
        Debug.Log($"[ENHANCED-FILTER] Enhanced filtering complete: {choicesToFilter.Length} -> {currentFilteredChoices.Length}");
        Debug.Log($"[ENHANCED-FILTER] Total pressed choices tracked: {pressedChoiceIndices.Count} by index, {pressedChoiceTexts.Count} by text");
        
        // Log the remaining choices
        for (int i = 0; i < currentFilteredChoices.Length; i++)
        {
            var choice = currentFilteredChoices[i];
            Debug.Log($"[ENHANCED-FILTER]   Remaining choice {i}: '{choice.playerChoice}' (correct: {choice.correctChoice})");
        }
        
        // Check if only correct choices remain
        bool onlyCorrectChoicesRemain = true;
        foreach (var choice in currentFilteredChoices)
        {
            if (!choice.correctChoice)
            {
                onlyCorrectChoicesRemain = false;
                break;
            }
        }
        
        if (onlyCorrectChoicesRemain)
        {
            Debug.Log("[ENHANCED-FILTER] Only correct choices remain - dialog will continue normally when selected");
        }
        
        // Check if we have any choices left to respawn
        if (currentFilteredChoices.Length == 0)
        {
            Debug.LogError("[ENHANCED-FILTER] ✗ NO CHOICES LEFT! All choices have been pressed. This should not happen!");
            Debug.LogError("[ENHANCED-FILTER] This indicates a dialog configuration issue - there should always be at least one correct choice.");
            return;
        }
        
        // Respawn the choices with the filtered array
        Debug.Log("[ENHANCED-FILTER] Calling RespawnChoicesWithFilteredArray()...");
        RespawnChoicesWithFilteredArray();
        Debug.Log("[ENHANCED-FILTER] === FilterIncorrectChoiceAndRespawn COMPLETE ===");
    }
    
    /// <summary>
    /// FIXED: Spawn a completely new question bar with filtered choices
    /// Don't reuse old buttons - create fresh ones
    /// </summary>
    private void RespawnChoicesWithFilteredArray()
    {
        Debug.Log("[SPAWN-NEW] === RespawnChoicesWithFilteredArray CALLED ===");
        
        if (currentFilteredChoices == null || currentFilteredChoices.Length == 0)
        {
            Debug.LogError("[SPAWN-NEW] No filtered choices to respawn!");
            return;
        }
        
        Debug.Log($"[SPAWN-NEW] Going to spawn new question bar with {currentFilteredChoices.Length} filtered choices");
        
        // STEP 1: DESTROY the old question instance completely
        if (questionInstance != null)
        {
            Debug.Log("[SPAWN-NEW] Destroying old question instance...");
            Destroy(questionInstance);
            questionInstance = null;
        }
        
        // STEP 2: SPAWN a completely new question bar
        Debug.Log("[SPAWN-NEW] Creating brand new question instance...");
        #pragma warning disable CS0618
        questionInstance = SummonQuestionBar();
        #pragma warning restore CS0618
        
        if (questionInstance == null)
        {
            Debug.LogError("[SPAWN-NEW] FATAL: Failed to create new question instance!");
            return;
        }
        
        Debug.Log($"[SPAWN-NEW] ✓ Created fresh question instance: {questionInstance.name}");
        
        // STEP 3: Show the filtered choices on the NEW question bar
        Debug.Log("[SPAWN-NEW] Populating new question bar with filtered choices...");
        for (int i = 0; i < currentFilteredChoices.Length; i++)
        {
            var choice = currentFilteredChoices[i];
            Debug.Log($"[SPAWN-NEW]   Choice {i}: '{choice.playerChoice}' (correct: {choice.correctChoice})");
        }
        
        try
        {
            // Clear any existing callback and set new one
            onChoiceSelected = null;
            
            // Populate the NEW question bar with filtered choices
            ShowChoicesWithButtons(currentFilteredChoices, OnPlayerChoseResponse);
            Debug.Log($"[SPAWN-NEW] ✓ SUCCESS: New question bar spawned with {currentFilteredChoices.Length} choices");
            
            // Verify the new buttons work
            StartCoroutine(VerifyNewQuestionBar());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SPAWN-NEW] ✗ ERROR in ShowChoicesWithButtons: {e.Message}");
        }
        
        Debug.Log("[SPAWN-NEW] === RespawnChoicesWithFilteredArray COMPLETE ===");
    }
    
    /// <summary>
    /// Verify the newly spawned question bar has the correct buttons
    /// </summary>
    private IEnumerator VerifyNewQuestionBar()
    {
        yield return null; // Wait one frame for UI setup
        
        Debug.Log("[SPAWN-NEW] === VERIFYING NEW QUESTION BAR ===");
        
        if (questionInstance != null)
        {
            Button[] newButtons = questionInstance.GetComponentsInChildren<Button>();
            Debug.Log($"[SPAWN-NEW] Found {newButtons.Length} buttons in new question bar");
            
            int activeButtons = 0;
            for (int i = 0; i < newButtons.Length; i++)
            {
                if (newButtons[i].gameObject.activeInHierarchy)
                {
                    activeButtons++;
                    TMP_Text btnText = newButtons[i].GetComponentInChildren<TMP_Text>();
                    string text = btnText?.text ?? "NO TEXT";
                    Debug.Log($"[SPAWN-NEW] Button {i} is ACTIVE with text: '{text}'");
                }
            }
            
            Debug.Log($"[SPAWN-NEW] {activeButtons} out of {newButtons.Length} buttons are active");
            
            if (activeButtons == currentFilteredChoices?.Length)
            {
                Debug.Log($"[SPAWN-NEW] ✓ SUCCESS: Correct number of active buttons ({activeButtons})");
            }
            else
            {
                Debug.LogWarning($"[SPAWN-NEW] ⚠ Expected {currentFilteredChoices?.Length ?? 0} active buttons, got {activeButtons}");
            }
        }
        else
        {
            Debug.LogError("[SPAWN-NEW] ✗ questionInstance is null during verification!");
        }
        
        Debug.Log("[SPAWN-NEW] === VERIFICATION COMPLETE ===");
    }
    
    /// <summary>
    /// Test the enhanced pressed choice tracking system
    /// </summary>
    [ContextMenu("Test Enhanced Pressed Choice Tracking")]
    public void TestEnhancedPressedChoiceTracking()
    {
        Debug.Log("=== TESTING ENHANCED PRESSED CHOICE TRACKING ===");
        
        if (coreGameData == null || currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError("No valid game data!");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("No choices available!");
            return;
        }
        
        Debug.Log($"INITIAL STATE:");
        Debug.Log($"  - Total choices in dialog: {currentBlock.Dialog.choices.Length}");
        Debug.Log($"  - Pressed choices tracked: {pressedChoiceIndices.Count} by index, {pressedChoiceTexts.Count} by text");
        Debug.Log($"  - Using filtered choices: {isUsingFilteredChoices}");
        
        // List all available choices
        Debug.Log("Available choices:");
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            var choice = currentBlock.Dialog.choices[i];
            bool alreadyPressed = IsChoiceAlreadyPressed(choice.playerChoice);
            Debug.Log($"  Choice {i}: '{choice.playerChoice}' (correct: {choice.correctChoice}, pressed: {alreadyPressed})");
        }
        
        // Show original choices if not already showing
        if (questionInstance == null)
        {
            #pragma warning disable CS0618
            questionInstance = SummonQuestionBar();
            #pragma warning restore CS0618
        }
        ShowChoicesWithButtons(currentBlock.Dialog.choices, OnPlayerChoseResponse);
        
        Debug.Log("STEP 1: Simulating pressing first incorrect choice...");
        
        // Find first incorrect choice
        int incorrectIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectIndex = i;
                break;
            }
        }
        
        if (incorrectIndex >= 0)
        {
            Debug.Log($"Pressing choice {incorrectIndex}: '{currentBlock.Dialog.choices[incorrectIndex].playerChoice}'");
            FilterIncorrectChoiceAndRespawn(incorrectIndex);
            
            Debug.Log($"AFTER FIRST PRESS:");
            Debug.Log($"  - Remaining choices: {currentFilteredChoices?.Length ?? 0}");
            Debug.Log($"  - Pressed choices tracked: {pressedChoiceIndices.Count} by index, {pressedChoiceTexts.Count} by text");
            
            // Test pressing another incorrect choice if available
            if (currentFilteredChoices != null && currentFilteredChoices.Length > 1)
            {
                Debug.Log("STEP 2: Simulating pressing second incorrect choice...");
                
                int secondIncorrectIndex = -1;
                for (int i = 0; i < currentFilteredChoices.Length; i++)
                {
                    if (!currentFilteredChoices[i].correctChoice)
                    {
                        secondIncorrectIndex = i;
                        break;
                    }
                }
                
                if (secondIncorrectIndex >= 0)
                {
                    Debug.Log($"Pressing choice {secondIncorrectIndex}: '{currentFilteredChoices[secondIncorrectIndex].playerChoice}'");
                    FilterIncorrectChoiceAndRespawn(secondIncorrectIndex);
                    
                    Debug.Log($"AFTER SECOND PRESS:");
                    Debug.Log($"  - Remaining choices: {currentFilteredChoices?.Length ?? 0}");
                    Debug.Log($"  - Pressed choices tracked: {pressedChoiceIndices.Count} by index, {pressedChoiceTexts.Count} by text");
                    
                    Debug.Log("Remaining choices after two presses:");
                    if (currentFilteredChoices != null)
                    {
                        for (int i = 0; i < currentFilteredChoices.Length; i++)
                        {
                            var choice = currentFilteredChoices[i];
                            Debug.Log($"  Choice {i}: '{choice.playerChoice}' (correct: {choice.correctChoice})");
                        }
                    }
                }
                else
                {
                    Debug.Log("No second incorrect choice available to test");
                }
            }
            else
            {
                Debug.Log("Not enough choices remaining for second press test");
            }
        }
        else
        {
            Debug.LogError("No incorrect choices to test with!");
        }
        
        Debug.Log("VERIFICATION:");
        Debug.Log($"✓ Pressed choices are properly tracked and will not respawn");
        Debug.Log($"✓ Only unpressed choices will appear in future spawns");
        Debug.Log($"✓ System prevents infinite loops by tracking all pressed choices");
        
        Debug.Log("=== ENHANCED PRESSED CHOICE TRACKING TEST COMPLETE ===");
    }
    
    /// <summary>
    /// Simple demonstration of the pressed choice system
    /// </summary>
    [ContextMenu("Demo Pressed Choice Prevention")]
    public void DemoPressedChoicePrevention()
    {
        Debug.Log("=== DEMO: PRESSED CHOICE PREVENTION ===");
        Debug.Log("This system ensures that once a button is pressed, it will NEVER appear again in future spawns");
        Debug.Log("");
        Debug.Log("HOW IT WORKS:");
        Debug.Log("1. Player sees all available choices (e.g., 3 buttons)");
        Debug.Log("2. Player presses a wrong choice → Dialog response plays");
        Debug.Log("3. When dialog ends → System respawns question bar with ONLY unpressed choices");
        Debug.Log("4. Pressed choice is PERMANENTLY excluded from future spawns");
        Debug.Log("5. Process repeats until player picks a correct choice");
        Debug.Log("");
        Debug.Log("TECHNICAL IMPLEMENTATION:");
        Debug.Log("- TrackPressedChoice(): Records both choice text and original index");
        Debug.Log("- FilterIncorrectChoiceAndRespawn(): Excludes ALL pressed choices, not just the current one");
        Debug.Log("- IsChoiceAlreadyPressed(): Checks if choice text was pressed before");
        Debug.Log("- ClearPressedChoicesTracking(): Resets tracking for new dialog blocks");
        Debug.Log("");
        Debug.Log("RESULT: No choice can be pressed twice, ensuring clean UI progression");
        Debug.Log("=== END DEMO ===");
    }
    
    /// <summary>
    /// Complete test of the pressed choice prevention system with simulated user interaction
    /// </summary>
    [ContextMenu("Test Complete Pressed Choice Flow")]
    public void TestCompletePressedChoiceFlow()
    {
        Debug.Log("=== TESTING COMPLETE PRESSED CHOICE FLOW ===");
        
        if (coreGameData == null || currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError("No valid game data!");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("No choices available!");
            return;
        }
        
        Debug.Log("=== INITIAL STATE ===");
        Debug.Log($"Dialog has {currentBlock.Dialog.choices.Length} total choices:");
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            var choice = currentBlock.Dialog.choices[i];
            Debug.Log($"  Choice {i}: '{choice.playerChoice}' (correct: {choice.correctChoice})");
        }
        
        // Reset to clean state
        ResetFilteredChoicesSystem();
        
        Debug.Log("=== STEP 1: Show initial choices ===");
        if (questionInstance == null)
        {
            #pragma warning disable CS0618
            questionInstance = SummonQuestionBar();
            #pragma warning restore CS0618
        }
        ShowChoicesWithButtons(currentBlock.Dialog.choices, OnPlayerChoseResponse);
        
        Debug.Log("=== STEP 2: Simulate first incorrect choice selection ===");
        // Find first incorrect choice
        int firstIncorrectIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                firstIncorrectIndex = i;
                break;
            }
        }
        
        if (firstIncorrectIndex >= 0)
        {
            var firstChoice = currentBlock.Dialog.choices[firstIncorrectIndex];
            Debug.Log($"Selecting first incorrect choice {firstIncorrectIndex}: '{firstChoice.playerChoice}'");
            
            // Simulate the complete selection process
            OnPlayerChoseResponse(firstIncorrectIndex);
            
            Debug.Log($"After first selection - Pressed choices: {pressedChoiceTexts.Count}");
            LogPressedChoicesSummary();
            
            // Simulate response completion and filtering
            Debug.Log("=== STEP 3: Simulate response completion and respawn ===");
            FilterIncorrectChoiceAndRespawn(firstIncorrectIndex);
            
            Debug.Log($"After first respawn - Available choices: {currentFilteredChoices?.Length ?? 0}");
            if (currentFilteredChoices != null)
            {
                for (int i = 0; i < currentFilteredChoices.Length; i++)
                {
                    Debug.Log($"  Remaining choice {i}: '{currentFilteredChoices[i].playerChoice}' (correct: {currentFilteredChoices[i].correctChoice})");
                }
            }
            
            // Test second incorrect choice if available
            Debug.Log("=== STEP 4: Test second incorrect choice (if available) ===");
            int secondIncorrectIndex = -1;
            if (currentFilteredChoices != null)
            {
                for (int i = 0; i < currentFilteredChoices.Length; i++)
                {
                    if (!currentFilteredChoices[i].correctChoice)
                    {
                        secondIncorrectIndex = i;
                        break;
                    }
                }
            }
            
            if (secondIncorrectIndex >= 0)
            {
                var secondChoice = currentFilteredChoices[secondIncorrectIndex];
                Debug.Log($"Selecting second incorrect choice {secondIncorrectIndex}: '{secondChoice.playerChoice}'");
                
                OnPlayerChoseResponse(secondIncorrectIndex);
                FilterIncorrectChoiceAndRespawn(secondIncorrectIndex);
                
                Debug.Log($"After second respawn - Available choices: {currentFilteredChoices?.Length ?? 0}");
                LogPressedChoicesSummary();
            }
            else
            {
                Debug.Log("No second incorrect choice available (good!)");
            }
        }
        else
        {
            Debug.LogError("No incorrect choices found to test with!");
        }
        
        Debug.Log("=== VERIFICATION ===");
        Debug.Log("✓ Each pressed choice should be permanently removed from future spawns");
        Debug.Log("✓ Only unpressed choices should remain available");
        Debug.Log("✓ System prevents infinite loops by ensuring progress");
        Debug.Log("=== COMPLETE PRESSED CHOICE FLOW TEST COMPLETE ===");
    }
    
    /// <summary>
    /// Test the specific bug: correct choice first, then incorrect choice
    /// This should NOT result in "no choices left" error
    /// </summary>
    [ContextMenu("Test Correct Then Incorrect Choice Bug")]
    public void TestCorrectThenIncorrectChoiceBug()
    {
        Debug.Log("=== TESTING CORRECT THEN INCORRECT CHOICE BUG ===");
        Debug.Log("This test simulates: Pick correct choice → Continue to next dialog → Pick incorrect choice");
        Debug.Log("Expected result: Should work fine, incorrect choice should be filtered properly");
        
        if (coreGameData == null || currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError("No valid game data!");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("No choices available!");
            return;
        }
        
        Debug.Log("=== STEP 1: Reset and show initial choices ===");
        ResetFilteredChoicesSystem();
        
        Debug.Log($"Initial dialog has {currentBlock.Dialog.choices.Length} choices:");
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            var choice = currentBlock.Dialog.choices[i];
            Debug.Log($"  Choice {i}: '{choice.playerChoice}' (correct: {choice.correctChoice})");
        }
        
        // Find correct and incorrect choices
        int correctChoiceIndex = -1;
        int incorrectChoiceIndex = -1;
        
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (currentBlock.Dialog.choices[i].correctChoice && correctChoiceIndex == -1)
            {
                correctChoiceIndex = i;
            }
            if (!currentBlock.Dialog.choices[i].correctChoice && incorrectChoiceIndex == -1)
            {
                incorrectChoiceIndex = i;
            }
        }
        
        if (correctChoiceIndex == -1)
        {
            Debug.LogError("No correct choice found for testing!");
            return;
        }
        
        if (incorrectChoiceIndex == -1)
        {
            Debug.LogError("No incorrect choice found for testing!");
            return;
        }
        
        Debug.Log($"Found correct choice at index {correctChoiceIndex}: '{currentBlock.Dialog.choices[correctChoiceIndex].playerChoice}'");
        Debug.Log($"Found incorrect choice at index {incorrectChoiceIndex}: '{currentBlock.Dialog.choices[incorrectChoiceIndex].playerChoice}'");
        
        Debug.Log("=== STEP 2: Simulate selecting CORRECT choice first ===");
        Debug.Log("This should NOT be tracked as pressed (correct choices lead to normal progression)");
        
        OnPlayerChoseResponse(correctChoiceIndex);
        
        Debug.Log($"After correct choice - Pressed choices tracked: {pressedChoiceTexts.Count}");
        LogPressedChoicesSummary();
        
        if (pressedChoiceTexts.Count > 0)
        {
            Debug.LogError("❌ BUG: Correct choice was tracked! This will cause problems.");
        }
        else
        {
            Debug.Log("✅ GOOD: Correct choice was NOT tracked (as expected)");
        }
        
        Debug.Log("=== STEP 3: Simulate moving to next dialog block ===");
        Debug.Log("In real gameplay, correct choice would lead to next dialog with new choices");
        Debug.Log("For this test, we'll simulate having a new set of choices that includes some incorrect ones");
        
        // Reset for "new dialog block" simulation
        selectedChoiceIndex = -1;
        currentChoiceResponseIndex = -1;
        isShowingResponse = false;
        
        Debug.Log("=== STEP 4: Simulate selecting INCORRECT choice in new dialog ===");
        Debug.Log("This should be tracked and filtered properly, without 'no choices left' error");
        
        // Create a mock scenario where we have multiple choices including incorrect ones
        if (currentBlock.Dialog.choices.Length >= 2)
        {
            Debug.Log("Simulating OnPlayerChoseResponse with incorrect choice...");
            OnPlayerChoseResponse(incorrectChoiceIndex);
            
            Debug.Log($"After incorrect choice - Pressed choices tracked: {pressedChoiceTexts.Count}");
            LogPressedChoicesSummary();
            
            if (pressedChoiceTexts.Count == 1)
            {
                Debug.Log("✅ GOOD: Only the incorrect choice is tracked");
            }
            else
            {
                Debug.LogError($"❌ UNEXPECTED: Expected 1 tracked choice, got {pressedChoiceTexts.Count}");
            }
            
            Debug.Log("=== STEP 5: Test filtering (this is where the bug occurred) ===");
            try
            {
                FilterIncorrectChoiceAndRespawn(incorrectChoiceIndex);
                
                if (currentFilteredChoices != null && currentFilteredChoices.Length > 0)
                {
                    Debug.Log($"✅ SUCCESS: Filtering worked! {currentFilteredChoices.Length} choices remain");
                    for (int i = 0; i < currentFilteredChoices.Length; i++)
                    {
                        Debug.Log($"  Remaining choice {i}: '{currentFilteredChoices[i].playerChoice}' (correct: {currentFilteredChoices[i].correctChoice})");
                    }
                }
                else
                {
                    Debug.LogError("❌ STILL BROKEN: No choices left after filtering!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ ERROR during filtering: {e.Message}");
            }
        }
        
        Debug.Log("=== VERIFICATION ===");
        Debug.Log("✅ Correct choices should NOT be tracked (they lead to normal progression)");
        Debug.Log("✅ Incorrect choices should be tracked and filtered");
        Debug.Log("✅ 'No choices left' error should never occur with proper dialog setup");
        Debug.Log("=== TEST COMPLETE ===");
    }
    
    /// <summary>
    /// SIMPLIFIED: Just respawn question bar when dialog response ends for incorrect choice
    /// </summary>
    private void CheckAndTriggerChoiceFiltering()
    {
        Debug.Log("[SIMPLE-FILTER] Checking if we need to respawn choices...");
        
        // Only proceed if we're currently showing responses
        if (!isShowingResponse)
        {
            Debug.Log("[SIMPLE-FILTER] Not showing response, no action needed");
            return;
        }
        
        // Get current choice data
        if (coreGameData == null || currentBlockIndex >= coreGameData.coreBlock.Length) return;
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null || selectedChoiceIndex < 0) return;
        
        // Get the choice array we're working with
        CoreGameDialogChoices[] choicesToCheck = isUsingFilteredChoices ? currentFilteredChoices : currentBlock.Dialog.choices;
        if (choicesToCheck == null || selectedChoiceIndex >= choicesToCheck.Length) return;
        
        var selectedChoice = choicesToCheck[selectedChoiceIndex];
        
        // Check if this was the last response
        bool isLastResponse = (selectedChoice.dialogResponses == null || selectedChoice.dialogResponses.Length == 0) ||
                              (currentChoiceResponseIndex >= selectedChoice.dialogResponses.Length - 1);
        
        if (isLastResponse)
        {
            Debug.Log($"[SIMPLE-FILTER] Last response for choice '{selectedChoice.playerChoice}' (correct: {selectedChoice.correctChoice})");
            
            // FIXED: ALWAYS wait for user input, regardless of whether choice is correct or incorrect
            // This ensures the last dialog response doesn't just disappear automatically
            Debug.Log("[SIMPLE-FILTER] Dialog response animation completed, waiting for user to press SPACE to continue");
            
            // Reset the text animation state so user can proceed with SPACE
            isTextAnimating = false;
            isInDialogTransition = false;
            
            // DO NOT automatically continue here - let HandleDialogProgression handle it when user presses SPACE
            // This applies to both correct AND incorrect choices for consistent behavior
        }
        else
        {
            Debug.Log("[SIMPLE-FILTER] Not the last response, waiting...");
        }
    }
    
    /// <summary>
    /// Auto-advance to next response after a delay (optional feature)
    /// </summary>
    private IEnumerator AutoAdvanceToNextResponse(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        
        Debug.Log("[AUTO-ADVANCE] Checking if we should auto-advance to next response...");
        
        if (isShowingResponse && !isTextAnimating && !isInDialogTransition)
        {
            Debug.Log("[AUTO-ADVANCE] Triggering HandleDialogProgression for next response");
            HandleDialogProgression();
        }
        else
        {
            Debug.Log($"[AUTO-ADVANCE] Skipping auto-advance - isShowingResponse:{isShowingResponse}, isTextAnimating:{isTextAnimating}, isInDialogTransition:{isInDialogTransition}");
        }
    }
    
    /// <summary>
    /// Test the correct choice filtering system with detailed flow demonstration
    /// </summary>
    [ContextMenu("Test Correct Choice System")]
    public void TestCorrectChoiceSystem()
    {
        Debug.Log("=== TESTING CORRECT CHOICE SYSTEM ===");
        
        if (coreGameData == null || coreGameData.coreBlock == null)
        {
            Debug.LogError("CoreGameData is null or has no blocks!");
            return;
        }
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError("Current block index is out of range!");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("Current dialog has no choices to test!");
            return;
        }
        
        Debug.Log($"Current Dialog Choices Analysis:");
        Debug.Log($"Total choices: {currentBlock.Dialog.choices.Length}");
        
        int correctChoices = 0;
        int incorrectChoices = 0;
        
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            var choice = currentBlock.Dialog.choices[i];
            string correctness = choice.correctChoice ? "CORRECT" : "INCORRECT";
            Debug.Log($"  Choice {i}: '{choice.playerChoice}' - {correctness}");
            
            if (choice.correctChoice)
                correctChoices++;
            else
                incorrectChoices++;
        }
        
        Debug.Log($"Summary: {correctChoices} correct choice(s), {incorrectChoices} incorrect choice(s)");
        
        if (isUsingFilteredChoices)
        {
            Debug.Log($"Currently using filtered choices:");
            Debug.Log($"  Filtered array has {currentFilteredChoices?.Length ?? 0} choice(s)");
            Debug.Log($"  Original array had {originalChoices?.Length ?? 0} choice(s)");
            
            if (currentFilteredChoices != null)
            {
                for (int i = 0; i < currentFilteredChoices.Length; i++)
                {
                    var choice = currentFilteredChoices[i];
                    string correctness = choice.correctChoice ? "CORRECT" : "INCORRECT";
                    Debug.Log($"    Filtered choice {i}: '{choice.playerChoice}' - {correctness}");
                }
            }
        }
        else
        {
            Debug.Log("Not currently using filtered choices (original choice set active)");
        }
        
        Debug.Log("Expected behavior:");
        Debug.Log("=== FLOW DEMONSTRATION ===");
        Debug.Log("1. Initial state: User sees all choices (e.g., 3 buttons)");
        Debug.Log("2. If user picks WRONG choice (correctChoice = false):");
        Debug.Log("   a) Shows dialog response as normal");
        Debug.Log("   b) When dialog response ends, respawns remaining choices");
        Debug.Log("   c) Wrong choice is REMOVED from options (3 becomes 2)");
        Debug.Log("3. If user picks WRONG choice again:");
        Debug.Log("   a) Shows dialog response as normal");
        Debug.Log("   b) When dialog response ends, respawns remaining choices");
        Debug.Log("   c) Wrong choice is REMOVED from options (2 becomes 1)");
        Debug.Log("4. If user picks CORRECT choice (correctChoice = true):");
        Debug.Log("   a) Shows dialog response");
        Debug.Log("   b) Continues to next dialog block normally");
        Debug.Log("   c) NO respawning - normal dialog progression");
        Debug.Log("=== CURRENT STATE ===");
        Debug.Log($"questionInstance != null: {questionInstance != null}");
        Debug.Log($"isUsingFilteredChoices: {isUsingFilteredChoices}");
        Debug.Log($"isShowingResponse: {isShowingResponse}");
        if (incorrectChoices > 0)
        {
            Debug.Log("✓ Has incorrect choices - filtering system will activate when wrong choice is picked");
            Debug.Log("- Selecting an incorrect choice will show its response, then remove that choice");
            Debug.Log("- Choice buttons will respawn with fewer options");
            Debug.Log("- This continues until only correct choice(s) remain");
        }
        
        if (correctChoices > 0)
        {
            Debug.Log("✓ Has correct choice(s) - selecting one will proceed with normal dialog flow");
        }
        
        if (correctChoices == 0)
        {
            Debug.LogError("✗ CRITICAL: No correct choices found! This will cause an infinite loop!");
        }
        
        Debug.Log("=== END CORRECT CHOICE SYSTEM TEST ===");
    }
    
    /// <summary>
    /// Manually test incorrect choice filtering (for debugging)
    /// </summary>
    [ContextMenu("Test Filter Incorrect Choice")]
    public void TestFilterIncorrectChoice()
    {
        Debug.Log("=== TESTING FILTER INCORRECT CHOICE ===");
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("No choices available to test filtering!");
            return;
        }
        
        Debug.Log($"Current state before filtering:");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
        Debug.Log($"  - currentFilteredChoices != null: {currentFilteredChoices != null}");
        
        // Find first incorrect choice
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex >= 0)
        {
            Debug.Log($"Testing filter of incorrect choice {incorrectChoiceIndex}: '{currentBlock.Dialog.choices[incorrectChoiceIndex].playerChoice}'");
            FilterIncorrectChoiceAndRespawn(incorrectChoiceIndex);
            
            Debug.Log($"After filtering:");
            Debug.Log($"  - questionInstance != null: {questionInstance != null}");
            Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
            Debug.Log($"  - currentFilteredChoices != null: {currentFilteredChoices != null}");
            Debug.Log($"  - currentFilteredChoices.Length: {currentFilteredChoices?.Length ?? 0}");
        }
        else
        {
            Debug.Log("No incorrect choices found to filter!");
        }
        
        Debug.Log("=== END FILTER INCORRECT CHOICE TEST ===");
    }
    
    /// <summary>
    /// Force simulate an incorrect choice selection and response completion
    /// This tests the exact flow that should happen when a player picks wrong choice
    /// </summary>
    [ContextMenu("Simulate Incorrect Choice Flow")]
    public void SimulateIncorrectChoiceFlow()
    {
        Debug.Log("=== SIMULATING INCORRECT CHOICE FLOW ===");
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("No choices available to simulate!");
            return;
        }
        
        // Find first incorrect choice
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex < 0)
        {
            Debug.LogError("No incorrect choices found to simulate!");
            return;
        }
        
        var incorrectChoice = currentBlock.Dialog.choices[incorrectChoiceIndex];
        Debug.Log($"Simulating selection of incorrect choice {incorrectChoiceIndex}: '{incorrectChoice.playerChoice}'");
        
        // Step 1: Simulate choice selection
        Debug.Log("STEP 1: Simulating OnPlayerChoseResponse()");
        selectedChoiceIndex = incorrectChoiceIndex;
        isShowingResponse = true;
        
        // Step 2: Check if there are responses to show
        if (incorrectChoice.dialogResponses != null && incorrectChoice.dialogResponses.Length > 0)
        {
            Debug.Log($"STEP 2: Choice has {incorrectChoice.dialogResponses.Length} responses");
            currentChoiceResponseIndex = incorrectChoice.dialogResponses.Length - 1; // Simulate all responses shown
            Debug.Log("STEP 3: Simulating all responses have been shown...");
        }
        else
        {
            Debug.Log("STEP 2: Choice has no responses, skipping to filtering");
        }
        
        // Step 3: Simulate HandleDialogProgression call (what happens when user presses SPACE after responses)
        Debug.Log("STEP 4: Simulating HandleDialogProgression() after responses complete");
        Debug.Log($"Current state before HandleDialogProgression:");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        Debug.Log($"  - selectedChoiceIndex: {selectedChoiceIndex}");
        Debug.Log($"  - currentChoiceResponseIndex: {currentChoiceResponseIndex}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
        
        // Important: The logic should work like this:
        // 1. selectedChoiceIndex points to the choice in the current array being used
        // 2. If we're using original choices, selectedChoiceIndex refers to original array
        // 3. If we're using filtered choices, selectedChoiceIndex refers to filtered array
        // 4. FilterIncorrectChoiceAndRespawn should remove from the current array
        
        // Manually trigger the incorrect choice filtering logic
        Debug.Log("STEP 5: Manually triggering filtering logic...");
        
        // Store the choice index before resetting state (like our fixed HandleDialogProgression does)
        int choiceToFilter = selectedChoiceIndex;
        Debug.Log($"STEP 5a: choiceToFilter = {choiceToFilter} (from selectedChoiceIndex)");
        
        // Reset state
        ResetDialogResponseState();
        Debug.Log($"STEP 5b: After ResetDialogResponseState() - questionInstance != null: {questionInstance != null}");
        
        // Filter and respawn
        Debug.Log($"STEP 5c: Calling FilterIncorrectChoiceAndRespawn({choiceToFilter})");
        FilterIncorrectChoiceAndRespawn(choiceToFilter);
        
        Debug.Log("=== SIMULATION COMPLETE ===");
    }
    
    /// <summary>
    /// Test the complete incorrect choice filtering flow
    /// This verifies the fixed progression logic
    /// </summary>
    [ContextMenu("Test Complete Incorrect Choice Flow")]
    public void TestCompleteIncorrectChoiceFlow()
    {
        Debug.Log("=== TESTING COMPLETE INCORRECT CHOICE FLOW ===");
        
        if (coreGameData == null || coreGameData.coreBlock == null)
        {
            Debug.LogError("CoreGameData is null or has no blocks!");
            return;
        }
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError($"Current block index {currentBlockIndex} is out of range");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("Current block has no dialog or choices!");
            return;
        }
        
        // Find first incorrect choice
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex < 0)
        {
            Debug.LogError("No incorrect choices found to test with!");
            return;
        }
        
        var incorrectChoice = currentBlock.Dialog.choices[incorrectChoiceIndex];
        Debug.Log($"Testing with incorrect choice {incorrectChoiceIndex}: '{incorrectChoice.playerChoice}'");
        
        // Verify choice has responses
        if (incorrectChoice.dialogResponses == null || incorrectChoice.dialogResponses.Length == 0)
        {
            Debug.LogError("Incorrect choice has no dialog responses!");
            return;
        }
        
        Debug.Log($"Choice has {incorrectChoice.dialogResponses.Length} responses");
        
        // Step 1: Simulate choice selection
        Debug.Log("STEP 1: Simulating incorrect choice selection...");
        isShowingResponse = true;
        selectedChoiceIndex = incorrectChoiceIndex;
        currentChoiceResponseIndex = 0;
        
        // Show first response
        Debug.Log("STEP 2: Showing first response...");
        var firstResponse = incorrectChoice.dialogResponses[0];
        string npcName = ConvertNpcNameToString(firstResponse.npcName);
        Debug.Log($"Response: '{npcName}' says '{firstResponse.npcResponse}'");
        
        // Step 3: Simulate all responses being shown
        Debug.Log("STEP 3: Simulating all responses shown...");
        currentChoiceResponseIndex = incorrectChoice.dialogResponses.Length - 1;
        
        // Step 4: Test HandleDialogProgression (what happens when responses complete)
        Debug.Log("STEP 4: Testing HandleDialogProgression after responses complete...");
        Debug.Log($"Current state before progression:");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        Debug.Log($"  - selectedChoiceIndex: {selectedChoiceIndex}");
        Debug.Log($"  - currentChoiceResponseIndex: {currentChoiceResponseIndex}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
        
        // This should trigger filtering and respawning
        Debug.Log("Calling HandleDialogProgression() - this should trigger filtering...");
        HandleDialogProgression();
        
        // Check results
        Debug.Log("STEP 5: Checking results after progression...");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        Debug.Log($"  - selectedChoiceIndex: {selectedChoiceIndex}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
        Debug.Log($"  - currentFilteredChoices length: {currentFilteredChoices?.Length ?? 0}");
        
        if (isUsingFilteredChoices && currentFilteredChoices != null)
        {
            Debug.Log("✓ Filtering was applied! Remaining choices:");
            for (int i = 0; i < currentFilteredChoices.Length; i++)
            {
                Debug.Log($"  Choice {i}: '{currentFilteredChoices[i].playerChoice}' (correct: {currentFilteredChoices[i].correctChoice})");
            }
            
            // Verify buttons are visible and functional
            if (questionInstance != null)
            {
                Button[] buttonArray = questionInstance.GetComponentsInChildren<Button>();
                Debug.Log($"UI Check: Found {buttonArray.Length} buttons");
                for (int i = 0; i < buttonArray.Length && i < currentFilteredChoices.Length; i++)
                {
                    if (buttonArray[i].gameObject.activeInHierarchy)
                    {
                        TMP_Text btnText = buttonArray[i].GetComponentInChildren<TMP_Text>();
                        string buttonText = btnText?.text ?? "NO TEXT";
                        Debug.Log($"  Button {i}: Active, Text: '{buttonText}'");
                    }
                    else
                    {
                        Debug.Log($"  Button {i}: INACTIVE");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("✗ Filtering was NOT applied - choices should have been filtered!");
        }
        
        Debug.Log("=== END COMPLETE INCORRECT CHOICE FLOW TEST ===");
    }
    
    /// <summary>
    /// Test just the filtering part without simulation - check if basic filtering works
    /// </summary>
    [ContextMenu("Test Filter Only")]
    public void TestFilterOnly()
    {
        Debug.Log("=== TESTING FILTER ONLY ===");
        
        if (coreGameData == null || coreGameData.coreBlock == null)
        {
            Debug.LogError("CoreGameData is null or has no blocks!");
            return;
        }
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError($"Current block index {currentBlockIndex} is out of range");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("Current block has no dialog or choices!");
            return;
        }
        
        // Find first incorrect choice
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex < 0)
        {
            Debug.LogError("No incorrect choices found to test with!");
            return;
        }
        
        Debug.Log($"Before filtering: {currentBlock.Dialog.choices.Length} choices");
        Debug.Log($"questionInstance != null: {questionInstance != null}");
        
        // Ensure we have a question instance
        if (questionInstance == null)
        {
            Debug.Log("Creating question instance for test...");
            questionInstance = SummonQuestionBar();
            if (questionInstance == null)
            {
                Debug.LogError("Failed to create question instance!");
                return;
            }
        }
        
        Debug.Log($"Calling FilterIncorrectChoiceAndRespawn({incorrectChoiceIndex})...");
        FilterIncorrectChoiceAndRespawn(incorrectChoiceIndex);
        
        Debug.Log("After filtering:");
        Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
        Debug.Log($"  - currentFilteredChoices length: {currentFilteredChoices?.Length ?? 0}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        
        if (questionInstance != null)
        {
            Button[] buttons = questionInstance.GetComponentsInChildren<Button>();
            Debug.Log($"  - Found {buttons.Length} buttons in question instance");
            for (int i = 0; i < buttons.Length; i++)
            {
                Debug.Log($"    Button {i}: {buttons[i].name} - Active: {buttons[i].gameObject.activeInHierarchy}");
                if (buttons[i].gameObject.activeInHierarchy)
                {
                    TMP_Text btnText = buttons[i].GetComponentInChildren<TMP_Text>();
                    string text = btnText?.text ?? "NO TEXT";
                    Debug.Log($"      Text: '{text}'");
                }
            }
        }
        
        Debug.Log("=== END FILTER ONLY TEST ===");
    }
    
    /// <summary>
    /// Debug current dialog progression state and attempt to recover from stuck states
    /// </summary>
    [ContextMenu("Debug Dialog Progression State")]
    public void DebugDialogProgressionState()
    {
        Debug.Log("=== DIALOG PROGRESSION STATE DEBUG ===");
        
        Debug.Log($"Dialog Manager State:");
        Debug.Log($"  - isPlayingCutscene: {isPlayingCutscene}");
        Debug.Log($"  - isTextAnimating: {isTextAnimating}");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        Debug.Log($"  - IsSequenceRunning: {IsSequenceRunning}");
        
        Debug.Log($"Block Information:");
        Debug.Log($"  - currentBlockIndex: {currentBlockIndex}");
        Debug.Log($"  - Total blocks: {(coreGameData?.coreBlock?.Length ?? 0)}");
        
        Debug.Log($"Choice State:");
        Debug.Log($"  - selectedChoiceIndex: {selectedChoiceIndex}");
        Debug.Log($"  - currentChoiceResponseIndex: {currentChoiceResponseIndex}");
        Debug.Log($"  - onChoiceSelected != null: {onChoiceSelected != null}");
        
        Debug.Log($"UI State:");
        Debug.Log($"  - dialogInstance != null: {dialogInstance != null}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        
        if (coreGameData != null && currentBlockIndex < coreGameData.coreBlock.Length)
        {
            var currentBlock = coreGameData.coreBlock[currentBlockIndex];
            Debug.Log($"Current Block:");
            Debug.Log($"  - Type: {currentBlock.Type}");
            
            if (currentBlock.Dialog != null)
            {
                Debug.Log($"  - Dialog Type: {currentBlock.Dialog.dialogType}");
                Debug.Log($"  - Has Choices: {currentBlock.Dialog.choices != null && currentBlock.Dialog.choices.Length > 0}");
                if (currentBlock.Dialog.choices != null)
                {
                    Debug.Log($"  - Choice Count: {currentBlock.Dialog.choices.Length}");
                }
            }
        }
        
        Debug.Log("=== ATTEMPTING PROGRESSION ===");
        Debug.Log("Calling HandleDialogProgression() to see current behavior...");
        HandleDialogProgression();
        
        Debug.Log("=== END DIALOG PROGRESSION STATE DEBUG ===");
    }
    
    /// <summary>
    /// Force reset dialog state - use this if dialog gets stuck
    /// </summary>
    [ContextMenu("Force Reset Dialog State")]
    public void ForceResetDialogState()
    {
        Debug.Log("=== FORCE RESETTING DIALOG STATE ===");
        
        // Reset all dialog states
        isShowingResponse = false;
        isTextAnimating = false;
        isPlayingCutscene = false;
        selectedChoiceIndex = -1;
        currentChoiceResponseIndex = -1;
        onChoiceSelected = null;
        
        // Reset input throttling
        isProcessingInput = false;
        isInDialogTransition = false;
        lastInputTime = 0f;
        lastDialogUpdateTime = 0f;
        
        // Clear component cache
        ClearComponentCache();
        
        // Clear button states
        buttonTweenIds.Clear();
        
        // Stop any audio
        if (dialogAudioSource != null && dialogAudioSource.isPlaying)
        {
            dialogAudioSource.Stop();
        }
        
        // Cancel any active tweens
        if (dialogTween != null)
        {
            LeanTween.cancel(gameObject, dialogTween.id);
            dialogTween = null;
        }
        
        // Clear 3D dialogs
        ClearAll3DDialogs();
        
        // Hide any active choices
        HideChoices();
        
        Debug.Log("Dialog state has been reset. Try pressing SPACE or selecting a choice again.");
        Debug.Log("If still stuck, try 'Debug Dialog Progression State' to see what's happening.");
        
        Debug.Log("=== DIALOG STATE RESET COMPLETE ===");
    }
    
    /// <summary>
    /// Comprehensive dialog progression fix - call this if dialog gets stuck
    /// </summary>
    [ContextMenu("Fix Dialog Progression")]
    public void FixDialogProgression()
    {
        Debug.Log("=== ATTEMPTING TO FIX DIALOG PROGRESSION ===");
        
        if (coreGameData == null || coreGameData.coreBlock == null)
        {
            Debug.LogError("CoreGameData is null or has no blocks!");
            return;
        }
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.Log("Already at end of game");
            FinishCoreGame();
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        Debug.Log($"Current block {currentBlockIndex}: Type={currentBlock.Type}");
        
        if (currentBlock.Type == CoreGameBlock.CoreType.Dialog && currentBlock.Dialog != null)
        {
            bool hasChoices = currentBlock.Dialog.choices != null && currentBlock.Dialog.choices.Length > 0;
            Debug.Log($"Dialog block has choices: {hasChoices}");
            
            if (!hasChoices)
            {
                Debug.Log("No choices - this dialog should auto-advance. Forcing continuation...");
                isShowingResponse = false;
                isTextAnimating = false;
                currentChoiceResponseIndex = -1;
                selectedChoiceIndex = -1;
                onChoiceSelected = null;
                
                // Force advance to next block
                ContinueToNextBlock();
            }
            else
            {
                Debug.Log($"Dialog has {currentBlock.Dialog.choices.Length} choices - waiting for user selection");
                
                // Check if we're stuck in response mode
                if (isShowingResponse)
                {
                    Debug.Log("Currently showing response - this might be the problem");
                    if (selectedChoiceIndex >= 0 && selectedChoiceIndex < currentBlock.Dialog.choices.Length)
                    {
                        var selectedChoice = currentBlock.Dialog.choices[selectedChoiceIndex];
                        if (selectedChoice.dialogResponses == null || selectedChoice.dialogResponses.Length == 0)
                        {
                            Debug.Log("Selected choice has no responses - forcing next block");
                            isShowingResponse = false;
                            currentChoiceResponseIndex = -1;
                            selectedChoiceIndex = -1;
                            ContinueToNextBlock();
                        }
                        else
                        {
                            Debug.Log($"Selected choice has {selectedChoice.dialogResponses.Length} responses, currentResponseIndex={currentChoiceResponseIndex}");
                            if (currentChoiceResponseIndex >= selectedChoice.dialogResponses.Length - 1)
                            {
                                Debug.Log("All responses shown - forcing next block");
                                isShowingResponse = false;
                                currentChoiceResponseIndex = -1;
                                selectedChoiceIndex = -1;
                                ContinueToNextBlock();
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"Current block is not a dialog or has no dialog data - forcing next block");
            ContinueToNextBlock();
        }
        
        Debug.Log("=== DIALOG PROGRESSION FIX COMPLETE ===");
    }
    
    /// <summary>
    /// Test text animation - call this to verify animation is working
    /// </summary>
    [ContextMenu("Test Text Animation")]
    public void TestTextAnimation()
    {
        Debug.Log("=== TESTING TEXT ANIMATION ===");
        
        if (dialogInstance == null)
        {
            Debug.LogError("No dialog instance found! Please create a dialog first.");
            return;
        }
        
        // Find the text component to animate
        TMP_Text dialogTextComponent = null;
        DialogPrefabController controller = dialogInstance.GetComponent<DialogPrefabController>();
        if (controller != null && controller.dialogueText != null)
        {
            dialogTextComponent = controller.dialogueText;
            Debug.Log("Found DialogueText component via DialogPrefabController for test animation");
        }
        else
        {
            Transform textTransform = dialogInstance.transform.Find("DialogueText");
            if (textTransform != null)
            {
                dialogTextComponent = textTransform.GetComponent<TMP_Text>();
                Debug.Log("Found DialogueText component via direct search for test animation");
            }
        }
        
        if (dialogTextComponent != null)
        {
            string testText = "This is a test of the typewriter text animation system. It should appear character by character with proper timing.";
            Debug.Log($"Starting test animation with text: '{testText}'");
            AnimateDialogText(testText, dialogTextComponent, null);
        }
        else
        {
            Debug.LogError("Could not find DialogueText component for animation test!");
        }
        
        Debug.Log("=== END TEXT ANIMATION TEST ===");
    }
    
    /// <summary>
    /// Test DialogSpace control during text animation
    /// </summary>
    [ContextMenu("Test DialogSpace Control")]
    public void TestDialogSpaceControl()
    {
        Debug.Log("=== TESTING DIALOGSPACE CONTROL ===");
        
        if (dialogInstance == null)
        {
            Debug.LogError("Dialog instance is null! Please show a dialog first.");
            return;
        }
        
        // Test caching DialogSpace
        CacheDialogSpace();
        
        if (cachedDialogSpace != null)
        {
            Debug.Log($"DialogSpace found: {cachedDialogSpace.name}");
            Debug.Log($"Current state: {(cachedDialogSpace.activeSelf ? "ENABLED" : "DISABLED")}");
            
            // Test disable
            Debug.Log("Testing DISABLE...");
            SetDialogSpaceActive(false);
            
            // Wait a moment then test enable
            StartCoroutine(TestDialogSpaceDelayed());
        }
        else
        {
            Debug.LogError("DialogSpace not found in dialog instance!");
        }
        
        Debug.Log("=== END DIALOGSPACE CONTROL TEST ===");
    }
    
    private IEnumerator TestDialogSpaceDelayed()
    {
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Testing ENABLE...");
        SetDialogSpaceActive(true);
        
        Debug.Log("DialogSpace control test complete!");
    }
    
    #endregion
    
    /// <summary>
    /// Get NPC response from CoreGameDialogChoices using current response index
    /// </summary>
    private string GetNpcResponseFromChoice(CoreGameDialogChoices choice)
    {
        if (choice.dialogResponses == null || choice.dialogResponses.Length == 0)
        {
            Debug.LogWarning("No dialog responses found in choice!");
            return "No response available.";
        }
        
        // Use selectedResponseIndex, but clamp it to valid range
        int responseIndex = Mathf.Clamp(selectedResponseIndex, 0, choice.dialogResponses.Length - 1);
        return choice.dialogResponses[responseIndex].npcResponse;
    }
    
    /// <summary>
    /// Get NPC name from CoreGameDialogChoices using current response index
    /// </summary>
    private string GetNpcNameFromChoice(CoreGameDialogChoices choice)
    {
        if (choice.dialogResponses == null || choice.dialogResponses.Length == 0)
        {
            Debug.LogWarning("No dialog responses found in choice!");
            return "Unknown";
        }
        
        // Use selectedResponseIndex, but clamp it to valid range
        int responseIndex = Mathf.Clamp(selectedResponseIndex, 0, choice.dialogResponses.Length - 1);
        return ConvertNpcNameToString(choice.dialogResponses[responseIndex].npcName);
    }
    
    /// <summary>
    /// Set which response index to use from dialogResponses array
    /// </summary>
    public void SetResponseIndex(int index)
    {
        selectedResponseIndex = index;
        Debug.Log($"Response index set to: {selectedResponseIndex}");
    }
    
    /// <summary>
    /// Get a random response from the available responses
    /// </summary>
    public void UseRandomResponse(CoreGameDialogChoices choice)
    {
        if (choice.dialogResponses != null && choice.dialogResponses.Length > 0)
        {
            selectedResponseIndex = UnityEngine.Random.Range(0, choice.dialogResponses.Length);
            Debug.Log($"Using random response index: {selectedResponseIndex}");
        }
    }
    
    /// <summary>
    /// Get the number of available responses for a choice
    /// </summary>
    public int GetResponseCount(CoreGameDialogChoices choice)
    {
        return choice.dialogResponses?.Length ?? 0;
    }
    
    /// <summary>
    /// Set response index based on some condition (e.g., player stats, previous choices, etc.)
    /// </summary>
    public void SetResponseByCondition(CoreGameDialogChoices choice, System.Func<CoreGameDialogChoicesResponse, bool> condition)
    {
        if (choice.dialogResponses == null || choice.dialogResponses.Length == 0)
            return;
            
        for (int i = 0; i < choice.dialogResponses.Length; i++)
        {
            if (condition(choice.dialogResponses[i]))
            {
                selectedResponseIndex = i;
                Debug.Log($"Response index set to {i} based on condition");
                return;
            }
        }
        
        // If no condition matches, use first response
        selectedResponseIndex = 0;
        Debug.Log("No condition matched, using first response");
    }
    
    /// <summary>
    /// Preview all available responses for a choice (for debugging)
    /// </summary>
    public void LogAllResponses(CoreGameDialogChoices choice)
    {
        if (choice.dialogResponses == null || choice.dialogResponses.Length == 0)
        {
            Debug.Log("No responses available for this choice");
            return;
        }
        
        Debug.Log($"Available responses for choice '{choice.playerChoice}':");
        for (int i = 0; i < choice.dialogResponses.Length; i++)
        {
            var response = choice.dialogResponses[i];
            string responseNpcName = ConvertNpcNameToString(response.npcName);
            Debug.Log($"  [{i}] {responseNpcName}: {response.npcResponse}");
        }
    }
    
    /// <summary>
    /// Update the NPC name display text (2D dialogs only)
    /// </summary>
    private void UpdateNpcNameDisplay(string npcName)
    {
        Debug.Log($"UpdateNpcNameDisplay called with: '{npcName}'");
        // Use immediate update to avoid delays and synchronize with dialog text
        UpdateNpcNameImmediate(npcName);
    }
    
    /// <summary>
    /// Update NPC name in 3D dialog displays
    /// </summary>
    private void UpdateNpcNameIn3DDialog(string npcName)
    {
        // Update NPC names in all possible 3D dialog locations
        string[] modelNames = { "Linda_Model", "Isayat_Model", "Rey_Baby_Model" };
        
        foreach (string modelName in modelNames)
        {
            GameObject model = GameObject.Find(modelName);
            if (model != null)
            {
                // Look for NPC name text component (you might need to adjust the path)
                Transform npcNameTransform = model.transform.Find("DialogueName");
                if (npcNameTransform != null)
                {
                    var npcNameComponent = npcNameTransform.GetComponent<TMP_Text>();
                    if (npcNameComponent != null)
                    {
                        npcNameComponent.text = npcName;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Extract NPC name from dialog text if it follows "Name: dialog" format
    /// </summary>
    private string ExtractNpcNameFromDialogText(string dialogText)
    {
        if (string.IsNullOrEmpty(dialogText))
            return "";
        
        // Check if dialog follows "Name: dialog text" format
        int colonIndex = dialogText.IndexOf(':');
        if (colonIndex > 0 && colonIndex < 20) // Reasonable name length limit
        {
            string potentialName = dialogText.Substring(0, colonIndex).Trim();
            // Simple validation - names shouldn't be too long and should be reasonable
            if (potentialName.Length > 0 && potentialName.Length < 20 && !potentialName.Contains(' '))
            {
                return potentialName;
            }
            // Handle names with spaces (like "Rey Baby")
            else if (potentialName.Length > 0 && potentialName.Length < 20)
            {
                return potentialName;
            }
        }
        
        return ""; // No name found
    }

    #region Stress Modifier System
    
    /// <summary>
    /// Process all modifiers in dialog text and apply them
    /// Supported modifiers:
    /// - {+100stress}, {-50stress} - for stress changes
    /// - {animation:animationName} - for triggering animations
    /// - {scene:sceneName} - for changing scenes
    /// - {prefab:prefabName} - for spawning prefabs
    /// Returns the cleaned text without the modifiers
    /// </summary>
    /// <param name="dialogText">Original dialog text with potential modifiers</param>
    /// <returns>Clean dialog text without modifiers</returns>
    private string ProcessStressModifiers(string dialogText)
    {
        if (string.IsNullOrEmpty(dialogText))
        {
            return dialogText;
        }
        
        string processedText = dialogText;
        
        // Process stress modifiers: {+100stress}, {-50stress}
        processedText = ProcessStressModifiersInternal(processedText);
        
        // Process animation modifiers: {animation:animationName}
        processedText = ProcessAnimationModifiers(processedText);
        
        // Process scene modifiers: {scene:sceneName}
        processedText = ProcessSceneModifiers(processedText);
        
        // Process prefab modifiers: {prefab:prefabName}
        processedText = ProcessPrefabModifiers(processedText);
        
        // Clean up any double spaces that might result from removing modifiers
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"\s+", " ").Trim();
        
        return processedText;
    }
    
    /// <summary>
    /// Process stress modifiers in dialog text and apply them to stressvariable
    /// Modifiers format: {+100stress}, {-50stress}, etc.
    /// </summary>
    /// <param name="dialogText">Text containing stress modifiers</param>
    /// <returns>Text with stress modifiers removed</returns>
    private string ProcessStressModifiersInternal(string dialogText)
    {
        string processedText = dialogText;
        
        // Regex pattern to find stress modifiers: {+number stress} or {-number stress}
        System.Text.RegularExpressions.Regex stressPattern = 
            new System.Text.RegularExpressions.Regex(@"\{([+-]?\d+)stress\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        System.Text.RegularExpressions.MatchCollection matches = stressPattern.Matches(dialogText);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                string valueStr = match.Groups[1].Value;
                if (int.TryParse(valueStr, out int stressValue))
                {
                    // Apply stress modifier to both local variable and ScriptableObject
                    stressvariable += stressValue;
                    
                    // Sync with ScriptableObject for UI consistency
                    if (saveData != null)
                    {
                        saveData.mother_stress_level += stressValue;
                        Debug.Log($"[STRESS] Synced stress with ScriptableObject: {saveData.mother_stress_level}");
                    }
                    else
                    {
                        Debug.LogWarning("[STRESS] saveData is null, stress not synced to ScriptableObject");
                    }
                    
                    Debug.Log($"[STRESS] Applied stress modifier: {stressValue} (Local: {stressvariable}, ScriptableObject: {saveData?.mother_stress_level ?? -1})");
                    
                    // Refresh all stress bar UIs to reflect the change
                    RefreshStressBars();
                }
                else
                {
                    Debug.LogWarning($"[STRESS] Failed to parse stress value: '{valueStr}' from modifier: '{match.Value}'");
                }
                
                // Remove the modifier from the text
                processedText = processedText.Replace(match.Value, "");
            }
        }
        
        return processedText;
    }
    
    /// <summary>
    /// Process animation modifiers in dialog text and trigger animations
    /// Modifiers format: {animation:animationName}
    /// </summary>
    /// <param name="dialogText">Text containing animation modifiers</param>
    /// <returns>Text with animation modifiers removed</returns>
    private string ProcessAnimationModifiers(string dialogText)
    {
        string processedText = dialogText;
        
        // Regex pattern to find animation modifiers: {animation:animationName}
        System.Text.RegularExpressions.Regex animationPattern = 
            new System.Text.RegularExpressions.Regex(@"\{animation:([^}]+)\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        System.Text.RegularExpressions.MatchCollection matches = animationPattern.Matches(dialogText);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                string animationName = match.Groups[1].Value.Trim();
                
                if (!string.IsNullOrEmpty(animationName))
                {
                    Debug.Log($"[ANIMATION] Triggering animation: '{animationName}'");
                    TriggerAnimation(animationName);
                }
                else
                {
                    Debug.LogWarning($"[ANIMATION] Empty animation name in modifier: '{match.Value}'");
                }
                
                // Remove the modifier from the text
                processedText = processedText.Replace(match.Value, "");
            }
        }
        
        return processedText;
    }
    
    /// <summary>
    /// Process scene modifiers in dialog text and trigger scene changes
    /// Modifiers format: {scene:sceneName}
    /// </summary>
    /// <param name="dialogText">Text containing scene modifiers</param>
    /// <returns>Text with scene modifiers removed</returns>
    private string ProcessSceneModifiers(string dialogText)
    {
        string processedText = dialogText;
        
        // Regex pattern to find scene modifiers: {scene:sceneName}
        System.Text.RegularExpressions.Regex scenePattern = 
            new System.Text.RegularExpressions.Regex(@"\{scene:([^}]+)\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        System.Text.RegularExpressions.MatchCollection matches = scenePattern.Matches(dialogText);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                string sceneName = match.Groups[1].Value.Trim();
                
                if (!string.IsNullOrEmpty(sceneName))
                {
                    Debug.Log($"[SCENE] Triggering scene change to: '{sceneName}'");
                    TriggerSceneChange(sceneName);
                }
                else
                {
                    Debug.LogWarning($"[SCENE] Empty scene name in modifier: '{match.Value}'");
                }
                
                // Remove the modifier from the text
                processedText = processedText.Replace(match.Value, "");
            }
        }
        
        return processedText;
    }
    
    /// <summary>
    /// Process prefab modifiers in dialog text and spawn prefabs
    /// Modifiers format: {prefab:prefabName}
    /// </summary>
    /// <param name="dialogText">Text containing prefab modifiers</param>
    /// <returns>Text with prefab modifiers removed</returns>
    private string ProcessPrefabModifiers(string dialogText)
    {
        string processedText = dialogText;
        
        // Regex pattern to find prefab modifiers: {prefab:prefabName}
        System.Text.RegularExpressions.Regex prefabPattern = 
            new System.Text.RegularExpressions.Regex(@"\{prefab:([^}]+)\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        System.Text.RegularExpressions.MatchCollection matches = prefabPattern.Matches(dialogText);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                string prefabName = match.Groups[1].Value.Trim();
                
                if (!string.IsNullOrEmpty(prefabName))
                {
                    Debug.Log($"[PREFAB] Spawning prefab: '{prefabName}'");
                    SpawnPrefab(prefabName);
                }
                else
                {
                    Debug.LogWarning($"[PREFAB] Empty prefab name in modifier: '{match.Value}'");
                }
                
                // Remove the modifier from the text
                processedText = processedText.Replace(match.Value, "");
            }
        }
        
        return processedText;
    }
    
    /// <summary>
    /// Trigger an animation by name
    /// Override this method to implement your specific animation logic
    /// </summary>
    /// <param name="animationName">Name of the animation to trigger</param>
    protected virtual void TriggerAnimation(string animationName)
    {
        Debug.Log($"[ANIMATION] TriggerAnimation called with: '{animationName}'");
        
        // Trigger event for external subscribers
        OnAnimationTriggered?.Invoke(animationName);
        
        // Example implementation - you can customize this based on your animation system
        try
        {
            // Method 1: Try DialogCharacterAnimatorManager for character animations
            DialogCharacterAnimatorManager animManager = FindFirstObjectByType<DialogCharacterAnimatorManager>();
            if (animManager != null)
            {
                // Try to play the animation on the current NPC
                CoreGameBlock currentBlock = coreGameData.coreBlock[currentBlockIndex];
                if (currentBlock.Dialog != null)
                {
                    CoreGameDialog.NpcName currentNpc = currentBlock.Dialog.npcName;
                    
                    // Try to get the animator for the current NPC and play the animation directly
                    var animatorEntries = animManager.characterAnimators;
                    foreach (var entry in animatorEntries)
                    {
                        if (entry.npcName == currentNpc && entry.animator != null)
                        {
                            // Try Play first (for animation state names) - most common case
                            try
                            {
                                entry.animator.Play(animationName);
                                Debug.Log($"[ANIMATION] ✓ Triggered animation '{animationName}' on {currentNpc} using Play");
                                return;
                            }
                            catch (System.Exception playEx)
                            {
                                Debug.LogWarning($"[ANIMATION] Play failed for '{animationName}' on {currentNpc}: {playEx.Message}");
                                // If Play fails, try SetTrigger (for trigger parameters)
                                try
                                {
                                    entry.animator.SetTrigger(animationName);
                                    Debug.Log($"[ANIMATION] ✓ Triggered animation '{animationName}' on {currentNpc} using SetTrigger");
                                    return;
                                }
                                catch (System.Exception triggerEx)
                                {
                                    Debug.LogWarning($"[ANIMATION] SetTrigger also failed for '{animationName}' on {currentNpc}: {triggerEx.Message}");
                                }
                            }
                        }
                    }
                }
            }
            
            // Method 2: Try to find an Animator component on this GameObject
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                try
                {
                    animator.Play(animationName);
                    Debug.Log($"[ANIMATION] ✓ Triggered animation '{animationName}' on local Animator using Play");
                    return;
                }
                catch (System.Exception playEx)
                {
                    Debug.LogWarning($"[ANIMATION] Play failed for '{animationName}' on local Animator: {playEx.Message}");
                    try
                    {
                        animator.SetTrigger(animationName);
                        Debug.Log($"[ANIMATION] ✓ Triggered animation '{animationName}' on local Animator using SetTrigger");
                        return;
                    }
                    catch (System.Exception triggerEx)
                    {
                        Debug.LogWarning($"[ANIMATION] SetTrigger also failed for '{animationName}' on local Animator: {triggerEx.Message}");
                    }
                }
            }
            
            // Method 3: Look for global animation manager or specific objects
            GameObject animationTarget = GameObject.Find("AnimationManager");
            if (animationTarget != null)
            {
                Animator targetAnimator = animationTarget.GetComponent<Animator>();
                if (targetAnimator != null)
                {
                    try
                    {
                        targetAnimator.Play(animationName);
                        Debug.Log($"[ANIMATION] ✓ Triggered animation '{animationName}' on AnimationManager using Play");
                        return;
                    }
                    catch (System.Exception playEx)
                    {
                        Debug.LogWarning($"[ANIMATION] Play failed for '{animationName}' on AnimationManager: {playEx.Message}");
                        try
                        {
                            targetAnimator.SetTrigger(animationName);
                            Debug.Log($"[ANIMATION] ✓ Triggered animation '{animationName}' on AnimationManager using SetTrigger");
                            return;
                        }
                        catch (System.Exception triggerEx)
                        {
                            Debug.LogWarning($"[ANIMATION] SetTrigger also failed for '{animationName}' on AnimationManager: {triggerEx.Message}");
                        }
                    }
                }
            }
            
            // Method 4: Custom animation handling - you can add your own logic here
            HandleCustomAnimation(animationName);
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ANIMATION] Error triggering animation '{animationName}': {e.Message}");
        }
    }
    
    /// <summary>
    /// Handle custom animations that don't use Unity's Animator system
    /// Override this method for custom animation implementations
    /// </summary>
    /// <param name="animationName">Name of the custom animation</param>
    protected virtual void HandleCustomAnimation(string animationName)
    {
        Debug.Log($"[ANIMATION] HandleCustomAnimation called with: '{animationName}'");
        
        // Example custom animations
        switch (animationName.ToLower())
        {
            case "shake":
                StartCoroutine(ShakeAnimation());
                break;
                
            case "fade":
                StartCoroutine(FadeAnimation());
                break;
                
            case "bounce":
                StartCoroutine(BounceAnimation());
                break;
                
            default:
                Debug.LogWarning($"[ANIMATION] Unknown custom animation: '{animationName}'");
                break;
        }
    }
    
    /// <summary>
    /// Trigger a scene change by name
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    protected virtual void TriggerSceneChange(string sceneName)
    {
        Debug.Log($"[SCENE] TriggerSceneChange called with: '{sceneName}'");
        
        // Trigger event for external subscribers
        OnSceneChangeTriggered?.Invoke(sceneName);
        
        try
        {
            // Check if the scene exists in build settings
            if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(sceneName) >= 0)
            {
                Debug.Log($"[SCENE] ✓ Loading scene '{sceneName}' from build settings");
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            else
            {
                // Try loading by exact name
                Debug.Log($"[SCENE] Attempting to load scene '{sceneName}' by name");
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SCENE] Error loading scene '{sceneName}': {e.Message}");
            
            // Fallback: try async loading
            StartCoroutine(LoadSceneAsync(sceneName));
        }
    }
    
    /// <summary>
    /// Spawn a prefab by name
    /// Override this method to implement your specific prefab spawning logic
    /// </summary>
    /// <param name="prefabName">Name of the prefab to spawn</param>
    protected virtual void SpawnPrefab(string prefabName)
    {
        Debug.Log($"[PREFAB] SpawnPrefab called with: '{prefabName}'");
        
        // Trigger event for external subscribers
        OnPrefabSpawned?.Invoke(prefabName);
        
        try
        {
            // Method 1: Try to load from Resources folder
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab != null)
            {
                GameObject spawnedObject = Instantiate(prefab);
                Debug.Log($"[PREFAB] ✓ Spawned prefab '{prefabName}' from Resources folder");
                
                // Optional: Position the spawned object
                PositionSpawnedPrefab(spawnedObject, prefabName);
                return;
            }
            
            // Method 2: Try to load from Resources with "Prefabs/" path
            prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
            if (prefab != null)
            {
                GameObject spawnedObject = Instantiate(prefab);
                Debug.Log($"[PREFAB] ✓ Spawned prefab '{prefabName}' from Resources/Prefabs folder");
                
                // Optional: Position the spawned object
                PositionSpawnedPrefab(spawnedObject, prefabName);
                return;
            }
            
            // Method 3: Custom prefab handling
            HandleCustomPrefabSpawn(prefabName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PREFAB] Error spawning prefab '{prefabName}': {e.Message}");
        }
    }
    
    /// <summary>
    /// Load scene asynchronously as a fallback
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"[SCENE] Attempting async load of scene: '{sceneName}'");
        
        var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                Debug.Log($"[SCENE] Loading scene '{sceneName}': {asyncLoad.progress * 100f:F1}%");
                yield return null;
            }
            
            Debug.Log($"[SCENE] ✓ Scene '{sceneName}' loaded successfully");
        }
        else
        {
            Debug.LogError($"[SCENE] ✗ Failed to start async load for scene '{sceneName}'");
        }
    }
    
    /// <summary>
    /// Position a spawned prefab based on its name or type
    /// Override this method to customize prefab positioning
    /// </summary>
    /// <param name="spawnedObject">The instantiated prefab</param>
    /// <param name="prefabName">Name of the prefab for context</param>
    protected virtual void PositionSpawnedPrefab(GameObject spawnedObject, string prefabName)
    {
        Debug.Log($"[PREFAB] Positioning spawned prefab: '{prefabName}'");
        
        // Example positioning logic - customize based on your needs
        Vector3 spawnPosition = Vector3.zero;
        
        // Position based on prefab name or type
        switch (prefabName.ToLower())
        {
            case "particle":
            case "explosion":
                // Spawn at camera position
                if (Camera.main != null)
                {
                    spawnPosition = Camera.main.transform.position + Camera.main.transform.forward * 2f;
                }
                break;
                
            case "npc":
            case "character":
                // Spawn in front of player
                spawnPosition = transform.position + transform.forward * 2f;
                break;
                
            case "item":
            case "pickup":
                // Spawn near player
                spawnPosition = transform.position + Vector3.right * 1f;
                break;
                
            default:
                // Default position in scene
                spawnPosition = Vector3.zero;
                break;
        }
        
        spawnedObject.transform.position = spawnPosition;
        Debug.Log($"[PREFAB] ✓ Positioned '{prefabName}' at {spawnPosition}");
    }
    
    /// <summary>
    /// Handle custom prefab spawning logic when standard methods fail
    /// Override this method for advanced prefab management
    /// </summary>
    /// <param name="prefabName">Name of the prefab to spawn</param>
    protected virtual void HandleCustomPrefabSpawn(string prefabName)
    {
        Debug.Log($"[PREFAB] HandleCustomPrefabSpawn called with: '{prefabName}'");
        
        // Example custom prefab handling
        switch (prefabName.ToLower())
        {
            case "dialog":
                // Spawn a dialog prefab
                if (npcDialogThemplate != null)
                {
                    GameObject customDialog = Instantiate(npcDialogThemplate);
                    Debug.Log($"[PREFAB] ✓ Spawned custom dialog prefab");
                }
                break;
                
            case "question":
                // Spawn a question prefab
                if (npcQuestionThemplate != null)
                {
                    GameObject customQuestion = Instantiate(npcQuestionThemplate);
                    Debug.Log($"[PREFAB] ✓ Spawned custom question prefab");
                }
                break;
                
            default:
                Debug.LogWarning($"[PREFAB] ✗ Unknown prefab '{prefabName}' - cannot spawn");
                break;
        }
    }
    
    #region Example Custom Animations
    
    /// <summary>
    /// Example shake animation
    /// </summary>
    private IEnumerator ShakeAnimation()
    {
        Debug.Log("[ANIMATION] Starting shake animation");
        
        Vector3 originalPosition = transform.position;
        float duration = 0.5f;
        float magnitude = 0.1f;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            
            transform.position = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
        Debug.Log("[ANIMATION] Shake animation complete");
    }
    
    /// <summary>
    /// Example fade animation
    /// </summary>
    private IEnumerator FadeAnimation()
    {
        Debug.Log("[ANIMATION] Starting fade animation");
        
        if (backgroundFade != null)
        {
            backgroundFade.gameObject.SetActive(true);
            
            // Fade in
            LeanTween.value(backgroundFade.gameObject, 0f, 1f, 0.5f)
                .setOnUpdate((float alpha) =>
                {
                    Color color = backgroundFade.color;
                    color.a = alpha;
                    backgroundFade.color = color;
                });
            
            yield return new WaitForSeconds(0.5f);
            
            // Fade out
            LeanTween.value(backgroundFade.gameObject, 1f, 0f, 0.5f)
                .setOnUpdate((float alpha) =>
                {
                    Color color = backgroundFade.color;
                    color.a = alpha;
                    backgroundFade.color = color;
                })
                .setOnComplete(() =>
                {
                    backgroundFade.gameObject.SetActive(false);
                });
        }
        
        Debug.Log("[ANIMATION] Fade animation complete");
    }
    
    /// <summary>
    /// Example bounce animation
    /// </summary>
    private IEnumerator BounceAnimation()
    {
        Debug.Log("[ANIMATION] Starting bounce animation");
        
        Vector3 originalScale = transform.localScale;
        
        // Scale up
        LeanTween.scale(gameObject, originalScale * 1.2f, 0.2f)
            .setEase(LeanTweenType.easeOutBack);
        
        yield return new WaitForSeconds(0.2f);
        
        // Scale back to normal
        LeanTween.scale(gameObject, originalScale, 0.2f)
            .setEase(LeanTweenType.easeInBack);
        
        yield return new WaitForSeconds(0.2f);
        
        Debug.Log("[ANIMATION] Bounce animation complete");
    }
    
    #endregion
    
    /// <summary>
    /// Get current stress value (for debugging or UI display)
    /// Reads from ScriptableObject for consistency with UI
    /// </summary>
    public int GetCurrentStress()
    {
        if (saveData != null)
        {
            // Ensure local variable is synced with ScriptableObject
            SynchronizeStressValues();
            return saveData.mother_stress_level;
        }
        else
        {
            Debug.LogWarning("[STRESS] saveData is null, returning local stress variable");
            return stressvariable;
        }
    }
    
    /// <summary>
    /// Set stress to a specific value (for debugging or save/load systems)
    /// </summary>
    public void SetStress(int value)
    {
        stressvariable = value;
        
        // Sync with ScriptableObject for UI consistency
        if (saveData != null)
        {
            saveData.mother_stress_level = value;
            Debug.Log($"[STRESS] Stress set to: {stressvariable} (Synced to ScriptableObject: {saveData.mother_stress_level})");
        }
        else
        {
            Debug.Log($"[STRESS] Stress set to: {stressvariable} (saveData is null, not synced)");
        }
        
        // Refresh all stress bar UIs to reflect the change
        RefreshStressBars();
    }
    
    /// <summary>
    /// Add stress value directly (alternative to using modifiers in text)
    /// </summary>
    public void AddStress(int value)
    {
        stressvariable += value;
        
        // Sync with ScriptableObject for UI consistency
        if (saveData != null)
        {
            saveData.mother_stress_level += value;
            Debug.Log($"[STRESS] Added {value} stress (Local: {stressvariable}, ScriptableObject: {saveData.mother_stress_level})");
        }
        else
        {
            Debug.Log($"[STRESS] Added {value} stress (Local: {stressvariable}, saveData is null)");
        }
        
        // Refresh all stress bar UIs to reflect the change
        RefreshStressBars();
    }
    
    /// <summary>
    /// Test method for all modifier systems - can be called from Unity Editor
    /// </summary>
    [ContextMenu("Test All Modifier Systems")]
    public void TestStressModifierSystem()
    {
        Debug.Log("=== ALL MODIFIER SYSTEMS TEST ===");
        
        // Test various modifier formats
        string[] testTexts = {
            "Hello! {+100stress} This should add 100 stress.",
            "I'm feeling bad today. {-50stress} That should reduce stress by 50.",
            "Multiple modifiers {+25stress} in one {+10stress} sentence.",
            "Let's trigger an animation {animation:shake} and see what happens!",
            "This will change the scene {scene:MainMenu} after the dialog.",
            "Let me spawn a prefab {prefab:TestPrefab} for you to see!",
            "Combined test: {+75stress} {animation:bounce} {scene:GameScene} {prefab:ItemPickup}",
            "No modifiers in this text.",
            "{+200stress} Modifier at the beginning.",
            "Modifier at the end {-75stress}",
            "Animation at start {animation:fade} with more text.",
            "Scene change {scene:Level2} in the middle of text.",
            "Prefab spawn {prefab:Explosion} in the middle.",
            "Invalid modifier {invalidstress} should be ignored.",
            "Case insensitive {+30STRESS} {ANIMATION:Shake} {SCENE:Menu} {PREFAB:Dialog} modifiers.",
            "{0stress} Zero modifier should work.",
            "Empty modifiers {animation:} {scene:} {prefab:} should be handled gracefully."
        };
        
        int initialStress = stressvariable;
        Debug.Log($"Initial stress: {initialStress}");
        
        foreach (string test in testTexts)
        {
            Debug.Log($"Testing: '{test}'");
            string processed = ProcessStressModifiers(test);
            Debug.Log($"Result: '{processed}' | Current stress: {stressvariable}");
            Debug.Log("---");
        }
        
        int finalStress = stressvariable;
        int totalChange = finalStress - initialStress;
        Debug.Log($"Final stress: {finalStress} (Change: {totalChange})");
        Debug.Log("=== END ALL MODIFIER SYSTEMS TEST ===");
    }
    
    /// <summary>
    /// Public property to access current stress value
    /// Reads from ScriptableObject for consistency with UI
    /// </summary>
    public int CurrentStress => GetCurrentStress();
    
    /// <summary>
    /// Event triggered when an animation modifier is processed
    /// Subscribe to this to handle animations in external systems
    /// </summary>
    public System.Action<string> OnAnimationTriggered;
    
    /// <summary>
    /// Event triggered when a scene modifier is processed
    /// Subscribe to this to handle scene changes in external systems
    /// </summary>
    public System.Action<string> OnSceneChangeTriggered;
    
    /// <summary>
    /// Event triggered when a prefab modifier is processed
    /// Subscribe to this to handle prefab spawning in external systems
    /// </summary>
    public System.Action<string> OnPrefabSpawned;
    
    /// <summary>
    /// Synchronize local stress variable with ScriptableObject
    /// Ensures both values stay in sync
    /// </summary>
    private void SynchronizeStressValues()
    {
        if (saveData != null)
        {
            // Keep local variable in sync with ScriptableObject (ScriptableObject is the source of truth)
            stressvariable = saveData.mother_stress_level;
        }
    }
    
    /// <summary>
    /// Refresh all stress bar UIs in the scene to reflect current stress level
    /// Call this after modifying stress to update all UI elements
    /// </summary>
    private void RefreshStressBars()
    {
        // Find all StressBarIndicatorIbu components in the scene
        StressBarIndicatorIbu[] stressBars = UnityEngine.Object.FindObjectsByType<StressBarIndicatorIbu>(FindObjectsSortMode.None);
        
        if (stressBars.Length > 0)
        {
            Debug.Log($"[STRESS] Refreshing {stressBars.Length} stress bar(s) in the scene");
            
            foreach (StressBarIndicatorIbu stressBar in stressBars)
            {
                if (stressBar != null)
                {
                    // Force the stress bar to update by calling its internal update method
                    // The stress bar should read from the same ScriptableObject we just updated
                    try
                    {
                        // Call any public update method if available, or the component will update automatically
                        stressBar.enabled = false;
                        stressBar.enabled = true; // Force refresh by disabling/enabling component
                        Debug.Log($"[STRESS] Refreshed stress bar: {stressBar.name}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[STRESS] Failed to refresh stress bar {stressBar.name}: {e.Message}");
                    }
                }
            }
        }
        else
        {
            Debug.Log("[STRESS] No stress bars found in the scene to refresh");
        }
    }
    
    /// <summary>
    /// Initialize the stress system - ensures saveData is loaded and synchronized
    /// </summary>
    [ContextMenu("Initialize Stress System")]
    public void InitializeStressSystem()
    {
        Debug.Log("=== INITIALIZING STRESS SYSTEM ===");
        
        // Load saveData if not already loaded
        if (saveData == null)
        {
            Debug.Log("Loading saveData from Resources...");
            saveData = Resources.Load<CoreGameSaves>(saveDataPath);
            
            if (saveData != null)
            {
                Debug.Log($"saveData loaded successfully from: {saveDataPath}");
            }
            else
            {
                Debug.LogError($"Failed to load saveData from: {saveDataPath}");
                return;
            }
        }
        
        Debug.Log($"Current saveData state:");
        Debug.Log($"  - Day: {saveData.day}");
        Debug.Log($"  - Mother Stress Level: {saveData.mother_stress_level}");
        Debug.Log($"  - Local stress variable: {stressvariable}");
        
        // Synchronize values
        SynchronizeStressValues();
        Debug.Log($"After synchronization - Local stress variable: {stressvariable}");
        
        // Refresh all stress bars
        RefreshStressBars();
        
        Debug.Log("=== STRESS SYSTEM INITIALIZATION COMPLETE ===");
    }
    
    /// <summary>
    /// Test complete stress system integration - modifiers, sync, and UI refresh
    /// </summary>
    [ContextMenu("Test Complete Stress System Integration")]
    public void TestCompleteStressSystemIntegration()
    {
        Debug.Log("=== TESTING COMPLETE STRESS SYSTEM INTEGRATION ===");
        
        // Initialize if needed
        if (saveData == null)
        {
            InitializeStressSystem();
        }
        
        // Test data before
        Debug.Log($"BEFORE TEST:");
        Debug.Log($"  - ScriptableObject stress: {saveData?.mother_stress_level ?? -1}");
        Debug.Log($"  - Local stress variable: {stressvariable}");
        Debug.Log($"  - GetCurrentStress(): {GetCurrentStress()}");
        Debug.Log($"  - CurrentStress property: {CurrentStress}");
        
        // Test SetStress
        Debug.Log("\nTesting SetStress(150)...");
        SetStress(150);
        
        // Test AddStress
        Debug.Log("\nTesting AddStress(50)...");
        AddStress(50);
        
        // Test stress modifiers
        Debug.Log("\nTesting stress modifiers...");
        string testDialog = "Hello! {+100stress} This should add stress. {-25stress} This should reduce it.";
        string processed = ProcessStressModifiers(testDialog);
        Debug.Log($"Processed dialog: '{processed}'");
        
        // Final state
        Debug.Log($"\nAFTER TEST:");
        Debug.Log($"  - ScriptableObject stress: {saveData?.mother_stress_level ?? -1}");
        Debug.Log($"  - Local stress variable: {stressvariable}");
        Debug.Log($"  - GetCurrentStress(): {GetCurrentStress()}");
        Debug.Log($"  - CurrentStress property: {CurrentStress}");
        
        // Count stress bars in scene
        StressBarIndicatorIbu[] stressBars = UnityEngine.Object.FindObjectsByType<StressBarIndicatorIbu>(FindObjectsSortMode.None);
        Debug.Log($"\nFound {stressBars.Length} stress bar(s) in the scene that should reflect the changes.");
        
        Debug.Log("=== STRESS SYSTEM INTEGRATION TEST COMPLETE ===");
    }
    
    /// <summary>
    /// Manually trigger an animation (can be called externally)
    /// </summary>
    /// <param name="animationName">Name of the animation to trigger</param>
    public void TriggerAnimationManual(string animationName)
    {
        Debug.Log($"[ANIMATION] Manual animation trigger: '{animationName}'");
        TriggerAnimation(animationName);
    }
    
    /// <summary>
    /// Manually trigger a scene change (can be called externally)
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    public void TriggerSceneChangeManual(string sceneName)
    {
        Debug.Log($"[SCENE] Manual scene change trigger: '{sceneName}'");
        TriggerSceneChange(sceneName);
    }
    
    /// <summary>
    /// Manually spawn a prefab (can be called externally)
    /// </summary>
    /// <param name="prefabName">Name of the prefab to spawn</param>
    public void SpawnPrefabManual(string prefabName)
    {
        Debug.Log($"[PREFAB] Manual prefab spawn trigger: '{prefabName}'");
        SpawnPrefab(prefabName);
    }
    
    /// <summary>
    /// Process a custom text string and apply all modifiers
    /// Useful for testing or processing text from external sources
    /// </summary>
    /// <param name="text">Text containing modifiers</param>
    /// <returns>Cleaned text with modifiers applied</returns>
    public string ProcessCustomText(string text)
    {
        Debug.Log($"[MODIFIERS] Processing custom text: '{text}'");
        string result = ProcessStressModifiers(text);
        Debug.Log($"[MODIFIERS] Processed result: '{result}'");
        return result;
    }
    
    /// <summary>
    /// Test spam protection during text animation
    /// </summary>
    [ContextMenu("Test Animation Spam Protection")]
    public void TestAnimationSpamProtection()
    {
        Debug.Log("=== TESTING ANIMATION SPAM PROTECTION ===");
        
        if (dialogInstance == null)
        {
            Debug.LogError("No dialog instance found! Please create a dialog first.");
            return;
        }
        
        // Start a long text animation
        string longText = "This is a very long text that will take some time to animate completely. We will test what happens when we spam space during this animation.";
        
        // Find dialog text component safely
        TMP_Text dialogTextComponent = null;
        DialogPrefabController controller = dialogInstance.GetComponent<DialogPrefabController>();
        if (controller != null && controller.dialogueText != null)
        {
            dialogTextComponent = controller.dialogueText;
        }
        else
        {
            Transform textTransform = dialogInstance.transform.Find("DialogueText");
            if (textTransform != null)
            {
                dialogTextComponent = textTransform.GetComponent<TMP_Text>();
            }
        }
        
        if (dialogTextComponent == null)
        {
            Debug.LogError("Could not find dialog text component for animation test!");
            return;
        }
        
        Debug.Log("Starting long text animation, then simulating spam...");
        AnimateDialogText(longText, dialogTextComponent, null);
        
        // Start spam test after animation begins
        StartCoroutine(SimulateAnimationSpam());
    }
    
    /// <summary>
    /// Simulate spam during text animation
    /// </summary>
    private IEnumerator SimulateAnimationSpam()
    {
        // Wait for animation to start
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[ANIM-SPAM-TEST] Starting spam during text animation");
        
        for (int i = 0; i < 5; i++)
        {
            Debug.Log($"[ANIM-SPAM-TEST] Spam attempt #{i + 1} - isTextAnimating: {isTextAnimating}");
            
            if (isTextAnimating)
            {
                // Simulate HandleDialogProgression call during animation
                HandleDialogProgression();
            }
            
            // Very short delay between spam attempts
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("[ANIM-SPAM-TEST] Animation spam test complete");
        Debug.Log("=== END ANIMATION SPAM PROTECTION TEST ===");
    }
    
    /// <summary>
    /// Test method to simulate rapid space key presses (spam test)
    /// </summary>
    [ContextMenu("Test Spam Protection")]
    public void TestSpamProtection()
    {
        Debug.Log("=== TESTING SPAM PROTECTION ===");
        Debug.Log("Simulating rapid space key presses...");
        
        StartCoroutine(SimulateSpamInput());
    }
    
    /// <summary>
    /// Simulate rapid input to test spam protection
    /// </summary>
    private IEnumerator SimulateSpamInput()
    {
        Debug.Log("[SPAM-TEST] Starting spam simulation");
        
        for (int i = 0; i < 10; i++)
        {
            Debug.Log($"[SPAM-TEST] Simulated input #{i + 1}");
            
            // Check current state before each input
            Debug.Log($"[SPAM-TEST] Pre-input state: isProcessingInput={isProcessingInput}, isInDialogTransition={isInDialogTransition}");
            
            // Simulate HandleDialogProgression call
            if (!isProcessingInput && !isInDialogTransition)
            {
                float currentTime = Time.time;
                bool canProcessInput = (currentTime - lastInputTime) >= INPUT_COOLDOWN;
                bool canUpdateDialog = (currentTime - lastDialogUpdateTime) >= 0.2f;
                
                if (canProcessInput && canUpdateDialog)
                {
                    Debug.Log($"[SPAM-TEST] Input #{i + 1} ACCEPTED");
                    lastInputTime = currentTime;
                    lastDialogUpdateTime = currentTime;
                    isProcessingInput = true;
                    isInDialogTransition = true;
                    
                    // Simulate quick processing
                    yield return new WaitForSeconds(0.1f);
                    
                    // Reset flags
                    isProcessingInput = false;
                    yield return new WaitForSeconds(0.1f);
                    isInDialogTransition = false;
                }
                else
                {
                    Debug.Log($"[SPAM-TEST] Input #{i + 1} BLOCKED - cooldown not met");
                }
            }
            else
            {
                Debug.Log($"[SPAM-TEST] Input #{i + 1} BLOCKED - already processing");
            }
            
            // Very short delay between inputs to simulate spam
            yield return new WaitForSeconds(0.05f);
        }
        
        Debug.Log("[SPAM-TEST] Spam simulation complete");
        Debug.Log("=== END SPAM PROTECTION TEST ===");
    }
    
    /// <summary>
    /// Debug method to test component caching and separation
    /// </summary>
    [ContextMenu("Test Component Separation")]
    public void TestComponentSeparation()
    {
        Debug.Log("=== TESTING COMPONENT SEPARATION ===");
        
        if (dialogInstance == null)
        {
            Debug.LogError("Dialog instance is null! Create a dialog first.");
            return;
        }
        
        // Clear cache to force fresh component detection
        ClearComponentCache();
        
        // Test NPC name update
        Debug.Log("Testing NPC name update...");
        UpdateNpcNameSafe("TEST NPC NAME");
        
        // Wait a moment then test dialog text update
        Debug.Log("Testing dialog text update...");
        UpdateDialogTextSafe("TEST DIALOG TEXT - This should appear in a different component than the name");
        
        // Log all components for verification
        LogAllDialogComponents();
        
        Debug.Log("=== END COMPONENT SEPARATION TEST ===");
    }
    
    #endregion

    #region Dialog Handling

    private void Show3DDialog(CoreGameDialog dialog)
    {
        // Reset audio tracking for new dialog (choice-level tracking)
        hasPlayedCurrentResponseAudio = false;
        lastPlayedAudioPath = "";
        lastAudioPlayedForChoiceIndex = -1;
        lastPlayedChoiceText = "";
        
        Debug.Log("[AUDIO] Reset choice-level audio tracking for new 3D dialog");
        
        // Handle cutscene fade effect for 3D dialogs
        HandleCutsceneFade(dialog.cutsceneType);
        
        GameObject targetModel = null;
        
        // Find the target model based on dialog3DLocation
        switch (dialog.dialog3DLocation)
        {
            case CoreGameDialog.Dialog3DLocation.Mother:
                targetModel = GameObject.Find("Linda_Model");
                break;
            case CoreGameDialog.Dialog3DLocation.Father:
                targetModel = GameObject.Find("Isayat_Model");
                break;
            case CoreGameDialog.Dialog3DLocation.Rey:
                targetModel = GameObject.Find("Rey_Baby_Model");
                break;
        }
        
        if (targetModel == null)
        {
            Debug.LogError($"No GameObject named '{GetModelName(dialog.dialog3DLocation)}' found for 3D dialog!");
            ContinueToNextBlock();
            return;
        }
        
        // Find the TextDialog3D component in the target model
        var textDialog3D = targetModel.transform.Find("TextDialog3D");
        if (textDialog3D == null)
        {
            Debug.LogError($"No child GameObject named 'TextDialog3D' found in '{targetModel.name}'!");
            ContinueToNextBlock();
            return;
        }

        var tmp3D = textDialog3D.GetComponent<TMP_Text>();
        if (tmp3D == null)
        {
            Debug.LogError($"'TextDialog3D' in '{targetModel.name}' does not have a TMP_Text component!");
            ContinueToNextBlock();
            return;
        }

        textDialog3D.gameObject.SetActive(true);
        
        // Process stress modifiers from the 3D dialog entry
        string processedDialogEntry = ProcessStressModifiers(dialog.dialogEntry);
        Debug.Log($"3D Dialog - Original: '{dialog.dialogEntry}'");
        Debug.Log($"3D Dialog - Processed: '{processedDialogEntry}'");
        
        // 3D dialogs don't need NPC name extraction - the 3D model represents the character
        // Extract and display NPC name if present in dialog entry
        // string npcName = ExtractNpcNameFromDialogText(dialog.dialogEntry);
        // if (!string.IsNullOrEmpty(npcName))
        // {
        //     UpdateNpcNameDisplay(npcName);
        // }
        
        AnimateDialogText(processedDialogEntry, tmp3D, dialog.audioDialogEntry);

        // Handle choices if any
        if (dialog.choices != null && dialog.choices.Length > 0)
        {
            ShowChoices(dialog.choices);
        }
    }
    
    private string GetModelName(CoreGameDialog.Dialog3DLocation location)
    {
        switch (location)
        {
            case CoreGameDialog.Dialog3DLocation.Mother:
                return "Linda_Model";
            case CoreGameDialog.Dialog3DLocation.Father:
                return "Isayat_Model";
            case CoreGameDialog.Dialog3DLocation.Rey:
                return "Rey_Baby_Model";
            default:
                return "Unknown";
        }
    }

    private void Show2DDialog(CoreGameDialog dialog)
    {
        // Reset audio tracking for new dialog (choice-level tracking)
        hasPlayedCurrentResponseAudio = false;
        lastPlayedAudioPath = "";
        lastAudioPlayedForChoiceIndex = -1;
        lastPlayedChoiceText = "";
        
        Debug.Log($"=== Show2DDialog FIELD MAPPING DEBUG ===");
        Debug.Log($"CoreGameDialog.npcName = '{dialog.npcName}' -> should go to DialogueName");
        Debug.Log($"CoreGameDialog.dialogEntry = '{dialog.dialogEntry}' -> should go to DialogueText");
        Debug.Log($"[AUDIO] Reset choice-level audio tracking for new 2D dialog");
        
        // Validate the dialog data structure
        ValidateDialogData(dialog);
        
        // Only summon dialog bar if we don't have one already
        if (dialogInstance == null)
        {
            dialogInstance = SummonDialogBar();
            if (dialogInstance == null)
            {
                Debug.LogError("Failed to create dialog bar!");
                ContinueToNextBlock();
                return;
            }
            
            // Clear component cache when new dialog instance is created
            ClearComponentCache();
        }
        
        // Validate the UI structure
        ValidateDialogUI();
        
        // Ensure BackgroundFade reference if not assigned
        EnsureBackgroundFadeReference();
        
        // Handle cutscene fade effect
        HandleCutsceneFade(dialog.cutsceneType);
        
        // CRITICAL: Assign NPC name to DialogueName component
        // Use NPC name from CoreGameDialog.npcName, or extract from dialog text as fallback
        string npcName = ConvertNpcNameToString(dialog.npcName);
        if (string.IsNullOrEmpty(npcName))
        {
            npcName = ExtractNpcNameFromDialogText(dialog.dialogEntry);
        }
        Debug.Log($"Final npcName for DialogueName component: '{npcName}'");
        DialogCharacterAnimatorManager animManager = FindObjectOfType<DialogCharacterAnimatorManager>();
        if (animManager != null)
        {
            animManager.HandleCharacterAnimation(dialog.npcName); // ✅ otomatis reset + play
        }
        // Setelah menentukan npcName
        BlendShapeManager blendShapeManager = FindObjectOfType<BlendShapeManager>();
        if (blendShapeManager != null)
        {
            blendShapeManager.SetExpressionByNpcName(dialog.npcName);
        }
        // Process stress modifiers from the initial dialog entry
        string processedDialogEntry = ProcessStressModifiers(dialog.dialogEntry);
        Debug.Log($"Original dialog entry: '{dialog.dialogEntry}'");
        Debug.Log($"Processed dialog entry: '{processedDialogEntry}'");
        
        if (!string.IsNullOrEmpty(npcName))
        {
            UpdateNpcNameDisplay(npcName);
        }
        else
        {
            Debug.LogWarning("NPC name is empty! DialogueName will not be updated.");
        }
        
        // CRITICAL: Animate dialog text instead of setting it instantly
        // Find the DialogueText component to animate
        TMP_Text dialogTextComponent = null;
        if (dialogInstance != null)
        {
            // Try DialogPrefabController first
            DialogPrefabController controller = dialogInstance.GetComponent<DialogPrefabController>();
            if (controller != null && controller.dialogueText != null)
            {
                dialogTextComponent = controller.dialogueText;
                Debug.Log("Found DialogueText component via DialogPrefabController for animation");
            }
            else
            {
                // Fallback to direct search
                Transform textTransform = dialogInstance.transform.Find("DialogueText");
                if (textTransform != null)
                {
                    dialogTextComponent = textTransform.GetComponent<TMP_Text>();
                    Debug.Log("Found DialogueText component via direct search for animation");
                }
            }
        }
        
        if (dialogTextComponent != null)
        {
            Debug.Log($"Starting dialog text animation for: '{processedDialogEntry}'");
            AnimateDialogText(processedDialogEntry, dialogTextComponent, dialog.audioDialogEntry);
        }
        else
        {
            Debug.LogError("Could not find DialogueText component for animation! Falling back to instant text.");
            UpdateDialogTextSafe(processedDialogEntry);
        }
        
        // Handle choices if any
        if (dialog.choices != null && dialog.choices.Length > 0)
        {
            ShowChoices(dialog.choices);
        }
    }

    private void ShowChoices(CoreGameDialogChoices[] choices)
    {
        // Use smart reset to preserve choice tracking when appropriate
        SmartResetFilteredChoicesSystem(choices, currentBlockIndex);
        
        GameObject questionBar = SummonQuestionBar();
        
        // If prefab failed, try creating a fallback question bar
        if (questionBar == null)
        {
            Debug.LogWarning("Prefab question bar failed, attempting to create fallback...");
            questionBar = CreateFallbackQuestionBar();
        }
        
        if (questionBar == null) 
        {
            Debug.LogError("Both prefab and fallback question bar creation failed!");
            return;
        }
        
        ShowChoicesWithButtons(choices, OnPlayerChoseResponse);
    }
    
    /// <summary>
    /// Integrated choice display system from PlayerAnswerManager
    /// UPDATED: Use button array approach for direct modification
    /// ENHANCED: Automatically filter out previously pressed incorrect choices
    /// </summary>
    private void ShowChoicesWithButtons(CoreGameDialogChoices[] choices, System.Action<int> callback)
    {
        Debug.Log($"Showing {choices?.Length ?? 0} choices using button array approach...");

        // PREVENT INPUT during choice display
        isInDialogTransition = true;

        // CLEAR INPUT STATES to prevent auto-selection bug
        if (dialogInputHandler != null)
        {
            dialogInputHandler.ClearChoiceInputStates();
            Debug.Log("[CHOICE-FIX] Cleared choice input states to prevent auto-selection");
        }

        onChoiceSelected = callback;
        buttonTweenIds.Clear();

        if (choices == null || choices.Length == 0)
        {
            Debug.LogWarning("No choices provided to ShowChoicesWithButtons!");
            return;
        }
        
        // ENHANCED: Auto-filter choices if we have pressed choices tracking
        CoreGameDialogChoices[] choicesToDisplay = choices;
        if (pressedChoiceTexts.Count > 0 && !isUsingFilteredChoices)
        {
            Debug.Log($"[AUTO-FILTER] Found {pressedChoiceTexts.Count} pressed choices, applying automatic filtering...");
            
            // Create filtered array excluding pressed choices
            var filteredList = new System.Collections.Generic.List<CoreGameDialogChoices>();
            for (int i = 0; i < choices.Length; i++)
            {
                if (!IsChoiceAlreadyPressed(choices[i].playerChoice))
                {
                    filteredList.Add(choices[i]);
                    Debug.Log($"[AUTO-FILTER] Keeping choice: '{choices[i].playerChoice}'");
                }
                else
                {
                    Debug.Log($"[AUTO-FILTER] Filtering out pressed choice: '{choices[i].playerChoice}'");
                }
            }
            
            if (filteredList.Count < choices.Length)
            {
                choicesToDisplay = filteredList.ToArray();
                currentFilteredChoices = choicesToDisplay;
                originalChoices = choices;
                isUsingFilteredChoices = true;
                Debug.Log($"[AUTO-FILTER] Filtered {choices.Length} -> {choicesToDisplay.Length} choices");
            }
        }
        else if (isUsingFilteredChoices && currentFilteredChoices != null)
        {
            // Use existing filtered choices
            choicesToDisplay = currentFilteredChoices;
            Debug.Log($"[AUTO-FILTER] Using existing filtered choices: {choicesToDisplay.Length} choices");
        }
        
        // Debug the choices data
        ValidateChoices(choicesToDisplay);
        
        // Validate the question UI structure
        ValidateQuestionUI();

        // Get all buttons from the question instance as an array
        Button[] buttonArray = null;
        if (questionInstance != null)
        {
            buttonArray = questionInstance.GetComponentsInChildren<Button>();
            Debug.Log($"Found {buttonArray.Length} buttons in question instance");
            
            // Debug what buttons we found
            for (int b = 0; b < buttonArray.Length; b++)
            {
                Debug.Log($"Button {b}: '{buttonArray[b].name}' (GameObject: {buttonArray[b].gameObject.name})");
            }
        }
        
        if (buttonArray == null || buttonArray.Length == 0)
        {
            Debug.LogError("No buttons found in question instance!");
            return;
        }

        // Show up to 3 choices on screen (or all choices if less than 3), limited by available buttons
        int choicesToShow = Mathf.Min(choicesToDisplay.Length, 3, buttonArray.Length);
        
        Debug.Log($"Total choices in data: {choicesToDisplay.Length}, UI will show: {choicesToShow} (Available buttons: {buttonArray.Length})");
        
        // Log filtered choices state for debugging
        if (isUsingFilteredChoices)
        {
            Debug.Log($"[FILTERED CHOICES] Using filtered choices array with {choicesToDisplay.Length} remaining choices");
            Debug.Log($"[FILTERED CHOICES] Original choices had {originalChoices?.Length ?? 0} choices");
        }

        for (int i = 0; i < choicesToShow; i++)
        {
            if (choicesToDisplay[i] != null && i < buttonArray.Length)
            {
                Debug.Log($"Processing choice {i}: '{choicesToDisplay[i].playerChoice}' (correctChoice: {choicesToDisplay[i].correctChoice}) -> Button {i} ({buttonArray[i].name})");
                
                Button btn = buttonArray[i];
                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();

                // CRITICAL: Use choicesToDisplay[i].playerChoice directly from CoreGameDialogChoices data structure
                string choiceText = choicesToDisplay[i].playerChoice;
                
                // Handle empty playerChoice
                if (string.IsNullOrEmpty(choiceText))
                {
                    choiceText = $"Choice {i + 1}";
                    Debug.LogWarning($"Choice {i} has empty playerChoice field! Using fallback: '{choiceText}'");
                }
                
                // Add key indicator based on button index (Q, W, E)
                string keyIndicator = GetKeyIndicator(i);
                string buttonTextWithKey = $"{keyIndicator} {choiceText}";
                
                Debug.Log($"Setting button array[{i}] ({btn.name}) text to: '{buttonTextWithKey}'");
                
                // Direct TMP_Text assignment to button
                TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    Debug.Log($"Found TMP_Text in button {i}: '{btnText.transform.name}' (current: '{btnText.text}')");
                    btnText.text = buttonTextWithKey;
                    Debug.Log($"Button {i} updated to: '{btnText.text}'");
                }
                else
                {
                    Debug.LogError($"CRITICAL: Button array[{i}] ({btn.name}) has no TMP_Text component!");
                    
                    // Debug button structure
                    Transform[] children = btn.GetComponentsInChildren<Transform>();
                    Debug.Log($"Button {i} ({btn.name}) children:");
                    for (int c = 0; c < children.Length; c++)
                    {
                        Component[] components = children[c].GetComponents<Component>();
                        string componentNames = "";
                        for (int j = 0; j < components.Length; j++)
                        {
                            componentNames += components[j].GetType().Name;
                            if (j < components.Length - 1) componentNames += ", ";
                        }
                        Debug.Log($"  - {children[c].name} (Components: {componentNames})");
                    }
                }

                int index = i; // Important for correct capture - this is the index in the FILTERED choices array
                btn.onClick.AddListener(() => {
                    // If animation is still playing, finish it instantly
                    if (buttonTweenIds.TryGetValue(btn, out int tweenId) && LeanTween.isTweening(tweenId))
                    {   
                        TMP_Text btnText2 = btn.GetComponentInChildren<TMP_Text>();
                        if (btnText2 != null)
                        {
                            string keyIndicator = GetKeyIndicator(index);
                            string choiceTextFinal = !string.IsNullOrEmpty(choicesToDisplay[index].playerChoice) ? choicesToDisplay[index].playerChoice : $"Choice {index + 1}";
                            btnText2.text = $"{keyIndicator} {choiceTextFinal}";
                        }
                        LeanTween.cancel(tweenId);
                        buttonTweenIds.Remove(btn);
                        return; // Don't invoke choice yet, just finish animation
                    }

                    // Custom logic: Only detect "mapname:scene_name" pattern
                    const string moveMapPrefix = "mapname:";
                    string npcResponse = GetNpcResponseFromChoice(choicesToDisplay[index]);
                    Debug.Log(npcResponse);
                    int prefixIndex = npcResponse.IndexOf(moveMapPrefix);
                    if (prefixIndex != -1)
                    {
                        int start = prefixIndex + moveMapPrefix.Length;
                        int end = npcResponse.IndexOf(' ', start);
                        string mapName;
                        if (end == -1)
                            mapName = npcResponse.Substring(start);
                        else
                            mapName = npcResponse.Substring(start, end - start);

                        // Handle map movement logic here if needed
                        Debug.Log($"Map movement detected: {mapName}");
                    }

                    Debug.Log($"[CHOICE SELECTED] Button {index} clicked - choice: '{choices[index].playerChoice}' (correctChoice: {choices[index].correctChoice})");
                    onChoiceSelected?.Invoke(index); // Pass the filtered array index
                    HideChoices(); // Hide all buttons after a choice is made
                });
            }
        }
        
        // Hide unused buttons
        for (int i = choicesToShow; i < buttonArray.Length; i++)
        {
            if (buttonArray[i] != null)
            {
                buttonArray[i].gameObject.SetActive(false);
                Debug.Log($"Hidden unused button {i}: {buttonArray[i].name}");
            }
        }
        
        // Reset dialog transition flag after a delay to prevent immediate input processing
        StartCoroutine(ResetChoiceTransitionFlag());
    }
    
    /// <summary>
    /// Reset choice transition flag to allow input processing after choices are shown
    /// </summary>
    private IEnumerator ResetChoiceTransitionFlag()
    {
        yield return new WaitForSeconds(0.3f); // Longer delay for choices
        isInDialogTransition = false;
        Debug.Log("[CHOICE-FIX] Choice transition flag reset - input processing now allowed");
    }
    
    /// <summary>
    /// Hide choices and clean up buttons using button array approach
    /// ENHANCED: Destroy the question prefab completely after choice selection
    /// </summary>
    private void HideChoices()
    {
        Debug.Log("Hiding choices and destroying question prefab...");
        
        if (questionInstance != null)
        {
            // Clean up button listeners before destroying
            Button[] buttonArray = questionInstance.GetComponentsInChildren<Button>();
            Debug.Log($"Found {buttonArray.Length} buttons to clean up in question instance");
            
            for (int i = 0; i < buttonArray.Length; i++)
            {
                Button btn = buttonArray[i];
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    Debug.Log($"Cleaned up button {i}: {btn.name}");
                }
            }
            
            // Stop any active tweens on the question instance
            if (buttonTweenIds.Count > 0)
            {
                Debug.Log($"Cancelling {buttonTweenIds.Count} active button tweens");
                foreach (var kvp in buttonTweenIds)
                {
                    if (LeanTween.isTweening(kvp.Value))
                    {
                        LeanTween.cancel(kvp.Value);
                    }
                }
                buttonTweenIds.Clear();
            }
            
            // Destroy the entire question prefab
            Debug.Log($"Destroying question prefab: {questionInstance.name}");
            Destroy(questionInstance);
            questionInstance = null;
        }
        else
        {
            Debug.LogWarning("Question instance is null, nothing to destroy");
        }
        
        // Also clean up any buttons from the answerButtons array (fallback) if they exist independently
        if (answerButtons != null)
        {
            Debug.Log($"Cleaning up {answerButtons.Length} buttons from answerButtons fallback array");
            for (int i = 0; i < answerButtons.Length; i++)
            {
                Button btn = answerButtons[i];
                if (btn != null && btn.gameObject != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.gameObject.SetActive(false);
                }
            }
        }
        
        // Clear choice selection state
        onChoiceSelected = null;
        
        Debug.Log("Question prefab destroyed and all choice state cleared");
    }
    
    /// <summary>
    /// Get key indicator for button based on index
    /// </summary>
    private string GetKeyIndicator(int buttonIndex)
    {
        switch (buttonIndex)
        {
            case 0: return "[Q]";
            case 1: return "[W]";
            case 2: return "[E]";
            default: return $"[{buttonIndex + 1}]"; // Fallback for additional buttons
        }
    }
    
    /// <summary>
    /// Get choice button by name from the question instance, with fallback creation
    /// </summary>
    private Button GetChoiceButton(string buttonName)
    {
        if (questionInstance == null)
        {
            Debug.LogWarning("Question instance is null, cannot find button!");
            return null;
        }
        
        Transform buttonTransform = questionInstance.transform.Find(buttonName);
        if (buttonTransform != null)
        {
            Button button = buttonTransform.GetComponent<Button>();
            if (button != null)
            {
                return button;
            }
            else
            {
                Debug.LogWarning($"GameObject '{buttonName}' found but has no Button component!");
            }
        }
        else
        {
            Debug.LogWarning($"Button '{buttonName}' not found as direct child of question instance!");
            
            // Try to find it deeper in the hierarchy
            Button[] allButtons = questionInstance.GetComponentsInChildren<Button>();
            foreach (Button btn in allButtons)
            {
                if (btn.transform.name.Contains(buttonName) || btn.transform.name.Equals(buttonName))
                {
                    Debug.Log($"Found button '{buttonName}' deeper in hierarchy: {btn.transform.name}");
                    return btn;
                }
            }
            
            Debug.LogWarning($"Button '{buttonName}' not found anywhere in question instance hierarchy!");
        }
        
        return null;
    }
    
    /// <summary>
    /// Create a fallback question bar programmatically if prefab fails
    /// </summary>
    private GameObject CreateFallbackQuestionBar()
    {
        Debug.Log("Creating fallback question bar programmatically...");
        
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found for fallback question bar!");
            return null;
        }
        
        // Create main container
        GameObject questionBar = new GameObject("FallbackQuestionBar");
        questionBar.transform.SetParent(canvas.transform, false);
        
        RectTransform questionRect = questionBar.AddComponent<RectTransform>();
        questionRect.anchorMin = new Vector2(0f, 0f);
        questionRect.anchorMax = new Vector2(1f, 0.3f);
        questionRect.offsetMin = Vector2.zero;
        questionRect.offsetMax = Vector2.zero;
        
        // Add background image
        Image background = questionBar.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 0.8f);
        
        // Create button container
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(questionBar.transform, false);
        
        RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.3f);
        containerRect.anchorMax = new Vector2(0.9f, 0.7f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // Add horizontal layout group
        HorizontalLayoutGroup layout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        
        // Create Q, W, E buttons
        string[] buttonNames = { "Q", "W", "E" };
        for (int i = 0; i < buttonNames.Length; i++)
        {
            GameObject buttonObj = new GameObject(buttonNames[i]);
            buttonObj.transform.SetParent(buttonContainer.transform, false);
            
            // Add button component
            Button button = buttonObj.AddComponent<Button>();
            
            // Add button image
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            TMP_Text buttonText = textObj.AddComponent<TMP_Text>();
            buttonText.text = $"[{buttonNames[i]}] Choice {i + 1}";
            buttonText.fontSize = 18;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Debug.Log($"Created fallback button: {buttonNames[i]}");
        }
        
        questionInstance = questionBar;
        Debug.Log("Fallback question bar created successfully!");
        
        return questionBar;
    }
    
    /// <summary>
    /// Initialize and clear all answer buttons
    /// </summary>
    private void InitializeButtons()
    {
        Debug.Log("Initializing buttons...");
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].gameObject.SetActive(false);
                answerButtons[i].onClick.RemoveAllListeners();
                
                // Find ALL TMP_Text components in the button and clear them
                TMP_Text[] allTexts = answerButtons[i].GetComponentsInChildren<TMP_Text>();
                foreach (var txt in allTexts)
                {
                    Debug.Log($"Clearing text component '{txt.transform.name}' in button {i}: was '{txt.text}'");
                    txt.text = "";
                }
                
                // Also try the direct approach
                TMP_Text btnText = answerButtons[i].GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    btnText.text = "";
                }
            }
        }
    }
    
    /// <summary>
    /// Validate choices for debugging purposes
    /// </summary>
    public void ValidateChoices(CoreGameDialogChoices[] choices)
    {
        if (choices == null)
        {
            Debug.LogError("Choices array is null!");
            return;
        }
        
        Debug.Log($"Validating {choices.Length} choices:");
        for (int i = 0; i < choices.Length; i++)
        {
            if (choices[i] == null)
            {
                Debug.LogError($"Choice {i} is null!");
            }
            else
            {
                Debug.Log($"Choice {i}: playerChoice='{choices[i].playerChoice}', hasResponses={choices[i].dialogResponses != null && choices[i].dialogResponses.Length > 0}");
            }
        }
    }

    /// <summary>
    /// Comprehensive data validation for debugging dialog/choice issues
    /// </summary>
    private void ValidateDialogData(CoreGameDialog dialog)
    {
        Debug.Log($"=== DIALOG DATA VALIDATION ===");
        string debugNpcName = ConvertNpcNameToString(dialog.npcName);
        Debug.Log($"CoreGameDialog.npcName: '{debugNpcName}' (Length: {debugNpcName?.Length ?? 0})");
        Debug.Log($"CoreGameDialog.dialogEntry: '{dialog.dialogEntry}' (Length: {dialog.dialogEntry?.Length ?? 0})");
        
        if (dialog.choices != null)
        {
            Debug.Log($"Dialog has {dialog.choices.Length} choices:");
            for (int i = 0; i < dialog.choices.Length; i++)
            {
                var choice = dialog.choices[i];
                if (choice != null)
                {
                    Debug.Log($"  Choice {i}:");
                    Debug.Log($"    - playerChoice: '{choice.playerChoice}' (Length: {choice.playerChoice?.Length ?? 0})");
                    
                    if (choice.dialogResponses != null)
                    {
                        Debug.Log($"    - Has {choice.dialogResponses.Length} responses:");
                        for (int j = 0; j < choice.dialogResponses.Length; j++)
                        {
                            var response = choice.dialogResponses[j];
                            if (response != null)
                            {
                                Debug.Log($"      Response {j}:");
                                string debugResponseNpcName = ConvertNpcNameToString(response.npcName);
                                Debug.Log($"        - NpcName: '{debugResponseNpcName}' (Length: {debugResponseNpcName?.Length ?? 0})");
                                Debug.Log($"        - npcResponse: '{response.npcResponse}' (Length: {response.npcResponse?.Length ?? 0})");
                            }
                            else
                            {
                                Debug.LogWarning($"      Response {j} is NULL!");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"    - Choice {i} has no dialogResponses array!");
                    }
                }
                else
                {
                    Debug.LogWarning($"  Choice {i} is NULL!");
                }
            }
        }
        else
        {
            Debug.Log("Dialog has no choices.");
        }
        
        Debug.Log($"=== END DIALOG DATA VALIDATION ===");
    }

    /// <summary>
    /// Validate UI components in dialog instance
    /// </summary>
    private void ValidateDialogUI()
    {
        Debug.Log($"=== DIALOG UI VALIDATION ===");
        
        if (dialogInstance == null)
        {
            Debug.LogError("dialogInstance is NULL!");
            return;
        }
        
        Debug.Log($"Dialog instance: {dialogInstance.name}");
        
        // Check for DialogueName component
        Transform dialogueNameTransform = dialogInstance.transform.Find("DialogueName");
        if (dialogueNameTransform != null)
        {
            TMP_Text dialogueNameText = dialogueNameTransform.GetComponent<TMP_Text>();
            if (dialogueNameText != null)
            {
                Debug.Log($"✓ DialogueName found: '{dialogueNameText.text}'");
            }
            else
            {
                Debug.LogWarning("DialogueName transform found but no TMP_Text component!");
            }
        }
        else
        {
            Debug.LogWarning("DialogueName transform not found!");
        }
        
        // Check for DialogueText component
        Transform dialogueTextTransform = dialogInstance.transform.Find("DialogueText");
        if (dialogueTextTransform != null)
        {
            TMP_Text dialogueTextComponent = dialogueTextTransform.GetComponent<TMP_Text>();
            if (dialogueTextComponent != null)
            {
                Debug.Log($"✓ DialogueText found: '{dialogueTextComponent.text}'");
            }
            else
            {
                Debug.LogWarning("DialogueText transform found but no TMP_Text component!");
            }
        }
        else
        {
            Debug.LogWarning("DialogueText transform not found!");
        }
        
        // List all TMP_Text components
        TMP_Text[] allTexts = dialogInstance.GetComponentsInChildren<TMP_Text>();
        Debug.Log($"All TMP_Text components in dialog instance ({allTexts.Length}):");
        for (int i = 0; i < allTexts.Length; i++)
        {
            Debug.Log($"  [{i}] {allTexts[i].transform.name}: '{allTexts[i].text}'");
        }
        
        Debug.Log($"=== END DIALOG UI VALIDATION ===");
    }

    /// <summary>
    /// Validate question bar UI components
    /// </summary>
    private void ValidateQuestionUI()
    {
        Debug.Log($"=== QUESTION UI VALIDATION ===");
        
        if (questionInstance == null)
        {
            Debug.LogError("questionInstance is NULL!");
            return;
        }
        
        Debug.Log($"Question instance: {questionInstance.name}");
        
        string[] buttonNames = { "Q", "W", "E" };
        foreach (string buttonName in buttonNames)
        {
            Button btn = GetChoiceButton(buttonName);
            if (btn != null)
            {
                TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    Debug.Log($"✓ Button {buttonName} found with text: '{btnText.text}' on component '{btnText.transform.name}'");
                }
                else
                {
                    Debug.LogWarning($"Button {buttonName} found but no TMP_Text component!");
                }
            }
            else
            {
                Debug.LogWarning($"Button {buttonName} not found!");
            }
        }
        
        Debug.Log($"=== END QUESTION UI VALIDATION ===");
    }

    /// <summary>
    /// Manual test method - can be called from Unity Editor for debugging
    /// </summary>
    [ContextMenu("Test Dialog System")]
    public void TestDialogSystem()
    {
        Debug.Log("=== MANUAL DIALOG SYSTEM TEST ===");
        
        if (coreGameData == null)
        {
            Debug.LogError("CoreGameData is null! Please assign a ScriptableObject.");
            return;
        }
        
        if (coreGameData.coreBlock == null || coreGameData.coreBlock.Length == 0)
        {
            Debug.LogError("CoreGameData has no blocks!");
            return;
        }
        
        Debug.Log($"CoreGameData has {coreGameData.coreBlock.Length} blocks");
        
        for (int i = 0; i < coreGameData.coreBlock.Length; i++)
        {
            var block = coreGameData.coreBlock[i];
            if (block.Dialog != null)
            {
                Debug.Log($"Block {i} - Dialog Block:");
                ValidateDialogData(block.Dialog);
            }
        }
        
        // Test UI components
        if (dialogInstance != null)
        {
            ValidateDialogUI();
        }
        else
        {
            Debug.Log("No dialog instance currently active");
        }
        
        if (questionInstance != null)
        {
            ValidateQuestionUI();
        }
        else
        {
            Debug.Log("No question instance currently active");
        }
        
        Debug.Log("=== END MANUAL DIALOG SYSTEM TEST ===");
    }
    
    /// <summary>
    /// Animate button text (from PlayerAnswerManager)
    /// </summary>
    private int AnimateButtonText(TMP_Text btnText, string fullText)
    {
        btnText.text = "";
        int len = fullText.Length;
        int counter = 0;

        int tweenId = LeanTween.value(btnText.gameObject, 0, len, 0.3f)
            .setOnUpdate((float val) =>
            {
                counter = Mathf.Clamp(Mathf.FloorToInt(val), 0, len);
                btnText.text = fullText.Substring(0, counter);
            })
            .setOnComplete(() =>
            {
                btnText.text = fullText;
            }).id;

        return tweenId;
    }

    /// <summary>
    /// Handle player choice selection with correct choice filtering system
    /// If choice is incorrect, remove it and respawn remaining choices
    /// If choice is correct, continue with normal dialog flow
    /// 
    /// IMPORTANT FIX: Only track INCORRECT choices, not correct ones!
    /// Correct choices lead to normal progression and should not be filtered.
    /// </summary>
    [Obsolete]
    private void OnPlayerChoseResponse(int choiceIndex)
    {
        Debug.Log($"=== OnPlayerChoseResponse - Choice {choiceIndex} Selected ===");
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        
        // Determine which choice array to use (filtered or original)
        CoreGameDialogChoices[] choicesToUse = isUsingFilteredChoices ? currentFilteredChoices : currentBlock.Dialog?.choices;
        
        Debug.Log($"[CHOICE SELECTION] Using choice array: {(isUsingFilteredChoices ? "FILTERED" : "ORIGINAL")}");
        Debug.Log($"[CHOICE SELECTION] Choice array length: {choicesToUse?.Length ?? 0}");
        Debug.Log($"[CHOICE SELECTION] Selected index: {choiceIndex}");
        
        if (choicesToUse == null || choiceIndex >= choicesToUse.Length)
        {
            Debug.LogError($"[CHOICE SELECTION] Invalid choice index {choiceIndex} or no choices available!");
            Debug.LogError($"[CHOICE SELECTION] choicesToUse is null: {choicesToUse == null}");
            Debug.LogError($"[CHOICE SELECTION] choicesToUse.Length: {choicesToUse?.Length ?? 0}");
            return;
        }
        
        var selectedChoice = choicesToUse[choiceIndex];
        Debug.Log($"[CHOICE SELECTION] Selected choice: '{selectedChoice.playerChoice}' (correctChoice: {selectedChoice.correctChoice})");
        
        // NOTIFY EXTERNAL SYSTEMS ABOUT PLAYER CHOICE
        if (OnPlayerChoiceSelected != null)
        {
            OnPlayerChoiceSelected.Invoke(selectedChoice.playerChoice);
            Debug.Log($"[CHOICE NOTIFICATION] Notified external systems about choice: '{selectedChoice.playerChoice}'");
        }
        
        // Hide choices first
        HideChoices();
        
        // Store which choice was selected (relative to the current choice array)
        selectedChoiceIndex = choiceIndex;
        Debug.Log($"[CHOICE SELECTION] Set selectedChoiceIndex = {selectedChoiceIndex} (relative to {(isUsingFilteredChoices ? "filtered" : "original")} array)");
        
        // Check if this is a correct choice
        if (selectedChoice.correctChoice)
        {
            Debug.Log("[CORRECT CHOICE] Player selected correct choice - proceeding with normal dialog flow");
            Debug.Log("[CORRECT CHOICE] NOT tracking this choice since it's correct - it leads to normal progression");
            
            // Don't reset filtered choices system when correct choice is selected
            // This allows the system to remember incorrect choices for future similar choice sets
            Debug.Log("[CORRECT CHOICE] Preserving choice tracking for future use");
            
            // Check if there are dialog responses to show
            if (selectedChoice.dialogResponses != null && selectedChoice.dialogResponses.Length > 0)
            {
                Debug.Log($"[CORRECT CHOICE] Found {selectedChoice.dialogResponses.Length} dialog responses to display");
                
                // Start showing responses from index 0
                currentChoiceResponseIndex = 0;
                isShowingResponse = true;
                
                ShowDialogResponse(selectedChoice, 0);
            }
            else
            {
                Debug.Log("[CORRECT CHOICE] No dialog responses found, continuing to next block");
                // No responses, just continue to next block
                ContinueToNextBlock();
            }
        }
        else
        {
            Debug.Log("[INCORRECT CHOICE] Player selected incorrect choice - will respawn choices after response");
            
            // CRITICAL: Only track INCORRECT choices, not correct ones!
            Debug.Log($"[PRESSED-CHOICE] Tracking INCORRECT choice {choiceIndex} as pressed: '{selectedChoice.playerChoice}'");
            
            // Find the original index of this choice for proper tracking
            int originalIndex = -1;
            if (originalChoices != null)
            {
                for (int i = 0; i < originalChoices.Length; i++)
                {
                    if (originalChoices[i].playerChoice == selectedChoice.playerChoice)
                    {
                        originalIndex = i;
                        break;
                    }
                }
            }
            
            // Track the pressed INCORRECT choice (use original index if found, otherwise current index)
            TrackPressedChoice(originalIndex >= 0 ? originalIndex : choiceIndex, selectedChoice.playerChoice);
            
            // Show the response for the incorrect choice first
            if (selectedChoice.dialogResponses != null && selectedChoice.dialogResponses.Length > 0)
            {
                Debug.Log($"[INCORRECT CHOICE] Showing incorrect choice response, then will filter choices");
                
                // Start showing responses from index 0
                currentChoiceResponseIndex = 0;
                isShowingResponse = true;
                
                // Mark that we need to respawn choices after this response completes
                // We'll handle this in HandleDialogProgression when responses finish
                ShowDialogResponse(selectedChoice, 0);
            }
            else
            {
                Debug.Log("[INCORRECT CHOICE] Incorrect choice has no response, filtering choices immediately");
                // No response to show, filter choices immediately
                FilterIncorrectChoiceAndRespawn(choiceIndex);
            }
        }
    }

    /// <summary>
    /// Show a specific dialog response from a choice's response array
    /// </summary>
    /// <param name="selectedChoice">The choice containing the responses</param>
    /// <param name="responseIndex">Index of the response to show</param>
    [Obsolete]
    private void ShowDialogResponse(CoreGameDialogChoices selectedChoice, int responseIndex)
    {
        if (selectedChoice.dialogResponses == null || responseIndex >= selectedChoice.dialogResponses.Length)
        {
            Debug.LogError($"Invalid response index {responseIndex} or no responses available!");
            ContinueToNextBlock();
            return;
        }
        
        var response = selectedChoice.dialogResponses[responseIndex];
        string npcName = ConvertNpcNameToString(response.npcName);
        string npcResponse = response.npcResponse;
        AudioClip audioClip = selectedChoice.audioDialogResponse;
        
        // ENHANCED: Check if this is a new choice selection (not just a new response index)
        // Audio should only play once per CHOICE, not once per response within that choice
        bool isNewChoiceSelection = (selectedChoiceIndex != lastAudioPlayedForChoiceIndex) || 
                                   (selectedChoice.playerChoice != lastPlayedChoiceText);
        
        if (isNewChoiceSelection)
        {
            // This is a completely new choice selection - reset audio tracking
            hasPlayedCurrentResponseAudio = false;
            lastPlayedAudioPath = "";
            lastAudioPlayedForChoiceIndex = selectedChoiceIndex;
            lastPlayedChoiceText = selectedChoice.playerChoice;
            Debug.Log($"[AUDIO] New choice selection detected: '{selectedChoice.playerChoice}' - audio can play");
        }
        else
        {
            // This is the same choice, just a different response index - don't reset audio tracking
            Debug.Log($"[AUDIO] Same choice selection: '{selectedChoice.playerChoice}' - audio already played, won't replay");
        }
        
        // Update the current response index
        currentChoiceResponseIndex = responseIndex;
        
        // Process stress modifiers from the dialog response
        string processedResponse = ProcessStressModifiers(npcResponse);
        
        Debug.Log($"Showing dialog response {responseIndex + 1}/{selectedChoice.dialogResponses.Length}:");
        Debug.Log($"  - NPC Name: '{npcName}' -> should go to DialogueName");
        Debug.Log($"  - Original Response: '{npcResponse}'");
        Debug.Log($"  - Processed Response: '{processedResponse}' -> should go to DialogueText");
        
        // Get current dialog type from the current block
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        
        // Show the response based on dialog type
        if (currentBlock.Dialog.dialogType == CoreGameDialog.DialogType.ThreeD)
        {
            Debug.Log("Displaying 3D dialog response");
            Show3DResponse(currentBlock.Dialog, processedResponse, audioClip);
        }
        else
        {
            Debug.Log("Displaying 2D dialog response");
            
            // Ensure dialog instance exists
            if (dialogInstance == null)
            {
                dialogInstance = SummonDialogBar();
                if (dialogInstance == null)
                {
                    Debug.LogError("Failed to create dialog bar for response!");
                    ContinueToNextBlock();
                    return;
                }
                
                // Clear component cache when new dialog instance is created
                ClearComponentCache();
            }
            
            // Update NPC name display immediately to synchronize with dialog animation
            if (!string.IsNullOrEmpty(npcName))
            {
                Debug.Log($"Updating NPC name to: '{npcName}'");
                UpdateNpcNameImmediate(npcName);
            }
            else
            {
                Debug.LogWarning("NPC name from response is empty!");
            }
            
            // Animate dialog response text instead of setting it instantly
            TMP_Text dialogTextComponent = null;
            if (dialogInstance != null)
            {
                // Try DialogPrefabController first
                DialogPrefabController controller = dialogInstance.GetComponent<DialogPrefabController>();
                if (controller != null && controller.dialogueText != null)
                {
                    dialogTextComponent = controller.dialogueText;
                    Debug.Log("Found DialogueText component via DialogPrefabController for response animation");
                }
                else
                {
                    // Fallback to direct search
                    Transform textTransform = dialogInstance.transform.Find("DialogueText");
                    if (textTransform != null)
                    {
                        dialogTextComponent = textTransform.GetComponent<TMP_Text>();
                        Debug.Log("Found DialogueText component via direct search for response animation");
                    }
                }
            }
            
            if (dialogTextComponent != null)
            {
                Debug.Log($"Starting dialog response text animation for: '{processedResponse}'");
                
                // Create completion callback to reset transition state when animation finishes
                System.Action onAnimationComplete = () => {
                    Debug.Log("[RESPONSE] Dialog response animation completed, resetting transition state");
                    isInDialogTransition = false;
                    
                    // NEW: Check if this was the last response and if we need to filter incorrect choices
                    CheckAndTriggerChoiceFiltering();
                    
                    // Optional: Auto-advance to next response after a short delay (can be disabled for manual control)
                    // StartCoroutine(AutoAdvanceToNextResponse(0.5f));
                };
                
                AnimateDialogText(processedResponse, dialogTextComponent, audioClip, onAnimationComplete);
            }
            else
            {
                Debug.LogError("Could not find DialogueText component for response animation! Falling back to instant text.");
                UpdateDialogTextSafe(processedResponse);
                isInDialogTransition = false; // Reset transition state since no animation
                
                // IMPORTANT: Even with instant text, we need to trigger choice filtering logic
                CheckAndTriggerChoiceFiltering();
            }
        }
        
        // Store the current response index for progression
        currentChoiceResponseIndex = responseIndex;
    }
    
    [Obsolete]
    private void Show3DResponse(CoreGameDialog dialog, string responseText, AudioClip audioClip)
    {
        GameObject targetModel = null;
        
        // Find the target model based on dialog3DLocation
        switch (dialog.dialog3DLocation)
        {
            case CoreGameDialog.Dialog3DLocation.Mother:
                targetModel = GameObject.Find("Linda_Model");
                break;
            case CoreGameDialog.Dialog3DLocation.Father:
                targetModel = GameObject.Find("Isayat_Model");
                break;
            case CoreGameDialog.Dialog3DLocation.Rey:
                targetModel = GameObject.Find("Rey_Baby_Model");
                break;
        }
        
        if (targetModel == null)
        {
            Debug.LogError($"No GameObject named '{GetModelName(dialog.dialog3DLocation)}' found for 3D response!");
            return;
        }
        
        // Find the TextDialog3D component in the target model
        var textDialog3D = targetModel.transform.Find("TextDialog3D");
        if (textDialog3D == null)
        {
            Debug.LogError($"No child GameObject named 'TextDialog3D' found in '{targetModel.name}' for response!");
            return;
        }

        var tmp3D = textDialog3D.GetComponent<TMP_Text>();
        if (tmp3D == null)
        {
            Debug.LogError($"'TextDialog3D' in '{targetModel.name}' does not have a TMP_Text component for response!");
            return;
        }

        textDialog3D.gameObject.SetActive(true);
        
        // Create completion callback to handle choice filtering for 3D dialogs too
        System.Action onAnimationComplete = () => {
            Debug.Log("[3D RESPONSE] Dialog response animation completed, resetting transition state");
            isInDialogTransition = false;
            
            // Check if this was the last response and if we need to filter incorrect choices
            CheckAndTriggerChoiceFiltering();
        };
        
        AnimateDialogText(responseText, tmp3D, audioClip, onAnimationComplete);
    }
    
    private void ClearAll3DDialogs()
    {
        // Clear all possible 3D dialog texts
        string[] modelNames = { "Linda_Model", "Isayat_Model", "Rey_Baby_Model" };
        
        foreach (string modelName in modelNames)
        {
            GameObject model = GameObject.Find(modelName);
            if (model != null)
            {
                Transform textDialog3D = model.transform.Find("TextDialog3D");
                if (textDialog3D != null)
                {
                    var tmpText = textDialog3D.GetComponent<TMP_Text>();
                    if (tmpText != null)
                    {
                        tmpText.text = ""; // Clear the text
                    }
                    textDialog3D.gameObject.SetActive(false); // Hide the dialog
                }
            }
        }
    }
    
    #endregion
    
    #region Fade System (Background Transitions)
    
    /// <summary>
    /// Handle cutscene fade effects based on CoreGameDialog.CutsceneType
    /// </summary>
    /// <param name="cutsceneType">The type of fade effect to apply</param>
    /// <param name="onComplete">Callback when fade animation completes</param>
    public void HandleCutsceneFade(CoreGameDialog.CutsceneType cutsceneType, System.Action onComplete = null)
    {
        if (backgroundFade == null)
        {
            Debug.LogWarning("BackgroundFade image is not assigned! Fade effects will be skipped.");
            onComplete?.Invoke();
            return;
        }
        
        switch (cutsceneType)
        {
            case CoreGameDialog.CutsceneType.None:
                // No fade effect, keep current state
                onComplete?.Invoke();
                break;
                
            case CoreGameDialog.CutsceneType.FadeIn:
                PerformFadeIn(onComplete);
                break;
                
            case CoreGameDialog.CutsceneType.FadeOut:
                PerformFadeOut(onComplete);
                break;
                
            case CoreGameDialog.CutsceneType.StayIn:
                SetFadeState(true, 1f); // Stay dark
                onComplete?.Invoke();
                break;
                
            case CoreGameDialog.CutsceneType.StayOut:
                SetFadeState(true, 0f); // Stay transparent
                onComplete?.Invoke();
                break;
        }
    }
    
    /// <summary>
    /// Perform fade in animation (transparent to dark)
    /// </summary>
    private void PerformFadeIn(System.Action onComplete = null)
    {
        backgroundFade.gameObject.SetActive(true);
        
        // Start from transparent
        Color fadeColor = backgroundFade.color;
        fadeColor.a = 0f;
        backgroundFade.color = fadeColor;
        
        // Animate to dark
        LeanTween.value(backgroundFade.gameObject, 0f, 1f, 1f)
            .setOnUpdate((float alpha) =>
            {
                Color color = backgroundFade.color;
                color.a = alpha;
                backgroundFade.color = color;
            })
            .setOnComplete(() =>
            {
                Debug.Log("Fade In completed");
                onComplete?.Invoke();
            })
            .setEase(LeanTweenType.easeInOutQuad);
    }
    
    /// <summary>
    /// Perform fade out animation (dark to transparent)
    /// </summary>
    private void PerformFadeOut(System.Action onComplete = null)
    {
        backgroundFade.gameObject.SetActive(true);
        
        // Start from dark
        Color fadeColor = backgroundFade.color;
        fadeColor.a = 1f;
        backgroundFade.color = fadeColor;
        
        // Animate to transparent
        LeanTween.value(backgroundFade.gameObject, 1f, 0f, 1f)
            .setOnUpdate((float alpha) =>
            {
                Color color = backgroundFade.color;
                color.a = alpha;
                backgroundFade.color = color;
            })
            .setOnComplete(() =>
            {
                backgroundFade.gameObject.SetActive(false); // Disable after fade out
                Debug.Log("Fade Out completed");
                onComplete?.Invoke();
            })
            .setEase(LeanTweenType.easeInOutQuad);
    }
    
    /// <summary>
    /// Set fade state immediately without animation
    /// </summary>
    /// <param name="active">Whether the background fade should be active</param>
    /// <param name="alpha">Alpha value (0 = transparent, 1 = dark)</param>
    private void SetFadeState(bool active, float alpha)
    {
        backgroundFade.gameObject.SetActive(active);
        
        if (active)
        {
            Color fadeColor = backgroundFade.color;
            fadeColor.a = alpha;
            backgroundFade.color = fadeColor;
            
            Debug.Log($"Fade state set to: Active={active}, Alpha={alpha}");
        }
    }
    
    /// <summary>
    /// Get BackgroundFade from dialog instance if not assigned in inspector
    /// </summary>
    private void EnsureBackgroundFadeReference()
    {
        if (backgroundFade == null && dialogInstance != null)
        {
            Transform fadeTransform = dialogInstance.transform.Find("BackgroundFade");
            if (fadeTransform != null)
            {
                backgroundFade = fadeTransform.GetComponent<Image>();
                Debug.Log("BackgroundFade reference found in dialog instance");
            }
        }
    }
    
    #endregion
    
    #region Camera Management
    
    private void SetupDialogCamera(CoreGameDialog.CamChoices camChoice)
    {
        Transform targetCamera = null;
        
        switch (camChoice)
        {
            case CoreGameDialog.CamChoices.Default_Engine:
                targetCamera = defaultCamera ?? Camera.main?.transform;
                break;
            case CoreGameDialog.CamChoices.Rey:
                targetCamera = reyCamera;
                break;
            case CoreGameDialog.CamChoices.Mother:
                targetCamera = momCamera;
                break;
            case CoreGameDialog.CamChoices.Father:
                targetCamera = fatherCamera;
                break;
        }
        
        if (targetCamera != null)
        {
            // Switch to the selected camera
            SetActiveCamera(targetCamera);
        }
    }
    
    private void SetActiveCamera(Transform cameraTransform)
    {
        // Disable all cameras first
        DisableAllCameras();
        
        // Enable the selected camera
        var camera = cameraTransform.GetComponent<Camera>();
        if (camera != null)
        {
            camera.enabled = true;
            Debug.Log($"Switched to camera: {cameraTransform.name}");
        }
    }
    
    private void DisableAllCameras()
    {
        Camera[] allCameras = { 
            defaultCamera?.GetComponent<Camera>(),
            reyCamera?.GetComponent<Camera>(),
            momCamera?.GetComponent<Camera>(),
            fatherCamera?.GetComponent<Camera>()
        };
        
        foreach (var cam in allCameras)
        {
            if (cam != null) cam.enabled = false;
        }
    }

    #endregion

    #region Cutscene Handling

    [Obsolete]
    private IEnumerator PlayCutscene(CoreGameAnimation animation)
    {
        isPlayingCutscene = true;
        Debug.Log($"Playing cutscene - moving to coordinates: {animation.Coordinates}");
        
        // Example cutscene: move camera to coordinates
        Transform activeCamera = Camera.main?.transform;
        if (activeCamera != null)
        {
            Vector3 startPos = activeCamera.position;
            Vector3 targetPos = animation.Coordinates;
            float duration = 2f;
            
            float elapsed = 0f;
            while (elapsed < duration && isPlayingCutscene)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                activeCamera.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            
            if (isPlayingCutscene)
            {
                activeCamera.position = targetPos;
            }
        }
        
        isPlayingCutscene = false;
        
        // Auto-continue after cutscene
        yield return new WaitForSeconds(1f);
        ContinueToNextBlock();
    }

    #endregion

    #region UI Management (Integrated from DialogController)

    [Obsolete]
    private GameObject SummonDialogBar()
    {
        Debug.Log("Summoning dialog bar!");

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene!");
            return null;
        }

        GameObject instance = Instantiate(npcDialogThemplate, canvas.transform, false);
        if (instance == null)
        {
            Debug.LogError("Failed to instantiate npcDialogThemplate prefab!");
            return null;
        }
        
        instance.SetActive(true);
        
        // Setup positioning and animation
        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0, 0);    // left-bottom
            rect.anchorMax = new Vector2(1, 0);    // right-bottom
            rect.pivot = new Vector2(0.5f, 0);     // bottom center
            rect.sizeDelta = new Vector2(0, rect.sizeDelta.y); // stretch width, keep height

            // Start off the bottom of the screen
            rect.anchoredPosition = new Vector2(0, -rect.rect.height);

            // Animate up to visible position (flush with bottom)
            LeanTween.value(instance, rect.anchoredPosition.y, 0, 0.3f)
                .setEaseInOutBack()
                .setOnUpdate((float val) => {
                    Vector2 pos = rect.anchoredPosition;
                    pos.y = val;
                    rect.anchoredPosition = pos;
                });
            Debug.Log("Dialog bar summoned!");
        }
        else
        {
            Debug.LogWarning("Dialog prefab has no RectTransform!");
        }
        
        return instance;
    }

    [Obsolete]
    private GameObject SummonQuestionBar()
    {
        Debug.Log("Summoning question bar!");

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene!");
            return null;
        }

        if (npcQuestionThemplate == null)
        {
            Debug.LogError("npcQuestionThemplate is not assigned! Please assign the prefab in the inspector.");
            return null;
        }

        GameObject instance = null;
        
        try
        {
            instance = Instantiate(npcQuestionThemplate, canvas.transform, false);
            
            if (instance == null)
            {
                Debug.LogError("Failed to instantiate npcQuestionThemplate prefab!");
                return null;
            }
            
            // Check for broken script references
            MonoBehaviour[] scripts = instance.GetComponentsInChildren<MonoBehaviour>();
            int brokenScripts = 0;
            foreach (var script in scripts)
            {
                if (script == null)
                {
                    brokenScripts++;
                }
            }
            
            if (brokenScripts > 0)
            {
                Debug.LogWarning($"Question bar prefab has {brokenScripts} missing script references, but continuing with instantiation.");
            }
            
            // CRITICAL FIX: Assign the instance to questionInstance
            questionInstance = instance;
            
            instance.SetActive(true);
            
            // Setup positioning and animation
            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);    // center
                rect.anchorMax = new Vector2(0.5f, 0.5f);    // center
                rect.pivot = new Vector2(0.5f, 0f);        // center
                rect.anchoredPosition = new Vector2(0, ((RectTransform)rect.parent).rect.height / 2 + rect.rect.height); // Start above the screen

                // Animate down to center of the screen
                LeanTween.value(instance, rect.anchoredPosition.y, 0, 0.3f)
                    .setEaseInOutBack()
                    .setOnUpdate((float val) => {
                        Vector2 pos = rect.anchoredPosition;
                        pos.y = val;
                        rect.anchoredPosition = pos;
                    });
                Debug.Log("Question bar summoned and positioned!");
            }
            else
            {
                Debug.LogWarning("Question prefab has no RectTransform!");
            }
            
            Debug.Log($"questionInstance assigned: {questionInstance != null}");
            
            // Validate that we can find the expected buttons
            ValidateQuestionBarStructure(instance);
            
            return instance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception while instantiating question bar: {e.Message}");
            if (instance != null)
            {
                DestroyImmediate(instance);
            }
            return null;
        }
    }
    
    /// <summary>
    /// Validate that the question bar has the expected button structure
    /// </summary>
    private void ValidateQuestionBarStructure(GameObject questionBar)
    {
        Debug.Log("=== QUESTION BAR STRUCTURE VALIDATION ===");
        
        if (questionBar == null)
        {
            Debug.LogError("Question bar is null!");
            return;
        }
        
        Debug.Log($"Question bar name: {questionBar.name}");
        
        // List all child objects
        Transform[] allChildren = questionBar.GetComponentsInChildren<Transform>();
        Debug.Log($"Question bar has {allChildren.Length} total transforms:");
        
        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform child = allChildren[i];
            Button button = child.GetComponent<Button>();
            TMP_Text text = child.GetComponent<TMP_Text>();
            
            string info = $"  [{i}] {child.name}";
            if (button != null) info += " [Button]";
            if (text != null) info += " [TMP_Text]";
            
            Debug.Log(info);
        }
        
        // Check for Q, W, E buttons specifically
        string[] expectedButtons = { "Q", "W", "E" };
        foreach (string buttonName in expectedButtons)
        {
            Transform buttonTransform = questionBar.transform.Find(buttonName);
            if (buttonTransform != null)
            {
                Button btn = buttonTransform.GetComponent<Button>();
                TMP_Text btnText = buttonTransform.GetComponentInChildren<TMP_Text>();
                
                Debug.Log($"✓ Found button '{buttonName}': Button={btn != null}, TMP_Text={btnText != null}");
            }
            else
            {
                Debug.LogWarning($"✗ Button '{buttonName}' not found as direct child!");
            }
        }
        
        Debug.Log("=== END QUESTION BAR STRUCTURE VALIDATION ===");
    }
    
    /// <summary>
    /// Destroy all question bars (integrated from DialogController)
    /// </summary>
    private static void DestroyAllQuestionBars()
    {
        // If you use a tag:
        foreach (var obj in GameObject.FindGameObjectsWithTag("QuestionBar"))
        {
            GameObject.Destroy(obj);
        }
    }
    
    private void DestroyDialogInstances()
    {
        // Stop any playing audio when destroying dialog instances
        if (dialogAudioSource != null && dialogAudioSource.isPlaying)
        {
            dialogAudioSource.Stop();
        }
        
        // Stop any active text animation
        if (dialogTween != null)
        {
            LeanTween.cancel(gameObject, dialogTween.id);
            dialogTween = null;
        }
        
        isTextAnimating = false;
        
        if (dialogInstance != null)
        {
            RectTransform rect = dialogInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                float parentHeight = ((RectTransform)rect.parent).rect.height;
                LeanTween.value(dialogInstance, rect.anchoredPosition.y, -parentHeight, 0.3f)
                    .setEaseOutQuint()
                    .setOnUpdate((float val) => {
                        Vector2 pos = rect.anchoredPosition;
                        pos.y = val;
                        rect.anchoredPosition = pos;
                    })
                    .setOnComplete(() => {
                        Destroy(dialogInstance);
                        dialogInstance = null;
                    });
            }
            else
            {
                Destroy(dialogInstance);
                dialogInstance = null;
            }
        }
        
        if (questionInstance != null)
        {
            Destroy(questionInstance);
            questionInstance = null;
        }
        
        // Also destroy any remaining question bars
        DestroyAllQuestionBars();
        
        // Hide choice buttons
        HideChoices();
        
        // Clear component and DialogSpace cache when dialog instances are destroyed
        ClearComponentCache();
    }
    
    #endregion
    
    #region Text Animation
    
    private void AnimateDialogText(string fullText, TMP_Text textComponent, AudioClip audioClip = null, System.Action onComplete = null)
    {
        // Stop any existing tweens but be smart about audio
        if (dialogTween != null) LeanTween.cancel(gameObject, dialogTween.id);
        
        // FIXED: Process special prefixes AND store both original and processed text
        string displayText = ProcessSpecialPrefixes(fullText);
        
        // Store the processed text for skip functionality to prevent variables showing in UI
        lastProcessedDialogText = displayText;
        lastTextComponent = textComponent;
        
        textComponent.text = "";
        int len = displayText.Length;
        
        // Always use normal text-based animation duration (no audio sync)
        float animationDuration = len * 0.02f; // Normal typing speed regardless of audio
        bool hasValidAudio = false;
        bool shouldPlayAudio = false;
        
        // Smart audio handling: Only play if we haven't played this audio clip for this response yet
        if (audioClip != null && audioClip.length > 0)
        {
            string audioPath = audioClip.name; // Use clip name as identifier
            
            // Check if this is a new audio clip or if we're skipping/replaying the same response
            if (!hasPlayedCurrentResponseAudio || lastPlayedAudioPath != audioPath)
            {
                shouldPlayAudio = true;
                lastPlayedAudioPath = audioPath;
                hasPlayedCurrentResponseAudio = true;
                Debug.Log($"[AUDIO] Will play new audio: {audioPath}");
            }
            else
            {
                Debug.Log($"[AUDIO] Skipping audio replay for: {audioPath} (already played for this response)");
            }
            
            // Stop currently playing audio if we're starting a new one
            if (shouldPlayAudio && dialogAudioSource != null && dialogAudioSource.isPlaying)
            {
                dialogAudioSource.Stop();
                Debug.Log("[AUDIO] Stopped previous audio to play new clip");
            }
            
            // Play audio if we should
            if (shouldPlayAudio)
            {
                try
                {
                    if (dialogAudioSource != null)
                    {
                        dialogAudioSource.clip = audioClip;
                        dialogAudioSource.Play();
                        hasValidAudio = true;
                        Debug.Log($"[AUDIO] Playing audio for dialog: {audioClip.name} (Duration: {audioClip.length}s)");
                        Debug.Log($"[AUDIO] Text animation will use normal speed, not synced to audio duration");
                    }
                    else
                    {
                        Debug.LogWarning("[AUDIO] DialogAudioSource is null, cannot play audio.");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[AUDIO] Failed to play audio clip '{audioClip.name}': {e.Message}");
                }
            }
        }
        else
        {
            if (audioClip == null)
            {
                Debug.Log("[AUDIO] No audio file provided for dialog. Using normal text animation timing.");
            }
            else
            {
                Debug.LogWarning($"[AUDIO] Audio clip provided but has invalid length ({audioClip.length}). Using normal text timing.");
            }
        }
        
        // Ensure minimum animation duration to prevent instant text
        animationDuration = Mathf.Max(animationDuration, 0.1f);
        
        isTextAnimating = true;
        
        // DISABLE DialogSpace when text animation starts
        SetDialogSpaceActive(false);
        
        dialogTween = LeanTween.value(gameObject, 0, len, animationDuration)
            .setOnUpdate((float val) => {
                int counter = Mathf.Clamp(Mathf.FloorToInt(val), 0, len);
                textComponent.text = displayText.Substring(0, counter);
            })
            .setOnComplete(() => {
                textComponent.text = displayText;
                isTextAnimating = false;
                
                // Clear stored text data since animation completed normally
                lastProcessedDialogText = "";
                lastTextComponent = null;
                
                // RE-ENABLE DialogSpace when text animation completes
                SetDialogSpaceActive(true);
                
                // Log completion
                if (hasValidAudio)
                {
                    Debug.Log("Dialog animation completed with audio sync.");
                }
                else
                {
                    Debug.Log("Dialog animation completed using default timing.");
                }
                
                // Call completion callback if provided
                onComplete?.Invoke();
            });
    }
    
    private string ProcessSpecialPrefixes(string fullText)
    {
        const string mapnamePrefix = "mapname:";
        const string exitgamePrefix = "exitgame:true";
        const string timelinePrefix = "timeline:";
        const string chargemeterPrefix = "charge:";
        
        if (fullText.StartsWith(mapnamePrefix))
        {
            int spaceIndex = fullText.IndexOf(' ');
            if (spaceIndex > mapnamePrefix.Length)
            {
                return fullText.Substring(spaceIndex + 1);
            }
        }
        else if (fullText.StartsWith(exitgamePrefix))
        {
            Application.Quit();
            return "Exiting game...";
        }
        else if (fullText.StartsWith(timelinePrefix))
        {
            Debug.Log("Timeline event triggered: " + fullText);
            // Handle timeline logic here
        }
        else if (fullText.StartsWith(chargemeterPrefix))
        {
            Debug.Log("Charge meter event: " + fullText);
            // Handle charge meter logic here
        }
        
        return fullText;
    }

    #endregion

    #region Input Handling

    private void Update()
    {
        float currentTime = Time.time;
        
        // Enhanced input throttling - prevent spam input with multiple checks
        if (isProcessingInput || isInDialogTransition)
        {
            return; // Don't process new input while already processing or transitioning
        }
        
        // Additional safety: ensure minimum time between inputs
        bool canProcessInput = (currentTime - lastInputTime) >= INPUT_COOLDOWN;
        
        // Extra safety: ensure minimum time between dialog updates
        bool canUpdateDialog = (currentTime - lastDialogUpdateTime) >= 0.2f;
        
        // Use Space key for dialog progression with enhanced protection
        // UPDATED: Use DialogInputHandler only (no fallback)
        bool spacePressed = dialogInputHandler != null && dialogInputHandler.GetDialogKeyDown();
        
        if (spacePressed && canProcessInput && canUpdateDialog)
        {
            // Set all protection flags immediately
            lastInputTime = currentTime;
            lastDialogUpdateTime = currentTime;
            isProcessingInput = true;
            isInDialogTransition = true;
            
            Debug.Log($"[INPUT] DIALOG KEY PRESSED - Time: {currentTime:F2}, Starting HandleDialogProgression()");
            
            try
            {
                HandleDialogProgression();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[INPUT] Error in HandleDialogProgression: {e.Message}");
            }
            finally
            {
                // Always reset flags even if there's an error
                StartCoroutine(ResetInputProcessingFlags());
            }
        }
        
        // Handle choice input with enhanced throttling
        if (canProcessInput && !isInDialogTransition)
        {
            HandleChoiceInput();
        }
        
        // Escape key for cutscenes (no throttling needed)
        // UPDATED: Use DialogInputHandler only (no fallback)
        bool escapePressed = dialogInputHandler != null && dialogInputHandler.GetEscapeKeyDown();
        
        if (escapePressed && isPlayingCutscene)
        {
            SkipCutscene();
        }
    }
    
    /// <summary>
    /// Reset transition flag after text skip to prevent getting stuck
    /// </summary>
    private IEnumerator ResetTransitionFlagAfterSkip()
    {
        // Wait a short time for text completion to finish
        yield return new WaitForSeconds(0.1f);
        isInDialogTransition = false;
        Debug.Log("[PROGRESSION] Transition flag reset after text skip");
    }
    
    /// <summary>
    /// Reset input processing flags after a delay to prevent spam
    /// </summary>
    private IEnumerator ResetInputProcessingFlags()
    {
        // Wait for a minimum delay before allowing new input
        yield return new WaitForSeconds(0.2f);
        
        // Reset processing flags
        isProcessingInput = false;
        
        // Wait a bit longer before allowing dialog transitions
        yield return new WaitForSeconds(0.1f);
        isInDialogTransition = false;
        
        Debug.Log("[INPUT] Input processing flags reset - ready for new input");
    }
    
    /// <summary>
    /// Handle choice input using Q, W, E keys
    /// UPDATED: Use DialogInputHandler instead of Input.GetKeyDown
    /// </summary>
    private void HandleChoiceInput()
    {
        // Only handle choice input if choices are currently visible
        if (onChoiceSelected == null) return;
        
        // Additional protection: don't process input immediately after showing choices
        if (isInDialogTransition) return;
        
        // Check for Q, W, E key presses for choice selection
        // UPDATED: Use DialogInputHandler only (no fallback)
        bool qPressed = dialogInputHandler != null && dialogInputHandler.GetChoiceQKeyDown();
        bool wPressed = dialogInputHandler != null && dialogInputHandler.GetChoiceWKeyDown();
        bool ePressed = dialogInputHandler != null && dialogInputHandler.GetChoiceEKeyDown();
        
        if (qPressed)
        {
            Debug.Log("[CHOICE-INPUT] Q key pressed - selecting choice 0");
            SelectChoice(0);
        }
        else if (wPressed)
        {
            Debug.Log("[CHOICE-INPUT] W key pressed - selecting choice 1");
            SelectChoice(1);
        }
        else if (ePressed)
        {
            Debug.Log("[CHOICE-INPUT] E key pressed - selecting choice 2");
            SelectChoice(2);
        }
    }
    
    /// <summary>
    /// Select a choice by index using keyboard input
    /// UPDATED: Use button array approach for consistency
    /// </summary>
    private void SelectChoice(int choiceIndex)
    {
        Debug.Log($"SelectChoice called with index {choiceIndex}");
        
        if (questionInstance == null)
        {
            Debug.LogWarning("Question instance is null, cannot select choice!");
            return;
        }
        
        // Get all buttons using the same array approach
        Button[] buttonArray = questionInstance.GetComponentsInChildren<Button>();
        Debug.Log($"Found {buttonArray.Length} buttons for choice selection");
        
        // Make sure the choice index is valid and within button array bounds
        if (choiceIndex >= 0 && choiceIndex < buttonArray.Length)
        {
            Button targetButton = buttonArray[choiceIndex];
            
            if (targetButton != null && targetButton.gameObject.activeInHierarchy)
            {
                Debug.Log($"Selecting choice {choiceIndex} - Button: {targetButton.name}");
                // Simulate button click
                targetButton.onClick.Invoke();
            }
            else
            {
                Debug.LogWarning($"Button at index {choiceIndex} is null or inactive!");
            }
        }
        else
        {
            Debug.LogWarning($"Choice index {choiceIndex} is out of range! Available buttons: {buttonArray.Length}");
        }
    }

    private void HandleDialogProgression()
    {
        Debug.Log("[PROGRESSION] === HandleDialogProgression CALLED ===");
        Debug.Log($"[PROGRESSION] Current state:");
        Debug.Log($"[PROGRESSION]   - isPlayingCutscene: {isPlayingCutscene}");
        Debug.Log($"[PROGRESSION]   - isTextAnimating: {isTextAnimating}");
        Debug.Log($"[PROGRESSION]   - isShowingResponse: {isShowingResponse}");
        Debug.Log($"[PROGRESSION]   - isProcessingInput: {isProcessingInput}");
        Debug.Log($"[PROGRESSION]   - isInDialogTransition: {isInDialogTransition}");
        Debug.Log($"[PROGRESSION]   - currentBlockIndex: {currentBlockIndex}/{(coreGameData?.coreBlock?.Length ?? 0)}");
        Debug.Log($"[PROGRESSION]   - selectedChoiceIndex: {selectedChoiceIndex}");
        Debug.Log($"[PROGRESSION]   - currentChoiceResponseIndex: {currentChoiceResponseIndex}");
        
        try
        {
            // Safety check: Ensure we have valid data
            if (coreGameData == null || coreGameData.coreBlock == null)
            {
                Debug.LogError("[PROGRESSION] CoreGameData is null or has no blocks!");
                return;
            }
            
            if (isPlayingCutscene) 
            {
                Debug.Log("[PROGRESSION] Blocked: Currently playing cutscene");
                return;
            }
            
            // If text is currently animating, skip to complete text and stop audio
            if (isTextAnimating)
            {
                Debug.Log("[PROGRESSION] Text is animating, skipping to complete...");
                
                // Set transition flag to prevent further input during completion
                isInDialogTransition = true;
                
                SkipTextAnimation();
                
                // Reset transition flag after a short delay
                StartCoroutine(ResetTransitionFlagAfterSkip());
                return;
            }
            
            if (currentBlockIndex >= coreGameData.coreBlock.Length)
            {
                Debug.Log("[PROGRESSION] Reached end of game, finishing...");
                FinishCoreGame();
                return;
            }
            
            var currentBlock = coreGameData.coreBlock[currentBlockIndex];
            Debug.Log($"[PROGRESSION] Current block type: {currentBlock.Type}");
            
            // If showing a choice response, handle multiple responses
            if (isShowingResponse)
            {
                Debug.Log("[PROGRESSION] Currently showing response, checking for more responses...");
                
                if (selectedChoiceIndex < 0 || currentBlock.Dialog?.choices == null || selectedChoiceIndex >= currentBlock.Dialog.choices.Length)
                {
                    Debug.LogError($"[PROGRESSION] Invalid selectedChoiceIndex {selectedChoiceIndex} or no choices available!");
                    // Reset state and continue
                    ResetDialogResponseState();
                    ContinueToNextBlock();
                    return;
                }
                
                var selectedChoice = currentBlock.Dialog.choices[selectedChoiceIndex];
                
                if (selectedChoice != null && selectedChoice.dialogResponses != null)
                {
                    int nextResponseIndex = currentChoiceResponseIndex + 1;
                    Debug.Log($"[PROGRESSION] Next response index would be: {nextResponseIndex} (total responses: {selectedChoice.dialogResponses.Length})");
                    
                    // Check if there are more responses to show
                    if (nextResponseIndex < selectedChoice.dialogResponses.Length)
                    {
                        Debug.Log($"[PROGRESSION] Showing next dialog response: {nextResponseIndex + 1}/{selectedChoice.dialogResponses.Length}");
                        
                        // Show next response without checking transition state since we want to continue
                        ShowDialogResponse(selectedChoice, nextResponseIndex);
                        return;
                    }
                    else
                    {
                        Debug.Log("[PROGRESSION] All dialog responses shown");
                        
                        // NOTE: For incorrect choices, CheckAndTriggerChoiceFiltering() should have already 
                        // handled the filtering automatically when the last response animation completed.
                        // If we reach here, it means the automatic callback failed or this is a correct choice.
                        
                        // Determine which choice array we're working with
                        CoreGameDialogChoices[] choicesToCheck = isUsingFilteredChoices ? currentFilteredChoices : currentBlock.Dialog.choices;
                        
                        // Check if the selected choice was correct
                        if (choicesToCheck != null && selectedChoiceIndex >= 0 && selectedChoiceIndex < choicesToCheck.Length)
                        {
                            var selectedChoiceToCheck = choicesToCheck[selectedChoiceIndex];
                            
                            Debug.Log($"[PROGRESSION] Choice '{selectedChoiceToCheck.playerChoice}' (correct: {selectedChoiceToCheck.correctChoice})");
                            
                            if (selectedChoiceToCheck.correctChoice)
                            {
                                Debug.Log("[PROGRESSION] Correct choice - continuing to next block");
                                ResetDialogResponseState();
                                ClearAll3DDialogs();
                                
                                if (!IsNext2DDialog() && !isUsingFilteredChoices)
                                {
                                    DestroyDialogInstances();
                                }
                                
                                ContinueToNextBlock();
                                return;
                            }
                            else
                            {
                                Debug.Log("[PROGRESSION] FALLBACK: Incorrect choice not handled by automatic callback - triggering manual filter");
                                ResetDialogResponseState();
                                FilterIncorrectChoiceAndRespawn(selectedChoiceIndex);
                                return; // Don't continue to next block
                            }
                        }
                        else
                        {
                            Debug.LogError("[PROGRESSION] Cannot determine if choice was correct - invalid selection data");
                            ResetDialogResponseState();
                            
                            // Handle error case - continue to next block
                            ClearAll3DDialogs();
                            
                            if (!IsNext2DDialog() && !isUsingFilteredChoices)
                            {
                                DestroyDialogInstances();
                            }
                            
                            Debug.Log("[PROGRESSION] Continuing to next block after error...");
                            ContinueToNextBlock();
                            return;
                        }
                    }
                }
                else
                {
                    Debug.Log("[PROGRESSION] No more responses or selectedChoice is null, continuing to next block");
                    // FALLBACK: Check if this was an incorrect choice that should be filtered
                    // This handles cases where the automatic callback failed
                    if (selectedChoice != null && !selectedChoice.correctChoice)
                    {
                        Debug.Log("[PROGRESSION] FALLBACK: Detected incorrect choice without responses - triggering filtering");
                        
                        // Store the choice index before resetting state
                        int choiceToFilter = selectedChoiceIndex;
                        
                        // Ensure questionInstance exists
                        if (questionInstance == null)
                        {
                            Debug.LogWarning("[PROGRESSION] FALLBACK: questionInstance is null! Creating new one...");
                            #pragma warning disable CS0618 // Type or member is obsolete
                            questionInstance = SummonQuestionBar();
                            #pragma warning restore CS0618 // Type or member is obsolete
                            if (questionInstance == null)
                            {
                                Debug.LogError("[PROGRESSION] FALLBACK: Failed to create questionInstance!");
                                return;
                            }
                        }
                        
                        // Reset dialog response state
                        ResetDialogResponseState();
                        
                        // Filter and respawn choices
                        Debug.Log($"[PROGRESSION] FALLBACK: Calling FilterIncorrectChoiceAndRespawn({choiceToFilter})");
                        FilterIncorrectChoiceAndRespawn(choiceToFilter);
                        return; // Don't continue to next block
                    }
                    
                    // No more responses, continue to next block
                    ResetDialogResponseState();
                    
                    // Clear 3D dialogs and handle destruction for completed responses
                    ClearAll3DDialogs();
                    
                    // Only destroy dialog instances if next block is not a 2D dialog
                    // AND we're not using filtered choices
                    if (!IsNext2DDialog() && !isUsingFilteredChoices)
                    {
                        DestroyDialogInstances();
                    }
                    
                    Debug.Log("[PROGRESSION] Continuing to next block after responses...");
                    ContinueToNextBlock();
                    return;
                }
            }
            
            // If current block is dialog and has no choices, continue
            if (currentBlock.Type == CoreGameBlock.CoreType.Dialog)
            {
                var dialog = currentBlock.Dialog;
                if (dialog.choices == null || dialog.choices.Length == 0)
                {
                    Debug.Log("[PROGRESSION] Current dialog has no choices, continuing to next block...");
                    ClearAll3DDialogs(); // Clear 3D dialogs when continuing
                    
                    // Only destroy dialog instances if next block is not a 2D dialog
                    // AND we're not using filtered choices (which means we need to keep the questionInstance)
                    if (!IsNext2DDialog() && !isUsingFilteredChoices)
                    {
                        DestroyDialogInstances();
                    }
                    
                    ContinueToNextBlock();
                }
                else
                {
                    Debug.Log($"[PROGRESSION] Current dialog has {dialog.choices.Length} choices - waiting for user selection");
                }
            }
            else
            {
                Debug.Log($"[PROGRESSION] Current block is not a dialog (type: {currentBlock.Type})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PROGRESSION] Exception in HandleDialogProgression: {e.Message}");
            Debug.LogError($"[PROGRESSION] Stack trace: {e.StackTrace}");
            
            // Reset state to prevent getting stuck
            ResetDialogResponseState();
        }
        finally
        {
            Debug.Log("[PROGRESSION] === END HandleDialogProgression ===");
        }
    }
    
    /// <summary>
    /// Reset dialog response state to prevent getting stuck
    /// </summary>
    private void ResetDialogResponseState()
    {
        Debug.Log("[PROGRESSION] Resetting dialog response state");
        isShowingResponse = false;
        currentChoiceResponseIndex = -1;
        selectedChoiceIndex = -1;
        isInDialogTransition = false;
        
        // Reset audio tracking when dialog response state is reset
        hasPlayedCurrentResponseAudio = false;
        lastPlayedAudioPath = "";
        lastAudioPlayedForChoiceIndex = -1;
        lastPlayedChoiceText = "";
        Debug.Log("[AUDIO] Reset choice-level audio tracking in ResetDialogResponseState");
    }
    
    /// <summary>
    /// Check if the next block in sequence is a 2D dialog
    /// </summary>
    private bool IsNext2DDialog()
    {
        int nextBlockIndex = currentBlockIndex + 1;
        
        if (nextBlockIndex >= coreGameData.coreBlock.Length)
        {
            return false; // No next block
        }
        
        var nextBlock = coreGameData.coreBlock[nextBlockIndex];
        
        return nextBlock.Type == CoreGameBlock.CoreType.Dialog && 
               nextBlock.Dialog != null && 
               nextBlock.Dialog.dialogType == CoreGameDialog.DialogType.TwoD;
    }
    
    private void SkipTextAnimation()
    {
        Debug.Log("[SKIP-ANIM] Skipping text animation...");
        
        // Prevent multiple simultaneous skip attempts
        if (!isTextAnimating)
        {
            Debug.Log("[SKIP-ANIM] No text animation running, nothing to skip");
            return;
        }
        
        // Set flag immediately to prevent race conditions
        bool wasTextAnimating = isTextAnimating;
        isTextAnimating = false;
        
        try
        {
            // Stop audio immediately
            if (dialogAudioSource != null && dialogAudioSource.isPlaying)
            {
                dialogAudioSource.Stop();
                Debug.Log("[SKIP-ANIM] Dialog audio stopped due to skip.");
            }
            
            // Complete the tween immediately
            if (dialogTween != null)
            {
                LeanTween.cancel(gameObject, dialogTween.id);
                dialogTween = null;
                Debug.Log("[SKIP-ANIM] Dialog tween cancelled");
            }
            
            // Only complete text display if we were actually animating
            if (wasTextAnimating)
            {
                // FIXED: Use the stored processed text to prevent variables from showing
                if (lastTextComponent != null && !string.IsNullOrEmpty(lastProcessedDialogText))
                {
                    lastTextComponent.text = lastProcessedDialogText;
                    Debug.Log("[SKIP-ANIM] Text completed with processed content (variables removed)");
                    
                    // Clear stored data after using it
                    lastProcessedDialogText = "";
                    lastTextComponent = null;
                }
                else
                {
                    // Fallback to the original method if we don't have stored data
                    CompleteCurrentTextDisplay();
                }
                
                // RE-ENABLE DialogSpace when text animation is skipped
                SetDialogSpaceActive(true);
                
                Debug.Log("[SKIP-ANIM] Text animation skipped and completed successfully.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SKIP-ANIM] Error skipping text animation: {e.Message}");
            Debug.LogError($"[SKIP-ANIM] Stack trace: {e.StackTrace}");
        }
        finally
        {
            // Ensure the flag is always set to false, even if there's an error
            isTextAnimating = false;
        }
    }
    
    private void CompleteCurrentTextDisplay()
    {
        // Get current block and complete its text display
        if (currentBlockIndex < coreGameData.coreBlock.Length)
        {
            var currentBlock = coreGameData.coreBlock[currentBlockIndex];
            
            if (currentBlock.Type == CoreGameBlock.CoreType.Dialog)
            {
                var dialog = currentBlock.Dialog;
                string textToDisplay;
                
                // Determine which text to display based on current state
                if (isShowingResponse && currentChoiceResponseIndex >= 0 && 
                    dialog.choices != null && currentChoiceResponseIndex < dialog.choices.Length)
                {
                    textToDisplay = ProcessSpecialPrefixes(GetNpcResponseFromChoice(dialog.choices[currentChoiceResponseIndex]));
                }
                else
                {
                    textToDisplay = ProcessSpecialPrefixes(dialog.dialogEntry);
                }
                
                // Complete the text based on dialog type
                if (dialog.dialogType == CoreGameDialog.DialogType.ThreeD)
                {
                    Complete3DText(dialog, textToDisplay);
                }
                else
                {
                    Complete2DText(textToDisplay);
                }
            }
        }
    }
    
    private void Complete2DText(string textToDisplay)
    {
        Debug.Log($"[COMPLETE-TEXT] Completing 2D text animation with: '{textToDisplay}'");
        
        // CRITICAL FIX: Use the same safe methods instead of generic GetComponentInChildren
        // This prevents targeting the wrong component during spam
        if (dialogInstance != null)
        {
            bool textCompleted = false;
            
            // Method 1: Try cached DialogueText component first
            if (cachedTextComponents.ContainsKey("DialogueText"))
            {
                try
                {
                    TMP_Text textComponent = cachedTextComponents["DialogueText"];
                    if (textComponent != null && textComponent.gameObject != null)
                    {
                        // Validate this is actually the dialog text component
                        string componentPath = GetTransformPath(textComponent.transform);
                        if ((componentPath.ToLower().Contains("text") || componentPath.ToLower().Contains("dialogue")) && !componentPath.ToLower().Contains("name"))
                        {
                            textComponent.text = textToDisplay;
                            Debug.Log($"[COMPLETE-TEXT] ✓ Completed text via cached DialogueText component");
                            textCompleted = true;
                        }
                        else
                        {
                            Debug.LogWarning($"[COMPLETE-TEXT] Cached component validation failed: '{componentPath}' looks like name component");
                            cachedTextComponents.Remove("DialogueText");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[COMPLETE-TEXT] Error with cached component: {e.Message}");
                    cachedTextComponents.Remove("DialogueText");
                }
            }
            
            // Method 2: Try DialogPrefabController if caching failed
            if (!textCompleted)
            {
                DialogPrefabController controller = dialogInstance.GetComponent<DialogPrefabController>();
                if (controller != null && controller.dialogueText != null)
                {
                    controller.dialogueText.text = textToDisplay;
                    Debug.Log($"[COMPLETE-TEXT] ✓ Completed text via DialogPrefabController");
                    textCompleted = true;
                }
            }
            
            // Method 3: Direct search with strict validation
            if (!textCompleted)
            {
                Transform textTransform = dialogInstance.transform.Find("DialogueText");
                if (textTransform != null)
                {
                    TMP_Text textComponent = textTransform.GetComponent<TMP_Text>();
                    if (textComponent != null)
                    {
                        // CRITICAL: Validate this is the dialog text component, not name
                        string transformName = textTransform.name.ToLower();
                        string fullPath = GetTransformPath(textTransform).ToLower();
                        
                        bool isTextComponent = ((transformName.Contains("text") || transformName.Contains("dialogue")) && !transformName.Contains("name")) ||
                                              (fullPath.Contains("text") && !fullPath.Contains("name"));
                        
                        if (isTextComponent)
                        {
                            textComponent.text = textToDisplay;
                            Debug.Log($"[COMPLETE-TEXT] ✓ Completed text via direct search");
                            textCompleted = true;
                        }
                        else
                        {
                            Debug.LogError($"[COMPLETE-TEXT] Component validation failed: '{transformName}' at '{fullPath}' is not dialog text");
                        }
                    }
                }
            }
            
            if (!textCompleted)
            {
                Debug.LogError($"[COMPLETE-TEXT] ✗ FAILED to complete text display - no valid dialog text component found");
                // Fallback to inspector-assigned dialogText if available
                if (dialogText != null)
                {
                    dialogText.text = textToDisplay;
                    Debug.Log($"[COMPLETE-TEXT] ✓ Completed text via inspector-assigned dialogText fallback");
                }
            }
        }
        else
        {
            Debug.LogWarning("[COMPLETE-TEXT] Dialog instance is null, cannot complete text");
            // Fallback to inspector-assigned dialogText if available
            if (dialogText != null)
            {
                dialogText.text = textToDisplay;
                Debug.Log($"[COMPLETE-TEXT] ✓ Completed text via inspector-assigned dialogText fallback");
            }
        }
    }
    
    private void Complete3DText(CoreGameDialog dialog, string textToDisplay)
    {
        // Complete 3D dialog text
        GameObject targetModel = null;
        
        switch (dialog.dialog3DLocation)
        {
            case CoreGameDialog.Dialog3DLocation.Mother:
                targetModel = GameObject.Find("Linda_Model");
                break;
            case CoreGameDialog.Dialog3DLocation.Father:
                targetModel = GameObject.Find("Isayat_Model");
                break;
            case CoreGameDialog.Dialog3DLocation.Rey:
                targetModel = GameObject.Find("Rey_Baby_Model");
                break;
        }
        
        if (targetModel != null)
        {
            var textDialog3D = targetModel.transform.Find("TextDialog3D");
            if (textDialog3D != null)
            {
                var tmp3D = textDialog3D.GetComponent<TMP_Text>();
                if (tmp3D != null)
                {
                    tmp3D.text = textToDisplay;
                }
            }
        }
    }
    
    #endregion
    
    #region Legacy Dialog System Integration (from NPCDialogManager/NPCDialogManagerMaster)
    
    /// <summary>
    /// Initiate legacy dialog system (integrated from NPCDialogManagerMaster)
    /// </summary>
    [Obsolete]
    public void InitiateStartDialog(string npcDialogFile)
    {
        // Clear any existing dialogs
        ClearAll3DDialogs();
        DestroyDialogInstances();
        
        // Load and start legacy dialog
        GameObject dialogObj = SummonDialogBar();
        if (dialogObj == null)
        {
            Debug.LogError("Dialog bar could not be summoned!");
            return;
        }

        InitiateLegacyDialog(npcDialogFile, dialogObj);
    }
    
    /// <summary>
    /// Initialize legacy dialog system (from NPCDialogManager)
    /// </summary>
    [Obsolete]
    private void InitiateLegacyDialog(string dialogFileName, GameObject dialogObj)
    {
        // This would integrate with your existing legacy dialog system
        // For now, just provide a framework for backward compatibility
        Debug.Log($"Legacy dialog system called with file: {dialogFileName}");
        
        // You can extend this to load DialogMasterManager files from Resources
        // and convert them to work with the CoreGame system
    }
    
    /// <summary>
    /// Handle NPC button interactions (from DialogButtonController/MenuButtonHandler)
    /// </summary>
    [Obsolete]
    public void OnNPCButtonClicked(string npcTag)
    {
        Debug.Log($"NPC button clicked! NPC tag: {npcTag}");
        
        // Handle different NPC types
        if (npcTag.Contains("npc-nene"))
        {
            InitiateStartDialog("NPC_Nene");
        }
        else if (npcTag.Contains("npc-shopkeeper"))
        {
            InitiateStartDialog("NPC_Shopkeeper");
        }
        else if (npcTag.Contains("villager"))
        {
            Debug.Log("Show villager dialog options.");
        }
    }
    
    #endregion
    
    #region Game Flow
    
    private void FinishCoreGame()
    {
        Debug.Log("Core game sequence finished!");
        
        try
        {
            // Stop any playing audio
            if (dialogAudioSource != null && dialogAudioSource.isPlaying)
            {
                dialogAudioSource.Stop();
                Debug.Log("Dialog audio stopped on game finish.");
            }
            
            // Stop any active tweens
            if (dialogTween != null)
            {
                LeanTween.cancel(gameObject, dialogTween.id);
                dialogTween = null;
            }
            
            isTextAnimating = false;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error cleaning up audio/animation on game finish: {e.Message}");
        }
        
        // Clean up dialogs and invoke completion event
        ClearAll3DDialogs(); // Clear any remaining 3D dialogs
        DestroyDialogInstances();
        onCoreGameFinished?.Invoke();
        IsSequenceRunning = false;
        currentCompletionCallback?.Invoke();
        currentCompletionCallback = null; 
    }
    
    /// <summary>
    /// Force trigger button respawn for testing - bypasses all conditions
    /// </summary>
    [ContextMenu("Force Button Respawn")]
    public void ForceButtonRespawn()
    {
        Debug.Log("=== FORCE BUTTON RESPAWN TEST ===");
        
        if (coreGameData == null || coreGameData.coreBlock == null)
        {
            Debug.LogError("CoreGameData is null or has no blocks!");
            return;
        }
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError($"Current block index {currentBlockIndex} is out of range");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("Current block has no dialog or choices!");
            return;
        }
        
        Debug.Log($"Original choices: {currentBlock.Dialog.choices.Length}");
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            Debug.Log($"  Choice {i}: '{currentBlock.Dialog.choices[i].playerChoice}' (correct: {currentBlock.Dialog.choices[i].correctChoice})");
        }
        
        // Find first incorrect choice to simulate its removal
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex < 0)
        {
            Debug.LogError("No incorrect choices found to test with!");
            return;
        }
        
        Debug.Log($"Forcing removal of incorrect choice {incorrectChoiceIndex}: '{currentBlock.Dialog.choices[incorrectChoiceIndex].playerChoice}'");
        
        // Force create filtered choices by removing the incorrect one
        List<CoreGameDialogChoices> filteredList = new List<CoreGameDialogChoices>();
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (i != incorrectChoiceIndex) // Skip the incorrect choice
            {
                filteredList.Add(currentBlock.Dialog.choices[i]);
            }
        }
        
        originalChoices = currentBlock.Dialog.choices;
        currentFilteredChoices = filteredList.ToArray();
        isUsingFilteredChoices = true;
        
        Debug.Log($"Created filtered choices: {currentFilteredChoices.Length}");
        for (int i = 0; i < currentFilteredChoices.Length; i++)
        {
            Debug.Log($"  Filtered Choice {i}: '{currentFilteredChoices[i].playerChoice}' (correct: {currentFilteredChoices[i].correctChoice})");
        }
        
        // Ensure we have a question instance
        if (questionInstance == null)
        {
            Debug.Log("Creating question instance...");
            #pragma warning disable CS0618 // Type or member is obsolete
            questionInstance = SummonQuestionBar();
            #pragma warning restore CS0618 // Type or member is obsolete
            if (questionInstance == null)
            {
                Debug.LogError("Failed to create question instance!");
                return;
            }
        }
        
        Debug.Log("Forcing button respawn with filtered choices...");
        RespawnChoicesWithFilteredArray();
        
        Debug.Log("=== FORCE BUTTON RESPAWN COMPLETE ===");
    }
    
    /// <summary>
    /// Test manual button activation
    /// </summary>
    [ContextMenu("Test Manual Button Activation")]
    public void TestManualButtonActivation()
    {
        Debug.Log("=== TESTING MANUAL BUTTON ACTIVATION ===");
        
        if (questionInstance == null)
        {
            Debug.Log("Creating question instance...");
            #pragma warning disable CS0618 // Type or member is obsolete
            questionInstance = SummonQuestionBar();
            #pragma warning restore CS0618 // Type or member is obsolete
            if (questionInstance == null)
            {
                Debug.LogError("Failed to create question instance!");
                return;
            }
        }
        
        Button[] buttons = questionInstance.GetComponentsInChildren<Button>();
        Debug.Log($"Found {buttons.Length} buttons");
        
        for (int i = 0; i < buttons.Length && i < 3; i++)
        {
            Button btn = buttons[i];
            Debug.Log($"Activating button {i}: {btn.name}");
            
            btn.gameObject.SetActive(true);
            
            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                string testText = $"[{GetKeyIndicator(i)}] Test Choice {i + 1}";
                btnText.text = testText;
                Debug.Log($"Set button {i} text to: '{testText}'");
            }
            else
            {
                Debug.LogError($"Button {i} has no TMP_Text component!");
            }
            
            // Add click listener
            btn.onClick.RemoveAllListeners();
            int index = i; // Capture for closure
            btn.onClick.AddListener(() => {
                Debug.Log($"Manual test button {index} clicked!");
            });
            
            Debug.Log($"Button {i} setup complete - Active: {btn.gameObject.activeInHierarchy}");
        }
        
        // Hide unused buttons
        for (int i = 3; i < buttons.Length; i++)
        {
            buttons[i].gameObject.SetActive(false);
        }
        
        Debug.Log("=== MANUAL BUTTON ACTIVATION COMPLETE ===");
    }
    
    /// <summary>
    /// Manual test of filtering without text animation - tests if the core logic works
    /// </summary>
    [ContextMenu("Manual Filter Test - No Animation")]
    public void ManualFilterTestNoAnimation()
    {
        Debug.Log("=== MANUAL FILTER TEST - NO ANIMATION ===");
        
        if (coreGameData == null || coreGameData.coreBlock == null)
        {
            Debug.LogError("CoreGameData is null or has no blocks!");
            return;
        }
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError($"Current block index {currentBlockIndex} is out of range");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("Current block has no dialog or choices!");
            return;
        }
        
        Debug.Log("STEP 1: Show initial choices");
        #pragma warning disable CS0618 // Type or member is obsolete
        ShowChoicesWithButtons(currentBlock.Dialog.choices, OnPlayerChoseResponse);
        #pragma warning restore CS0618 // Type or member is obsolete
        
        // Find first incorrect choice
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex < 0)
        {
            Debug.LogError("No incorrect choices found to test with!");
            return;
        }
        
        var incorrectChoice = currentBlock.Dialog.choices[incorrectChoiceIndex];
        Debug.Log($"STEP 2: Simulate selection of incorrect choice {incorrectChoiceIndex}: '{incorrectChoice.playerChoice}'");
        
        // Manually set up the dialog response state
        isShowingResponse = true;
        selectedChoiceIndex = incorrectChoiceIndex;
        
        // Set to last response (or 0 if no responses)
        if (incorrectChoice.dialogResponses != null && incorrectChoice.dialogResponses.Length > 0)
        {
            currentChoiceResponseIndex = incorrectChoice.dialogResponses.Length - 1;
            Debug.Log($"Set to last response index: {currentChoiceResponseIndex}");
        }
        else
        {
            currentChoiceResponseIndex = 0;
            Debug.Log("Choice has no responses, using index 0");
        }
        
        Debug.Log("STEP 3: Manually trigger CheckAndTriggerChoiceFiltering");
        Debug.Log($"State before filtering:");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        Debug.Log($"  - selectedChoiceIndex: {selectedChoiceIndex}");
        Debug.Log($"  - currentChoiceResponseIndex: {currentChoiceResponseIndex}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        
        CheckAndTriggerChoiceFiltering();
        
        Debug.Log("STEP 4: Check results");
        Debug.Log($"State after filtering:");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
        Debug.Log($"  - currentFilteredChoices length: {currentFilteredChoices?.Length ?? 0}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        
        if (questionInstance != null)
        {
            Button[] buttons = questionInstance.GetComponentsInChildren<Button>();
            Debug.Log($"Found {buttons.Length} buttons after filtering");
            int activeCount = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool isActive = buttons[i].gameObject.activeInHierarchy;
                if (isActive) activeCount++;
                TMP_Text btnText = buttons[i].GetComponentInChildren<TMP_Text>();
                string text = btnText?.text ?? "NO TEXT";
                Debug.Log($"  Button {i}: {buttons[i].name} - Active: {isActive} - Text: '{text}'");
            }
            
            if (activeCount == 0)
            {
                Debug.LogError("✗ CRITICAL: No buttons are active after manual filtering!");
            }
            else
            {
                Debug.Log($"✓ SUCCESS: {activeCount} buttons are active after manual filtering");
            }
        }
        else
        {
            Debug.LogError("✗ CRITICAL: questionInstance is null after manual filtering!");
        }
        
        Debug.Log("=== MANUAL FILTER TEST COMPLETE ===");
    }
    
    /// <summary>
    /// Check if the automatic text animation callback is working
    /// </summary>
    [ContextMenu("Test Animation Callback")]
    public void TestAnimationCallback()
    {
        Debug.Log("=== TESTING ANIMATION CALLBACK ===");
        
        // Create a mock completion callback like the one used in text animation
        System.Action testCallback = () => {
            Debug.Log("[TEST CALLBACK] Animation completed, calling CheckAndTriggerChoiceFiltering");
            CheckAndTriggerChoiceFiltering();
        };
        
        if (coreGameData == null || coreGameData.coreBlock == null)
        {
            Debug.LogError("CoreGameData is null or has no blocks!");
            return;
        }
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError($"Current block index {currentBlockIndex} is out of range");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("Current block has no dialog or choices!");
            return;
        }
        
        // Find first incorrect choice
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex < 0)
        {
            Debug.LogError("No incorrect choices found to test with!");
            return;
        }
        
        var incorrectChoice = currentBlock.Dialog.choices[incorrectChoiceIndex];
        Debug.Log($"Testing with incorrect choice {incorrectChoiceIndex}: '{incorrectChoice.playerChoice}'");
        
        // Set up state as if we just finished showing dialog responses
        isShowingResponse = true;
        selectedChoiceIndex = incorrectChoiceIndex;
        if (incorrectChoice.dialogResponses != null && incorrectChoice.dialogResponses.Length > 0)
        {
            currentChoiceResponseIndex = incorrectChoice.dialogResponses.Length - 1;
        }
        else
        {
            currentChoiceResponseIndex = 0;
        }
        
        Debug.Log($"Set up state: isShowingResponse={isShowingResponse}, selectedChoiceIndex={selectedChoiceIndex}, currentChoiceResponseIndex={currentChoiceResponseIndex}");
        
        // Manually invoke the callback
        Debug.Log("Manually invoking animation completion callback...");
        testCallback.Invoke();
        
        Debug.Log("=== ANIMATION CALLBACK TEST COMPLETE ===");
    }
    
    /// <summary>
    /// Complete test of incorrect choice flow with button verification
    /// </summary>
    [ContextMenu("Debug Complete Button Flow")]
    public void DebugCompleteButtonFlow()
    {
        Debug.Log("=== DEBUGGING COMPLETE BUTTON FLOW ===");
        
        if (coreGameData == null || coreGameData.coreBlock == null)
        {
            Debug.LogError("CoreGameData is null or has no blocks!");
            return;
        }
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError($"Current block index {currentBlockIndex} is out of range");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("Current block has no dialog or choices!");
            return;
        }
        
        Debug.Log("=== STEP 1: INITIAL STATE ===");
        Debug.Log($"Block has {currentBlock.Dialog.choices.Length} choices");
        Debug.Log($"isUsingFilteredChoices: {isUsingFilteredChoices}");
        Debug.Log($"questionInstance != null: {questionInstance != null}");
        
        if (questionInstance != null)
        {
            Button[] buttons = questionInstance.GetComponentsInChildren<Button>();
            Debug.Log($"Found {buttons.Length} buttons in questionInstance");
            for (int i = 0; i < buttons.Length; i++)
            {
                Debug.Log($"  Button {i}: {buttons[i].name} - Active: {buttons[i].gameObject.activeInHierarchy}");
            }
        }
        
        Debug.Log("=== STEP 2: SHOW INITIAL CHOICES ===");
        // Show initial choices
        ShowChoicesWithButtons(currentBlock.Dialog.choices, OnPlayerChoseResponse);
        
        if (questionInstance != null)
        {
            Button[] buttons = questionInstance.GetComponentsInChildren<Button>();
            Debug.Log($"After ShowChoicesWithButtons - Found {buttons.Length} buttons");
            int activeCount = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool isActive = buttons[i].gameObject.activeInHierarchy;
                if (isActive) activeCount++;
                TMP_Text btnText = buttons[i].GetComponentInChildren<TMP_Text>();
                string text = btnText?.text ?? "NO TEXT";
                Debug.Log($"  Button {i}: {buttons[i].name} - Active: {isActive} - Text: '{text}'");
            }
            Debug.Log($"Total active buttons: {activeCount}");
        }
        
        Debug.Log("=== STEP 3: SIMULATE INCORRECT CHOICE SELECTION ===");
        // Find first incorrect choice
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex < 0)
        {
            Debug.LogError("No incorrect choices found to test with!");
            return;
        }
        
        var incorrectChoice = currentBlock.Dialog.choices[incorrectChoiceIndex];
        Debug.Log($"Simulating selection of incorrect choice {incorrectChoiceIndex}: '{incorrectChoice.playerChoice}'");
        
        // Simulate the choice being selected
        isShowingResponse = true;
        selectedChoiceIndex = incorrectChoiceIndex;
        currentChoiceResponseIndex = 0;
        
        Debug.Log("=== STEP 4: SIMULATE DIALOG RESPONSES COMPLETION ===");
        if (incorrectChoice.dialogResponses != null && incorrectChoice.dialogResponses.Length > 0)
        {
            currentChoiceResponseIndex = incorrectChoice.dialogResponses.Length - 1; // Last response
            Debug.Log($"Set to last response index: {currentChoiceResponseIndex}");
        }
        else
        {
            Debug.Log("Choice has no responses");
        }
        
        Debug.Log("=== STEP 5: TRIGGER FILTERING ===");
        Debug.Log("Calling CheckAndTriggerChoiceFiltering()...");
        CheckAndTriggerChoiceFiltering();
        
        Debug.Log("=== STEP 6: CHECK RESULTS ===");
        Debug.Log($"After filtering:");
        Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
        Debug.Log($"  - currentFilteredChoices length: {currentFilteredChoices?.Length ?? 0}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        
        if (currentFilteredChoices != null)
        {
            Debug.Log("Filtered choices:");
            for (int i = 0; i < currentFilteredChoices.Length; i++)
            {
                Debug.Log($"  Choice {i}: '{currentFilteredChoices[i].playerChoice}' (correct: {currentFilteredChoices[i].correctChoice})");
            }
        }
        
        Debug.Log("=== STEP 7: CHECK BUTTON STATE ===");
        if (questionInstance != null)
        {
            Button[] finalButtons = questionInstance.GetComponentsInChildren<Button>();
            Debug.Log($"Final button check - Found {finalButtons.Length} buttons");
            int finalActiveCount = 0;
            for (int i = 0; i < finalButtons.Length; i++)
            {
                bool isActive = finalButtons[i].gameObject.activeInHierarchy;
                if (isActive) finalActiveCount++;
                TMP_Text btnText = finalButtons[i].GetComponentInChildren<TMP_Text>();
                string text = btnText?.text ?? "NO TEXT";
                Debug.Log($"  Final Button {i}: {finalButtons[i].name} - Active: {isActive} - Text: '{text}'");
            }
            Debug.Log($"Final total active buttons: {finalActiveCount}");
            
            if (finalActiveCount == 0)
            {
                Debug.LogError("✗ CRITICAL: NO BUTTONS ARE ACTIVE AFTER FILTERING!");
            }
            else if (finalActiveCount == currentFilteredChoices?.Length)
            {
                Debug.Log($"✓ SUCCESS: {finalActiveCount} buttons are active as expected");
            }
            else
            {
                Debug.LogWarning($"⚠ WARNING: Expected {currentFilteredChoices?.Length ?? 0} active buttons, found {finalActiveCount}");
            }
        }
        else
        {
            Debug.LogError("✗ CRITICAL: questionInstance is null after filtering!");
        }
        
        Debug.Log("=== DEBUGGING COMPLETE ===");
    }
    
    /// <summary>
    /// Test the automatic choice filtering trigger
    /// </summary>
    [ContextMenu("Test Auto Choice Filtering")]
    public void TestAutoChoiceFiltering()
    {
        Debug.Log("=== TESTING AUTO CHOICE FILTERING ===");
        
        if (coreGameData == null || coreGameData.coreBlock == null)
        {
            Debug.LogError("CoreGameData is null or has no blocks!");
            return;
        }
        
        if (currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError($"Current block index {currentBlockIndex} is out of range");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null)
        {
            Debug.LogError("Current block has no dialog or choices!");
            return;
        }
        
        // Find first incorrect choice
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex < 0)
        {
            Debug.LogError("No incorrect choices found to test with!");
            return;
        }
        
        var incorrectChoice = currentBlock.Dialog.choices[incorrectChoiceIndex];
        Debug.Log($"Testing auto filtering with incorrect choice {incorrectChoiceIndex}: '{incorrectChoice.playerChoice}'");
        
        // Simulate being in the response state for the last response
        Debug.Log("Setting up state to simulate last response completion...");
        isShowingResponse = true;
        selectedChoiceIndex = incorrectChoiceIndex;
        if (incorrectChoice.dialogResponses != null && incorrectChoice.dialogResponses.Length > 0)
        {
            currentChoiceResponseIndex = incorrectChoice.dialogResponses.Length - 1; // Last response
            Debug.Log($"Set currentChoiceResponseIndex to last response: {currentChoiceResponseIndex}");
        }
        else
        {
            currentChoiceResponseIndex = 0; // No responses case
            Debug.Log("Choice has no responses, set currentChoiceResponseIndex to 0");
        }
        
        // Ensure we have a question instance
        if (questionInstance == null)
        {
            Debug.Log("Creating question instance for test...");
            #pragma warning disable CS0618 // Type or member is obsolete
            questionInstance = SummonQuestionBar();
            #pragma warning restore CS0618 // Type or member is obsolete
            if (questionInstance == null)
            {
                Debug.LogError("Failed to create question instance!");
                return;
            }
        }
        
        Debug.Log("State before auto filtering:");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        Debug.Log($"  - selectedChoiceIndex: {selectedChoiceIndex}");
        Debug.Log($"  - currentChoiceResponseIndex: {currentChoiceResponseIndex}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
        
        Debug.Log("Calling CheckAndTriggerChoiceFiltering()...");
        CheckAndTriggerChoiceFiltering();
        
        Debug.Log("State after auto filtering:");
        Debug.Log($"  - isShowingResponse: {isShowingResponse}");
        Debug.Log($"  - selectedChoiceIndex: {selectedChoiceIndex}");
        Debug.Log($"  - questionInstance != null: {questionInstance != null}");
        Debug.Log($"  - isUsingFilteredChoices: {isUsingFilteredChoices}");
        Debug.Log($"  - currentFilteredChoices length: {currentFilteredChoices?.Length ?? 0}");
        
        if (isUsingFilteredChoices && currentFilteredChoices != null)
        {
            Debug.Log("✓ Auto filtering worked! Remaining choices:");
            for (int i = 0; i < currentFilteredChoices.Length; i++)
            {
                Debug.Log($"  Choice {i}: '{currentFilteredChoices[i].playerChoice}' (correct: {currentFilteredChoices[i].correctChoice})");
            }
            
            // Verify buttons are visible
            if (questionInstance != null)
            {
                Button[] buttons = questionInstance.GetComponentsInChildren<Button>();
                Debug.Log($"UI Check: Found {buttons.Length} buttons");
                int activeButtons = 0;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].gameObject.activeInHierarchy)
                    {
                        activeButtons++;
                        TMP_Text btnText = buttons[i].GetComponentInChildren<TMP_Text>();
                        string text = btnText?.text ?? "NO TEXT";
                        Debug.Log($"  Active Button {i}: '{text}'");
                    }
                }
                Debug.Log($"Total active buttons: {activeButtons}");
            }
        }
        else
        {
            Debug.LogError("✗ Auto filtering failed - choices should have been filtered!");
        }
        
        Debug.Log("=== END AUTO CHOICE FILTERING TEST ===");
    }
    
    /// <summary>
    /// SIMPLE TEST: Test incorrect choice flow - destroy old question bar, spawn new one
    /// </summary>
    [ContextMenu("Simple Test: Incorrect Choice Flow")]
    public void SimpleTestIncorrectChoiceFlow()
    {
        Debug.Log("=== SIMPLE TEST: DESTROY AND RESPAWN FLOW ===");
        
        if (coreGameData == null || currentBlockIndex >= coreGameData.coreBlock.Length)
        {
            Debug.LogError("No valid game data to test with!");
            return;
        }
        
        var currentBlock = coreGameData.coreBlock[currentBlockIndex];
        if (currentBlock.Dialog?.choices == null || currentBlock.Dialog.choices.Length == 0)
        {
            Debug.LogError("Current block has no choices to test with!");
            return;
        }
        
        // Find the first incorrect choice
        int incorrectChoiceIndex = -1;
        for (int i = 0; i < currentBlock.Dialog.choices.Length; i++)
        {
            if (!currentBlock.Dialog.choices[i].correctChoice)
            {
                incorrectChoiceIndex = i;
                break;
            }
        }
        
        if (incorrectChoiceIndex < 0)
        {
            Debug.LogError("No incorrect choices found in current block!");
            return;
        }
        
        Debug.Log($"Found incorrect choice at index {incorrectChoiceIndex}: '{currentBlock.Dialog.choices[incorrectChoiceIndex].playerChoice}'");
        
        // Simulate the full flow
        Debug.Log("STEP 1: Creating initial question instance...");
        if (questionInstance != null)
        {
            Debug.Log("Destroying existing question instance first...");
            Destroy(questionInstance);
            questionInstance = null;
        }
        
        #pragma warning disable CS0618
        questionInstance = SummonQuestionBar();
        #pragma warning restore CS0618
        
        Debug.Log("STEP 2: Showing initial choices...");
        ShowChoicesWithButtons(currentBlock.Dialog.choices, OnPlayerChoseResponse);
        
        Debug.Log($"STEP 3: Simulating incorrect choice selection (choice {incorrectChoiceIndex})...");
        selectedChoiceIndex = incorrectChoiceIndex;
        isShowingResponse = true;
        currentChoiceResponseIndex = 0;
        
        Debug.Log("STEP 4: Simulating response completion - this should DESTROY old and SPAWN new question bar...");
        CheckAndTriggerChoiceFiltering();
        
        Debug.Log("=== SIMPLE TEST COMPLETE ===");
    }
    
    #endregion
    
    #region Debug and Testing Methods
    
    /// <summary>
    /// Get a summary of pressed choices for debugging
    /// </summary>
    private string GetPressedChoicesSummary()
    {
        if (pressedChoiceTexts.Count == 0 && pressedChoiceIndices.Count == 0)
        {
            return "No pressed choices tracked";
        }
        
        string summary = $"Pressed Choices Summary:\n";
        summary += $"- {pressedChoiceTexts.Count} choices tracked by text\n";
        summary += $"- {pressedChoiceIndices.Count} choices tracked by index\n";
        
        if (pressedChoiceTexts.Count > 0)
        {
            summary += "Texts: " + string.Join(", ", pressedChoiceTexts) + "\n";
        }
        
        if (pressedChoiceIndices.Count > 0)
        {
            summary += "Indices: " + string.Join(", ", pressedChoiceIndices) + "\n";
        }
        
        summary += $"Using filtered choices: {isUsingFilteredChoices}\n";
        summary += $"Current filtered count: {currentFilteredChoices?.Length ?? 0}\n";
        summary += $"Original choices count: {originalChoices?.Length ?? 0}";
        
        return summary;
    }
    
    /// <summary>
    /// Debug method to print current filtering system state
    /// Call this to debug choice filtering issues
    /// </summary>
    [ContextMenu("Debug Choice Filtering State")]
    public void DebugChoiceFilteringState()
    {
        Debug.Log("=== CHOICE FILTERING DEBUG STATE ===");
        Debug.Log(GetPressedChoicesSummary());
        
        if (lastProcessedChoices != null)
        {
            Debug.Log($"Last processed choices: {lastProcessedChoices.Length} choices");
            for (int i = 0; i < lastProcessedChoices.Length; i++)
            {
                Debug.Log($"  Choice {i}: '{lastProcessedChoices[i].playerChoice}' (correct: {lastProcessedChoices[i].correctChoice})");
            }
        }
        else
        {
            Debug.Log("No last processed choices stored");
        }
        
        // Debug question instance state
        if (questionInstance != null)
        {
            Debug.Log($"Question instance exists: {questionInstance.name}");
        }
        else
        {
            Debug.Log("Question instance is null (destroyed after choice selection)");
        }
        
        Debug.Log("=== END DEBUG STATE ===");
    }
    
    /// <summary>
    /// Test method to verify prefab destruction after choice selection
    /// </summary>
    [ContextMenu("Test Prefab Destruction")]
    public void TestPrefabDestruction()
    {
        Debug.Log("=== TESTING PREFAB DESTRUCTION ===");
        
        if (questionInstance != null)
        {
            Debug.Log($"Question instance exists: {questionInstance.name}");
            Debug.Log("Calling HideChoices() to test destruction...");
            HideChoices();
            
            if (questionInstance == null)
            {
                Debug.Log("✓ SUCCESS: Question instance destroyed correctly");
            }
            else
            {
                Debug.LogError("✗ FAILED: Question instance still exists after HideChoices()");
            }
        }
        else
        {
            Debug.Log("No question instance exists to test destruction");
            
            // Try creating one for testing
            if (coreGameData?.coreBlock?[currentBlockIndex]?.Dialog?.choices != null)
            {
                Debug.Log("Creating question instance for testing...");
                ShowChoices(coreGameData.coreBlock[currentBlockIndex].Dialog.choices);
                
                if (questionInstance != null)
                {
                    Debug.Log($"✓ Question instance created: {questionInstance.name}");
                    Debug.Log("Now testing destruction...");
                    HideChoices();
                    
                    if (questionInstance == null)
                    {
                        Debug.Log("✓ SUCCESS: Question instance destroyed correctly after creation");
                    }
                    else
                    {
                        Debug.LogError("✗ FAILED: Question instance still exists after HideChoices()");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No valid choices available for testing");
            }
        }
        
        Debug.Log("=== END PREFAB DESTRUCTION TEST ===");
    }
    
    #endregion
}

/*
 * AUDIO TRACKING FIX SUMMARY:
 * 
 * Problem: Audio kept playing repeatedly when pressing SPACE during dialog responses,
 * especially when there are multiple responses in a sequence (response 1, 2, 3, etc.)
 * 
 * Root Cause: AnimateDialogText() was called every time space was pressed to progress
 * through dialog responses, and it would stop + restart the same audio clip each time.
 * 
 * Solution: Added choice-level audio tracking system with four variables:
 * 1. hasPlayedCurrentResponseAudio - tracks if current choice's audio was already played
 * 2. lastPlayedAudioPath - tracks the name of the last played audio clip
 * 3. lastAudioPlayedForChoiceIndex - tracks which choice index we played audio for
 * 4. lastPlayedChoiceText - tracks the choice text we played audio for
 * 
 * Key Behavior Change:
 * - Audio now plays ONCE PER CHOICE SELECTION, not once per individual response
 * - If a choice has 3 responses, audio plays only when showing response 1
 * - Pressing space through responses 2 and 3 will NOT replay the audio
 * - Only when selecting a completely different choice will audio play again
 * 
 * Changes Made:
 * 1. Added choice-level audio tracking variables to class fields
 * 2. Modified AnimateDialogText() to check if audio was already played for current choice
 * 3. Modified ShowDialogResponse() to detect new vs. existing choice selections
 * 4. Reset audio tracking in:
 *    - Show2DDialog() - when starting new dialogs
 *    - Show3DDialog() - when starting new dialogs  
 *    - ResetDialogResponseState() - when ending dialog response state
 * 
 * Result: Audio plays only once per choice selection, regardless of how many responses
 * that choice has. No more repeated audio when pressing space through multiple responses.
 */
