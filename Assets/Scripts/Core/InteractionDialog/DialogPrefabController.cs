using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple controller script for dialog prefabs to replace missing script references
/// Attach this to your dialog and question prefabs to fix the "missing script" errors
/// </summary>
public class DialogPrefabController : MonoBehaviour
{
    [Header("Dialog Components")]
    public TMP_Text dialogueName;
    public TMP_Text dialogueText;
    public Image backgroundFade;
    
    [Header("Choice Buttons")]
    public Button buttonQ;
    public Button buttonW;
    public Button buttonE;
    
    [Header("Auto-Find Components")]
    [SerializeField] private bool autoFindComponents = true;
    
    private void Awake()
    {
        if (autoFindComponents)
        {
            FindComponents();
        }
        
        InitializeComponents();
    }
    
    /// <summary>
    /// Automatically find components by name
    /// </summary>
    private void FindComponents()
    {
        // Find dialog text components
        if (dialogueName == null)
        {
            Transform nameTransform = transform.Find("DialogueName");
            if (nameTransform != null)
            {
                dialogueName = nameTransform.GetComponent<TMP_Text>();
                Debug.Log($"Auto-found DialogueName component on {gameObject.name}");
            }
        }
        
        if (dialogueText == null)
        {
            Transform textTransform = transform.Find("DialogueText");
            if (textTransform != null)
            {
                dialogueText = textTransform.GetComponent<TMP_Text>();
                Debug.Log($"Auto-found DialogueText component on {gameObject.name}");
            }
        }
        
        // Find background fade
        if (backgroundFade == null)
        {
            Transform fadeTransform = transform.Find("BackgroundFade");
            if (fadeTransform != null)
            {
                backgroundFade = fadeTransform.GetComponent<Image>();
                Debug.Log($"Auto-found BackgroundFade component on {gameObject.name}");
            }
        }
        
        // Find choice buttons
        if (buttonQ == null)
        {
            Transform qTransform = transform.Find("Q");
            if (qTransform != null)
            {
                buttonQ = qTransform.GetComponent<Button>();
                Debug.Log($"Auto-found Q button on {gameObject.name}");
            }
        }
        
        if (buttonW == null)
        {
            Transform wTransform = transform.Find("W");
            if (wTransform != null)
            {
                buttonW = wTransform.GetComponent<Button>();
                Debug.Log($"Auto-found W button on {gameObject.name}");
            }
        }
        
        if (buttonE == null)
        {
            Transform eTransform = transform.Find("E");
            if (eTransform != null)
            {
                buttonE = eTransform.GetComponent<Button>();
                Debug.Log($"Auto-found E button on {gameObject.name}");
            }
        }
    }
    
    /// <summary>
    /// Initialize components with default values
    /// </summary>
    private void InitializeComponents()
    {
        // Set default dialog text if components exist (but leave NPC name blank)
        if (dialogueName != null && string.IsNullOrEmpty(dialogueName.text))
        {
            dialogueName.text = ""; // Start with empty name - will be set by dialog system
        }
        
        if (dialogueText != null && string.IsNullOrEmpty(dialogueText.text))
        {
            dialogueText.text = "Dialog text will appear here...";
        }
        
        // Initialize buttons
        InitializeButton(buttonQ, "Q", "[Q] Choice 1");
        InitializeButton(buttonW, "W", "[W] Choice 2");
        InitializeButton(buttonE, "E", "[E] Choice 3");
        
        // Initialize background fade
        if (backgroundFade != null)
        {
            backgroundFade.gameObject.SetActive(false);
            Color fadeColor = backgroundFade.color;
            fadeColor.a = 0f;
            backgroundFade.color = fadeColor;
        }
    }
    
    /// <summary>
    /// Initialize a button with default settings
    /// </summary>
    private void InitializeButton(Button button, string buttonName, string defaultText)
    {
        if (button != null)
        {
            // Make sure button is initially hidden
            button.gameObject.SetActive(false);
            
            // Set default text if button has text component
            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null && string.IsNullOrEmpty(buttonText.text))
            {
                buttonText.text = defaultText;
            }
            
            Debug.Log($"Initialized {buttonName} button on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Public method to set dialog name
    /// </summary>
    public void SetDialogName(string name)
    {
        if (dialogueName != null)
        {
            dialogueName.text = name;
        }
    }
    
    /// <summary>
    /// Public method to set dialog text
    /// </summary>
    public void SetDialogText(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }
    
    /// <summary>
    /// Public method to set button text
    /// </summary>
    public void SetButtonText(string buttonName, string text)
    {
        Button targetButton = GetButtonByName(buttonName);
        if (targetButton != null)
        {
            TMP_Text buttonText = targetButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }
    }
    
    /// <summary>
    /// Get button by name
    /// </summary>
    public Button GetButtonByName(string buttonName)
    {
        switch (buttonName.ToUpper())
        {
            case "Q": return buttonQ;
            case "W": return buttonW;
            case "E": return buttonE;
            default: return null;
        }
    }
    
    /// <summary>
    /// Show or hide a button
    /// </summary>
    public void SetButtonActive(string buttonName, bool active)
    {
        Button targetButton = GetButtonByName(buttonName);
        if (targetButton != null)
        {
            targetButton.gameObject.SetActive(active);
        }
    }
    
    /// <summary>
    /// Set background fade alpha
    /// </summary>
    public void SetFadeAlpha(float alpha)
    {
        if (backgroundFade != null)
        {
            Color fadeColor = backgroundFade.color;
            fadeColor.a = Mathf.Clamp01(alpha);
            backgroundFade.color = fadeColor;
            backgroundFade.gameObject.SetActive(alpha > 0);
        }
    }
    
    /// <summary>
    /// Validate all components and report status
    /// </summary>
    [ContextMenu("Validate Components")]
    public void ValidateComponents()
    {
        Debug.Log($"=== Validating {gameObject.name} Components ===");
        
        Debug.Log($"DialogueName: {(dialogueName != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"DialogueText: {(dialogueText != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"BackgroundFade: {(backgroundFade != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"Button Q: {(buttonQ != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"Button W: {(buttonW != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"Button E: {(buttonE != null ? "✓ Found" : "✗ Missing")}");
        
        // List all child objects
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        Debug.Log($"All child objects ({allChildren.Length}):");
        for (int i = 0; i < allChildren.Length; i++)
        {
            if (allChildren[i] != transform) // Skip self
            {
                string components = "";
                if (allChildren[i].GetComponent<TMP_Text>()) components += "[TMP_Text]";
                if (allChildren[i].GetComponent<Button>()) components += "[Button]";
                if (allChildren[i].GetComponent<Image>()) components += "[Image]";
                
                Debug.Log($"  - {allChildren[i].name} {components}");
            }
        }
        
        Debug.Log($"=== End Validation ===");
    }
}
