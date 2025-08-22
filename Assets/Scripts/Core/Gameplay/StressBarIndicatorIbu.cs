using UnityEngine;
using UnityEngine.UI;

public class StressBarIndicatorIbu : MonoBehaviour
{
    [Header("UI References")]
    private Image stressBarFillImage;
    private Image indicatorStressImage; // Fill image inside IndicatorStress GameObject
    
    [Header("Save Data")]
    [SerializeField] private CoreGameSaves saveData;
    [SerializeField] private string saveDataPath = "Saves/coregamesaves"; // Path in Resources folder
    
    [Header("Stress Settings")]
    [SerializeField] private float maxStressLevel = 1000f; // Updated to 1000 scale
    [SerializeField] private bool enableAutoStressIncrease = false;
    [SerializeField] private float stressRate;
    
    [Header("Base Stress Colors (Day 1-12 - Brighter)")]
    [SerializeField] private Color lowStressColor = new Color(0f, 1f, 0f, 1f);         // 0-5 stress (Green)
    [SerializeField] private Color mediumLowStressColor = new Color(1f, 1f, 0f, 1f);   // 5-250 stress (Yellow)
    [SerializeField] private Color mediumStressColor = new Color(1f, 0.5f, 0f, 1f);    // 250-500 stress (Orange)
    [SerializeField] private Color mediumHighStressColor = new Color(0.8f, 0f, 0f, 1f); // Not used in new logic
    [SerializeField] private Color highStressColor = new Color(1f, 0f, 0f, 1f);        // 500-1000 stress (Red)
    [SerializeField] private Color maxStressColor = new Color(0.4f, 0f, 0f, 1f);       // 1000+ stress (Dark Red)
    
    [Header("Dark Stress Colors (Day 13+ - Darker)")]
    [SerializeField] private Color darkLowStressColor = new Color(0f, 0.7f, 0f, 1f);         // 0-5 stress (Dark Green)
    [SerializeField] private Color darkMediumLowStressColor = new Color(0.8f, 0.8f, 0f, 1f); // 5-250 stress (Dark Yellow)
    [SerializeField] private Color darkMediumStressColor = new Color(0.8f, 0.4f, 0f, 1f);    // 250-500 stress (Dark Orange)
    [SerializeField] private Color darkMediumHighStressColor = new Color(0.5f, 0f, 0f, 1f);  // Not used in new logic
    [SerializeField] private Color darkHighStressColor = new Color(0.7f, 0f, 0f, 1f);        // 500-1000 stress (Dark Red)
    [SerializeField] private Color darkMaxStressColor = new Color(0.3f, 0f, 0f, 1f);         // 1000+ stress (Very Dark Red)
    
    [Header("Day-based Settings")]
    [SerializeField] private int darkDayThreshold = 13; // Day 13+ uses darker colors
    
    [Header("Outline Colors")]
    [SerializeField] private Color lowStressOutline = new Color(0f, 1f, 0f, 0.3f);     // 0-5: Green outline
    [SerializeField] private Color mediumStressOutline = new Color(1f, 1f, 0f, 0.5f);  // 5-250: Yellow outline
    [SerializeField] private Color highStressOutline = new Color(1f, 0.5f, 0f, 0.7f);  // 250-500: Orange outline
    [SerializeField] private Color maxStressOutline = new Color(1f, 0f, 0f, 0.9f);     // 500+: Red outline
    
    [Header("Outline Settings")]
    [SerializeField] private float outlineThickness = 2f;
    
    // Component references
    private Outline outlineComponent;
    
    // Animation tracking
    private float currentFillAmount = 0f;
    private float targetFillAmount = 0f;
    private int previousStressLevel = 0;
    private bool isAnimating = false;
    private LTDescr fillAnimationTween;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private LeanTweenType animationEase = LeanTweenType.easeOutQuad;
    [SerializeField] private bool enableStressChangeAnimation = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false; // Debug logs removed

