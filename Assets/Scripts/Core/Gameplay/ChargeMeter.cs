using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class ChargeMeter : MonoBehaviour
{
    [Header("UI References")]
    private TMP_Text spaceSpamIndicator;
    private Image chargeMeterFillImage;
    
    [Header("Charge Settings")]
    private float chargeLevel = 0;
    [SerializeField] private float chargeRate = 10f;
    [SerializeField] private float pullbackThreshold = 25f; // Percentage when pullback starts
    [SerializeField] private float pullbackRate = 5f; // How fast it pulls back
    [SerializeField] private float maxChargeLevel = 100f;
    [SerializeField] private float successThreshold = 99f; // Percentage when considered successful
    
    [Header("Input Settings")]
    private float lastSpacePress = 0f;
    private float spacePressCooldown = 0.1f;

    void Start()
    {
        spaceSpamIndicator = GameObject.Find("TextSpam").GetComponent<TMP_Text>();
        if (spaceSpamIndicator == null)
        {
            Debug.LogError("SpaceSpamIndicator not found in the scene.");
        }

        chargeMeterFillImage = GameObject.Find("Indicator").GetComponent<Image>();
        if (chargeMeterFillImage == null)   
        {
            Debug.LogError("ChargeMeterFill not found in the scene.");
        }
    }
    
    /// <summary>
    /// Replacement untuk Input.GetKeyDown(KeyCode.Space) - now uses BaseInputHandler static method
    /// </summary>
    private bool GetSpaceKeyDown()
    {
        return BaseInputHandler.DialogKeyDown;
    }

    public void changeChargeRate(float newChargeRate)
    {
        chargeRate = newChargeRate;
        Debug.Log("Charge rate changed to: " + chargeRate);
    }

    public void changePullbackThreshold(float newThreshold)
    {
        pullbackThreshold = Mathf.Clamp(newThreshold, 0f, 100f);
        Debug.Log("Pullback threshold changed to: " + pullbackThreshold + "%");
    }

    public void changePullbackRate(float newPullbackRate)
    {
        pullbackRate = newPullbackRate;
        Debug.Log("Pullback rate changed to: " + pullbackRate);
    }

    public void resetChargeLevel()
    {
        chargeLevel = 0;
        Debug.Log("Charge level reset to: " + chargeLevel);
    }

    /// <summary>
    /// Get current charge level as percentage (0-100)
    /// </summary>
    public float GetChargePercentage()
    {
        return (chargeLevel / maxChargeLevel) * 100f;
    }

    /// <summary>
    /// Check if charge is above pullback threshold
    /// </summary>
    public bool IsInPullbackZone()
    {
        return GetChargePercentage() >= pullbackThreshold;
    }
    
    /// <summary>
    /// Check if charge is at or above success threshold
    /// </summary>
    public bool IsChargeSuccessful()
    {
        return GetChargePercentage() >= successThreshold;
    }
    
    /// <summary>
    /// Check if charge is at maximum level
    /// </summary>
    public bool IsChargeFull()
    {
        return chargeLevel >= maxChargeLevel;
    }
    
    /// <summary>
    /// Event that can be subscribed to when charge meter completes successfully
    /// </summary>
    public System.Action OnChargeSuccess;
    
    /// <summary>
    /// Deactivate the charge meter and reset values
    /// </summary>
    public void DeactivateChargeMeter()
    {
        chargeLevel = 0f;
        gameObject.SetActive(false);
        Debug.Log("[ChargeMeter] Deactivated and reset");
    }
    
    /// <summary>
    /// Manually activate the charge meter with custom settings
    /// </summary>
    public void ActivateChargeMeter(float customChargeRate = -1, float customPullbackThreshold = -1, float customPullbackRate = -1)
    {
        // Reset charge level
        chargeLevel = 0f;
        
        // Apply custom settings if provided
        if (customChargeRate > 0) chargeRate = customChargeRate;
        if (customPullbackThreshold > 0) pullbackThreshold = customPullbackThreshold;
        if (customPullbackRate > 0) pullbackRate = customPullbackRate;
        
        // Ensure UI elements are found
        if (spaceSpamIndicator == null)
        {
            spaceSpamIndicator = GameObject.Find("TextSpam")?.GetComponent<TMP_Text>();
        }
        if (chargeMeterFillImage == null)
        {
            chargeMeterFillImage = GameObject.Find("Indicator")?.GetComponent<Image>();
        }
        
        // Activate the object
        gameObject.SetActive(true);
        
        Debug.Log($"[ChargeMeter] Activated with settings - Rate: {chargeRate}, Pullback Threshold: {pullbackThreshold}%, Pullback Rate: {pullbackRate}");
    }

    void Update()
    {
        // Handle Space key press with cooldown
        // MIGRATED: Input.GetKeyDown(KeyCode.Space) -> GetSpaceKeyDown()
        if (GetSpaceKeyDown() && Time.time >= lastSpacePress + spacePressCooldown)
        {
            lastSpacePress = Time.time;
            
            // Visual feedback
            if (spaceSpamIndicator != null)
            {
                spaceSpamIndicator.fontSize = 50;
            }
            
            // Add charge
            chargeLevel += chargeRate;
            
            // Clamp to max level
            if (chargeLevel > maxChargeLevel)
            {
                chargeLevel = maxChargeLevel;
            }
            
            Debug.Log($"Space pressed! Charge level: {GetChargePercentage():F1}%");
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            // Visual feedback reset
            if (spaceSpamIndicator != null)
            {
                spaceSpamIndicator.fontSize = 60;
            }
        }

        // Apply pullback when above threshold
        if (IsInPullbackZone())
        {
            float pullbackAmount = pullbackRate * Time.deltaTime;
            chargeLevel -= pullbackAmount;
            
            // Don't let it go below 0
            if (chargeLevel < 0)
            {
                chargeLevel = 0;
            }
        }

        // Update visual meter
        if (chargeMeterFillImage != null)
        {
            chargeMeterFillImage.fillAmount = chargeLevel / maxChargeLevel;
        }

        // Check if meter reaches success threshold (99% or 100%)
        if (IsChargeSuccessful())
        {
            Debug.Log($"Charge meter successful! Level: {GetChargePercentage():F1}%");
            
            // Trigger success event if available
            OnChargeSuccess?.Invoke();
            
            // Deactivate and reset
            DeactivateChargeMeter();
        }
    }
}
