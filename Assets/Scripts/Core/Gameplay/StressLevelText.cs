using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StressLevelText : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI stressLevelText; // TextMeshPro component to display stress level name
    
    [Header("Save Data")]
    [SerializeField] private CoreGameSaves saveData;
    [SerializeField] private string saveDataPath = "Saves/coregamesaves"; // Path in Resources folder
    
    [Header("Stress Level Names")]
    [SerializeField] private string normalText = "Normal";
    [SerializeField] private string babyBluesText = "BabyBlues";
    [SerializeField] private string depresiText = "Depresi PostPartum";
    [SerializeField] private string psikosisText = "Psikosis Pospartum";
    
    private int previousStressLevel = -1;
    
    void Start()
    {
        // Auto-assign TextMeshPro component if not set
        if (stressLevelText == null)
        {
            stressLevelText = GetComponent<TextMeshProUGUI>();
        }
        
        // Load save data if not assigned
        if (saveData == null)
        {
            saveData = Resources.Load<CoreGameSaves>(saveDataPath);
        }
        
        // Initial update
        UpdateStressLevelText();
    }
    
    void Update()
    {
        if (saveData == null) return;
        
        // Only update if stress level changed
        if (saveData.mother_stress_level != previousStressLevel)
        {
            UpdateStressLevelText();
            previousStressLevel = saveData.mother_stress_level;
        }
    }
    
    /// <summary>
    /// Update the text based on current stress level
    /// </summary>
    private void UpdateStressLevelText()
    {
        if (saveData == null || stressLevelText == null) return;
        
        int stressLevel = saveData.mother_stress_level;
        string textToDisplay;
        
        if (stressLevel >= 900)
        {
            textToDisplay = psikosisText; // Psikosis Pospartum
        }
        else if (stressLevel >= 700)
        {
            textToDisplay = depresiText; // Depresi PostPartum
        }
        else if (stressLevel >= 6)
        {
            textToDisplay = babyBluesText; // BabyBlues
        }
        else
        {
            textToDisplay = normalText; // Normal
        }
        
        stressLevelText.text = textToDisplay;
    }
    
    /// <summary>
    /// Manually set the stress level for testing
    /// </summary>
    public void SetStressLevelForTest(int newStressLevel)
    {
        if (saveData != null)
        {
            saveData.mother_stress_level = newStressLevel;
            UpdateStressLevelText();
        }
    }
    
    /// <summary>
    /// Get current stress level text
    /// </summary>
    public string GetCurrentStressText()
    {
        if (saveData == null) return normalText;
        
        int stressLevel = saveData.mother_stress_level;
        
        if (stressLevel >= 900) return psikosisText;
        if (stressLevel >= 700) return depresiText;
        if (stressLevel >= 6) return babyBluesText;
        return normalText;
    }
}