    void Start()
    {
        InitializeComponents();
        LoadSaveData();
    }
    
    /// <summary>
    /// Initialize UI components and add Outline component if needed
    /// </summary>
    private void InitializeComponents()
    {
        // Find the stress bar fill image in Background GameObject (for outline)
        stressBarFillImage = GameObject.Find("Background").GetComponent<Image>();
        if (stressBarFillImage == null)
        {
            // Background GameObject not found in the scene or doesn't have an Image component
        }
        else
        {
            // Add or get Outline component for day-based visual effects
            outlineComponent = stressBarFillImage.GetComponent<Outline>();
            if (outlineComponent == null)
            {
                outlineComponent = stressBarFillImage.gameObject.AddComponent<Outline>();
            }
            
            // Initialize outline settings
            outlineComponent.effectDistance = new Vector2(outlineThickness, outlineThickness);
            outlineComponent.useGraphicAlpha = true;
        }
        
        // Find the indicator stress image (for fill color and amount)
        GameObject indicatorStressGO = GameObject.Find("IndicatorStress");
        if (indicatorStressGO != null)
        {
            indicatorStressImage = indicatorStressGO.GetComponent<Image>();
            if (indicatorStressImage == null)
            {
                // IndicatorStress GameObject found but doesn't have an Image component
            }
            else
            {
                // Initialize current fill amount
                currentFillAmount = indicatorStressImage.fillAmount;
            }
        }
        else
        {
            // IndicatorStress GameObject not found in the scene
        }
    }
    
    /// <summary>
    /// Load save data from Resources or assigned ScriptableObject
    /// </summary>
    private void LoadSaveData()
    {
        // If no save data is assigned, try to load from Resources
        if (saveData == null)
        {
            saveData = Resources.Load<CoreGameSaves>(saveDataPath);
            if (saveData == null)
            {
                // CoreGameSaves not found at Resources path
                // Please assign CoreGameSaves ScriptableObject in inspector or place it in Resources folder
                return;
            }
            else
            {
                // Store the initial stress level for animation tracking
                if (saveData != null)
                {
                    previousStressLevel = saveData.mother_stress_level;
                    targetFillAmount = Mathf.Clamp01(saveData.mother_stress_level / 1000f);
                    currentFillAmount = targetFillAmount;
                }
            }
        }
        
        // Initial update without animation
        UpdateStressBarImmediate();
        UpdateDayAndStressBasedColors();
    }

    void OnDestroy()
    {
        // Clean up any running animations
        if (fillAnimationTween != null)
        {
            LeanTween.cancel(fillAnimationTween.id);
            fillAnimationTween = null;
        }
    }

    void Update()
    {
        if (saveData == null || indicatorStressImage == null)
            return;
            
        // Optional: Auto-increase stress for testing
        if (enableAutoStressIncrease)
        {
            saveData.mother_stress_level += (int)(stressRate * Time.deltaTime);
        }
        
        // Check if stress level has changed
        if (saveData.mother_stress_level != previousStressLevel)
        {
            // Stress has changed - trigger animation
            AnimateStressChange(saveData.mother_stress_level);
            previousStressLevel = saveData.mother_stress_level;
        }
        
        // Always update colors (in case day changed)
        UpdateDayAndStressBasedColors();
    }
    
    /// <summary>
    /// Animate stress change from current level to new level
    /// </summary>
    /// <param name="newStressLevel">The new stress level to animate to</param>
    private void AnimateStressChange(int newStressLevel)
    {
        if (!enableStressChangeAnimation)
        {
            UpdateStressBarImmediate();
            return;
        }
        
        // Calculate new target fill amount
        float newTargetFill = Mathf.Clamp01(newStressLevel / 1000f);
        
        // If already animating, cancel the previous animation
        if (fillAnimationTween != null)
        {
            LeanTween.cancel(fillAnimationTween.id);
        }
        
        // Start from current fill amount and animate to new target
        targetFillAmount = newTargetFill;
        isAnimating = true;
        
        // Animate the fill amount
        fillAnimationTween = LeanTween.value(gameObject, currentFillAmount, targetFillAmount, animationDuration)
            .setEase(animationEase)
            .setOnUpdate((float value) => {
                currentFillAmount = value;
                UpdateFillDisplay(value);
            })
            .setOnComplete(() => {
                isAnimating = false;
                currentFillAmount = targetFillAmount;
                fillAnimationTween = null;
            });
    }
    
