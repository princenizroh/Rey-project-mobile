using UnityEngine;

/// <summary>
/// Example script showing how to use NarratorMainMenu
/// Attach this to a UI button or call from other scripts
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NarratorMainMenu narratorMainMenu;
    
    [Header("Debug")]
    [SerializeField] private bool autoFindNarrator = true;

    void Start()
    {
        if (autoFindNarrator && narratorMainMenu == null)
        {
            narratorMainMenu = FindFirstObjectByType<NarratorMainMenu>();
        }
    }

    /// <summary>
    /// Call this from UI button to play main menu animations based on save
    /// </summary>
    public void PlayMainMenuAnimations()
    {
        if (narratorMainMenu == null)
        {
            Debug.LogError("[MainMenuController] NarratorMainMenu not assigned!");
            return;
        }

        narratorMainMenu.PlayMainMenuSequence();
    }

    /// <summary>
    /// Alternative: Use NarratorManager to start main menu
    /// </summary>
    public void StartMainMenuViaManager()
    {
        if (NarratorManager.Instance != null)
        {
            NarratorManager.Instance.StartMainMenu();
        }
        else
        {
            Debug.LogError("[MainMenuController] NarratorManager.Instance is null!");
        }
    }

    /// <summary>
    /// Force play specific day animation (for testing)
    /// </summary>
    public void ForcePlayDay(int day)
    {
        if (narratorMainMenu == null)
        {
            Debug.LogError("[MainMenuController] NarratorMainMenu not assigned!");
            return;
        }

        narratorMainMenu.ForcePlayDayAnimation(day);
    }

    /// <summary>
    /// Show current save day in console
    /// </summary>
    public void ShowCurrentDay()
    {
        if (narratorMainMenu == null)
        {
            Debug.LogError("[MainMenuController] NarratorMainMenu not assigned!");
            return;
        }

        int currentDay = narratorMainMenu.GetCurrentDay();
        Debug.Log($"[MainMenuController] Current save day: {currentDay}");
    }

    #region UI Button Methods (assign these to buttons)
    
    public void OnPlayMainMenuClicked()
    {
        PlayMainMenuAnimations();
    }
    
    public void OnTestDay1Clicked()
    {
        ForcePlayDay(1);
    }
    
    public void OnTestDay7Clicked()
    {
        ForcePlayDay(7);
    }
    
    public void OnTestDay14Clicked()
    {
        ForcePlayDay(14);
    }
    
    public void OnShowCurrentDayClicked()
    {
        ShowCurrentDay();
    }
    
    #endregion
}