    /// <summary>
    /// Update the visual fill display with the given fill amount
    /// </summary>
    /// <param name="fillAmount">Fill amount between 0 and 1</param>
    private void UpdateFillDisplay(float fillAmount)
    {
        // Update IndicatorStress fill amount (primary stress display)
        if (indicatorStressImage != null)
        {
            indicatorStressImage.fillAmount = fillAmount;
        }
        
        // Also update Background if it exists (backup/secondary display)
        if (stressBarFillImage != null)
        {
            stressBarFillImage.fillAmount = fillAmount;
        }
    }
    
    /// <summary>
    /// Update the stress bar fill amount immediately without animation
    /// </summary>
    private void UpdateStressBarImmediate()
    {
        if (saveData == null)
            return;
            
        // Calculate fill amount - 100 stress = 0.1 fill, 1000 stress = 1.0 fill
        float fillAmount = Mathf.Clamp01(saveData.mother_stress_level / 1000f);
        
        currentFillAmount = fillAmount;
        targetFillAmount = fillAmount;
        
        UpdateFillDisplay(fillAmount);
    }
    
    /// <summary>
    /// Update the stress bar fill amount based on save data (with animation if enabled)
    /// </summary>
    private void UpdateStressBar()
    {
        if (saveData == null)
            return;
            
        // Check if stress level changed
        if (saveData.mother_stress_level != previousStressLevel)
        {
            AnimateStressChange(saveData.mother_stress_level);
            previousStressLevel = saveData.mother_stress_level;
        }
    }
    
    /// <summary>
    /// Update both fill color and outline color based on current day and stress level
    /// Day 13+ uses darker color palette regardless of stress level
    /// Stress level affects color progression within the day's palette and fill amount (0.0-1.0)
    /// </summary>
    private void UpdateDayAndStressBasedColors()
    {
        if (saveData == null)
            return;
            
        int currentDay = saveData.day;
        int currentStress = saveData.mother_stress_level;
        bool useDarkPalette = currentDay >= darkDayThreshold;
        
        // Update fill color based on day and stress
        UpdateDayAndStressBasedFillColor(currentStress, useDarkPalette);
        
        // Update outline color based on stress
        UpdateStressBasedOutlineColor(currentStress);
    }
    
    /// <summary>
    /// Update fill color based on day and stress level
    /// Day 13+ uses darker colors, stress affects progression within that palette
    /// </summary>
    private void UpdateDayAndStressBasedFillColor(int stressLevel, bool useDarkPalette)
    {
        if (indicatorStressImage == null)
            return;
            
        Color fillColor;
        
        // Select color palette based on day
        Color lowColor = useDarkPalette ? darkLowStressColor : lowStressColor;
        Color mediumLowColor = useDarkPalette ? darkMediumLowStressColor : mediumLowStressColor;
        Color mediumColor = useDarkPalette ? darkMediumStressColor : mediumStressColor;
        Color mediumHighColor = useDarkPalette ? darkMediumHighStressColor : mediumHighStressColor;
        Color highColor = useDarkPalette ? darkHighStressColor : highStressColor;
        Color maxColor = useDarkPalette ? darkMaxStressColor : maxStressColor;
        
        // Determine color based on stress level with new thresholds
        // <= 5: Green, <= 50: Yellow, <= 250: Yellow-Orange, <= 500: Orange, <= 1000: Red, 1000+: Dark Red
        if (stressLevel <= 4)
        {
            // 0-5: Green
            fillColor = lowColor;
        }
        else if (stressLevel <= 5)
        {
            // 5-50: Pure Yellow
            fillColor = mediumLowColor;
            
            // FORCE TEST: Set to bright red to see if this actually works
            if (stressLevel == 50) {
                fillColor = Color.red;
            }
        }
        else if (stressLevel <= 6)
        {
            // 50-250: Yellow to Orange transition
            float t = (stressLevel - 50f) / 200f;
            fillColor = Color.Lerp(mediumLowColor, mediumStressColor, t);
        }
        else if (stressLevel <= 500)
        {
            // 250-500: Orange
            fillColor = mediumStressColor;
        }
        else if (stressLevel <= 1000)
        {
            // 500-1000: Red (interpolate from orange to red)
            float t = (stressLevel - 500f) / 500f;
            fillColor = Color.Lerp(mediumStressColor, highColor, t);
        }
        else
        {
            // 1000+: Dark Red
            fillColor = maxColor;
        }
        
        // Apply fill color
        indicatorStressImage.color = fillColor;
    }
    
    /// <summary>
    /// Update outline color based on stress level
    /// </summary>
    private void UpdateStressBasedOutlineColor(int stressLevel)
    {
        if (outlineComponent == null)
            return;
            
        Color outlineColor;
        
        if (stressLevel <= 5)
        {
            // 0-5: Green outline
            outlineColor = lowStressOutline;
        }
        else if (stressLevel <= 500)
        {
            // 5-50: Yellow outline
            outlineColor = mediumStressOutline;
        }
        else if (stressLevel <= 750)
        {
            // 50-500: Orange outline
            outlineColor = highStressOutline;
        }
        else
        {
            // 500+: Red outline
            outlineColor = maxStressOutline;
        }
        
        // Apply outline color
        outlineComponent.effectColor = outlineColor;
        outlineComponent.enabled = true;
    }
    
    /// <summary>
    /// Manually refresh the stress bar and colors (useful after save data changes)
    /// </summary>
    [ContextMenu("Refresh Stress Bar")]
    public void RefreshStressBar()
    {
        if (saveData != null)
        {
            UpdateStressBarImmediate();
            UpdateDayAndStressBasedColors();
        }
    }
    
    /// <summary>
    /// Set stress level directly (for testing or external systems)
    /// Allows stress levels above 1000 for testing purposes
    /// </summary>
    public void SetStressLevel(int newStressLevel)
    {
        if (saveData != null)
        {
            int oldStress = saveData.mother_stress_level;
            saveData.mother_stress_level = Mathf.Max(0, newStressLevel); // Only ensure it's not negative
            
            // Trigger animation if stress changed
            if (oldStress != saveData.mother_stress_level)
            {
                AnimateStressChange(saveData.mother_stress_level);
                previousStressLevel = saveData.mother_stress_level;
            }
            
            UpdateDayAndStressBasedColors();
        }
    }
    
    /// <summary>
    /// Set day directly (for testing or external systems)
    /// </summary>
    public void SetDay(int newDay)
    {
        if (saveData != null)
        {
            saveData.day = Mathf.Max(1, newDay);
            UpdateDayAndStressBasedColors(); // Colors are now day AND stress-based
        }
    }
    
    /// <summary>
    /// Get current stress level
    /// </summary>
    public int GetCurrentStressLevel()
    {
        return saveData != null ? saveData.mother_stress_level : 0;
    }
    
    /// <summary>
    /// Get current day
    /// </summary>
    public int GetCurrentDay()
    {
        return saveData != null ? saveData.day : 1;
    }
    
    /// <summary>
    /// Enable or disable stress change animations
    /// </summary>
    public void SetAnimationEnabled(bool enabled)
    {
        enableStressChangeAnimation = enabled;
    }
    
    /// <summary>
    /// Get whether animations are currently enabled
    /// </summary>
    public bool IsAnimationEnabled()
    {
        return enableStressChangeAnimation;
    }
    
    /// <summary>
    /// Test method to cycle through different stress levels and days to see color changes
    /// </summary>
    [ContextMenu("Test Day and Stress Color Cycle")]
    public void TestDayAndStressColorCycle()
    {
        if (saveData == null)
            return;
        
        int[] testDays = { 1, 5, 12, 13, 15, 20 };
        int[] testStressLevels = { 0, 25, 100, 200, 400, 600, 800, 1000 };
        
        foreach (int testDay in testDays)
        {
            saveData.day = testDay;
            
            foreach (int testStress in testStressLevels)
            {
                saveData.mother_stress_level = testStress;
                UpdateDayAndStressBasedColors();
            }
        }
    }
    
    /// <summary>
    /// Test method to show how day affects colors even at 0 stress
    /// </summary>
    [ContextMenu("Test Day Influence on Zero Stress")]
    public void TestDayInfluenceOnZeroStress()
    {
        if (saveData == null)
            return;
        
        // Set stress to 0 and test different days
        saveData.mother_stress_level = 0;
        
        // Test early days (bright colors)
        for (int day = 1; day <= 12; day++)
        {
            saveData.day = day;
            UpdateDayAndStressBasedColors();
        }
        
        // Test late days (dark colors)
        for (int day = 13; day <= 20; day++)
        {
            saveData.day = day;
            UpdateDayAndStressBasedColors();
        }
    }
    
    /// <summary>
    /// Test method to verify fill amounts work correctly with the new scaling
    /// </summary>
    [ContextMenu("Test Fill Amount Calculation")]
    public void TestFillAmountCalculation()
    {
        if (saveData == null)
            return;
        
        int[] testStressLevels = { 0, 25, 100, 200, 400, 600, 800, 1000, 1200, 1500 };
        
        foreach (int testStress in testStressLevels)
        {
            saveData.mother_stress_level = testStress;
            UpdateStressBarImmediate();
        }
    }
    
    /// <summary>
    /// Quick test to set stress to 1000 and verify bar is full
    /// </summary>
    [ContextMenu("Test 1000 Stress Level")]
    public void Test1000StressLevel()
    {
        if (saveData != null)
        {
            SetStressLevel(1000);
        }
    }
    
    /// <summary>
    /// Quick test to set stress to various levels around 1000
    /// </summary>
    [ContextMenu("Test Critical Stress Levels")]
    public void TestCriticalStressLevels()
    {
        if (saveData == null)
            return;
        
        int[] criticalLevels = { 999, 1000, 1001, 1500 };
        
        foreach (int stress in criticalLevels)
        {
            SetStressLevel(stress);
        }
    }
    
    /// <summary>
    /// Test method to verify stress level 50 behavior
    /// </summary>
    [ContextMenu("Test Stress 50 Debug")]
    public void TestStress50Debug()
    {
        if (saveData != null)
        {
            SetStressLevel(50);
        }
    }
    
    /// <summary>
    /// Test animated stress changes
    /// </summary>
    [ContextMenu("Test Stress Animation")]
    public void TestStressAnimation()
    {
        if (saveData == null)
            return;
        
        StartCoroutine(TestStressAnimationCoroutine());
    }
    
    private System.Collections.IEnumerator TestStressAnimationCoroutine()
    {
        // Test increasing stress
        for (int i = 0; i <= 1000; i += 100)
        {
            SetStressLevel(i);
            yield return new WaitForSeconds(animationDuration + 0.1f);
        }
        
        yield return new WaitForSeconds(1f);
        
        // Test decreasing stress
        for (int i = 1000; i >= 0; i -= 150)
        {
            SetStressLevel(i);
            yield return new WaitForSeconds(animationDuration + 0.1f);
        }
    }
}
