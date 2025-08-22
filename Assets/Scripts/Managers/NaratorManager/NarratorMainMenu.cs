using UnityEngine;
using System.Collections;

public class NarratorMainMenu : NarratorBase
{
    [Header("Main Menu Settings")]
    [SerializeField] private bool autoPlayOnStart = true;
    [SerializeField] private float delayBeforePlay = 1f;

    protected override void Start()
    {
        base.Start();
        
        if (autoPlayOnStart)
        {
            StartCoroutine(DelayedMainMenuPlay());
        }
    }


    [System.Obsolete]
    public new IEnumerator StartNarration()
    {
        PlayMainMenuSequence();
        yield break; 
    }

    [System.Obsolete]
    protected override IEnumerator Narrate()
    {
        yield break; 
    }

    private IEnumerator DelayedMainMenuPlay()
    {
        yield return new WaitForSeconds(delayBeforePlay);
        PlayMainMenuSequence();
    }

    [ContextMenu("Play Main Menu Sequence")]
    public void PlayMainMenuSequence()
    {
        if (saveFileManager == null)
        {
            PlayDay1MainMenuAnimation();
            return;
        }

        int currentDay = GetCurrentSaveDay();
        
        
        PlayAnimationsForDay(currentDay);
    }

    private int GetCurrentSaveDay()
    {
        try
        {
            if (saveFileManager == null)
            {
                Debug.LogWarning("[NarratorMainMenu] SaveFileManager is null, defaulting to Day 1");
                return 1;
            }

            var saveDataField = saveFileManager.GetType().GetField("targetSaveObject", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (saveDataField != null)
            {
                var coreGameSaves = saveDataField.GetValue(saveFileManager);
                if (coreGameSaves != null)
                {
                    var dayField = coreGameSaves.GetType().GetField("day");
                    if (dayField != null)
                    {
                        int day = (int)dayField.GetValue(coreGameSaves);
                        int clampedDay = Mathf.Clamp(day, 1, 14); 
                        
                        return clampedDay;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NarratorMainMenu] Error getting save day: {e.Message}");
        }
        
        return 1; // Default to Day 1 if error
    }

    private void PlayAnimationsForDay(int day)
    {
        SetupMainMenuPositions();
        
        switch (day)
        {
            case 1:
                PlayDay1MainMenuAnimation();
                break;
            case 2:
                PlayDay2MainMenuAnimation();
                break;
            case 3:
                PlayDay3MainMenuAnimation();
                break;
            case 4:
                PlayDay4MainMenuAnimation();
                break;
            case 5:
                PlayDay5MainMenuAnimation();
                break;
            case 6:
                PlayDay6MainMenuAnimation();
                break;
            case 7:
                PlayDay7MainMenuAnimation();
                break;
            case 8:
                PlayDay8MainMenuAnimation();
                break;
            case 9:
                PlayDay9MainMenuAnimation();
                break;
            case 10:
                PlayDay10MainMenuAnimation();
                break;
            case 11:
                PlayDay11MainMenuAnimation();
                break;
            case 12:
                PlayDay12MainMenuAnimation();
                break;
            case 13:
                PlayDay13MainMenuAnimation();
                break;
            case 14:
                PlayDay14MainMenuAnimation();
                break;
            default:
                PlayDay1MainMenuAnimation();
                break;
        }
    }

    private void SetupMainMenuPositions()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 0);
        SafeSetCharacterSpawn(CharacterType.Father, 0);
        SafeSetCharacterSpawn(CharacterType.Baby, 0);
        SafeSetCharacterSpawn(CharacterType.Bidan, 0);
        SafeSetCharacterSpawn(CharacterType.Object, 0);
    }


    private void SafeSetCharacterSpawn(CharacterType characterType, int spawnIndex)
    {
        try
        {
            SetCharacterSpawn(characterType, spawnIndex);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NarratorMainMenu] Could not spawn {characterType}: {e.Message}");
        }
    }

    private void SafePlayCharacterAnimation(CharacterType characterType, string animationName)
    {
        try
        {
            PlayCharacterAnimation(characterType, animationName);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NarratorMainMenu] Could not animate {characterType} with {animationName}: {e.Message}");
        }
    }

    private void SafeSetHeadTarget(CharacterType characterType, CharacterTarget targetType)
    {
        try
        {
            if (headTrackingManager != null)
            {
                StartCoroutine(SetHeadTarget(characterType, targetType));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NarratorMainMenu] Could not set head target for {characterType}: {e.Message}");
        }
    }

    #region Day-Specific Animation Methods

    private void PlayDay1MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 0);
        SafeSetCharacterSpawn(CharacterType.Father, 0);
        SafeSetCharacterSpawn(CharacterType.Bidan, 0);
        SafeSetCharacterSpawn(CharacterType.Baby, 0);
        SafeSetCharacterSpawn(CharacterType.Object, 0);

        SafePlayCharacterAnimation(CharacterType.Mother, "Sit");
        SafePlayCharacterAnimation(CharacterType.Father, "Sitting");
        SafePlayCharacterAnimation(CharacterType.Bidan, "Idle");

        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
        SafeSetHeadTarget(CharacterType.Bidan, CharacterTarget.Baby);
        SafeSetHeadTarget(CharacterType.Father, CharacterTarget.Baby);
    }

    private void PlayDay2MainMenuAnimation()
    {

        SafeSetCharacterSpawn(CharacterType.Mother, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 1);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Angry");
        SafePlayCharacterAnimation(CharacterType.Father, "Sitting_Talking");
        
        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
    }

    private void PlayDay3MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 1);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Angry");
        SafePlayCharacterAnimation(CharacterType.Father, "Sitting_Talking");
        
        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
        SafeSetHeadTarget(CharacterType.Father, CharacterTarget.Baby);
    }

    private void PlayDay4MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 2);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);

        SafePlayCharacterAnimation(CharacterType.Mother, "Angry");
        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
    }

    private void PlayDay5MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 1);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Talking On Phone");        
        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
    }

    private void PlayDay6MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 1);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Thinking");
        
        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
    }

    private void PlayDay7MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 2);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Defeat");
        
        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
    }

    private void PlayDay8MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 1);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);

        SafePlayCharacterAnimation(CharacterType.Mother, "Sad Idle");

        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
        SafeSetHeadTarget(CharacterType.Ghost, CharacterTarget.Baby);
    }

    private void PlayDay9MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 1);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Sad Idle");
        
        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
    }

    private void PlayDay10MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 0);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Sad Idle");
        
        SafeSetHeadTarget(CharacterType.Mother, CharacterTarget.Baby);
    }

    private void PlayDay11MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 0);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Sitting Disbelief");
    }

    private void PlayDay12MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 0);
        SafeSetCharacterSpawn(CharacterType.Baby, 1);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Sitting Disbelief");
    }

    private void PlayDay13MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 0);
        SafeSetCharacterSpawn(CharacterType.Baby, 2);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Sitting");
    }

    private void PlayDay14MainMenuAnimation()
    {
        SafeSetCharacterSpawn(CharacterType.Mother, 0);
        SafeSetCharacterSpawn(CharacterType.Baby, 2);
        SafeSetCharacterSpawn(CharacterType.Father, 2);
        SafeSetCharacterSpawn(CharacterType.Bidan, 1);
        SafeSetCharacterSpawn(CharacterType.Object, 1);
        
        SafePlayCharacterAnimation(CharacterType.Mother, "Sitting");

    }
    
    #endregion

    #region Public Utility Methods
    public void ForcePlayDayAnimation(int day)
    {
        PlayAnimationsForDay(day);
    }

    public int GetCurrentDay()
    {
        return GetCurrentSaveDay();
    }
    
    public void RefreshMainMenu()
    {
        PlayMainMenuSequence();
    }
    
    public void ResetToMainMenuPositions()
    {
        SetupMainMenuPositions();
    }

    public void StopAllAnimations()
    {
        PlayCharacterAnimation(CharacterType.Mother, "Idle");
        PlayCharacterAnimation(CharacterType.Father, "Idle");
        PlayCharacterAnimation(CharacterType.Bidan, "Idle");
        PlayCharacterAnimation(CharacterType.Ghost, "Idle");
        
        StartCoroutine(ResetHeadTracking());
    }
    
    #endregion

    #region Context Menu Debug Methods
    
    [ContextMenu("Test Day 1 - Birth")]
    private void TestDay1() => ForcePlayDayAnimation(1);
    
    [ContextMenu("Test Day 2 - First Day")]
    private void TestDay2() => ForcePlayDayAnimation(2);
    
    [ContextMenu("Test Day 5 - Mother Angry")]
    private void TestDay5() => ForcePlayDayAnimation(5);
    
    [ContextMenu("Test Day 7 - Alone")]
    private void TestDay7() => ForcePlayDayAnimation(7);
    
    [ContextMenu("Test Day 8 - Supernatural Peak")]
    private void TestDay8() => ForcePlayDayAnimation(8);
    
    [ContextMenu("Test Day 12 - Chaos")]
    private void TestDay12() => ForcePlayDayAnimation(12);
    
    [ContextMenu("Test Day 14 - Final")]
    private void TestDay14() => ForcePlayDayAnimation(14);
    
    [ContextMenu("Get Current Save Day")]
    private void DebugCurrentDay()
    {
        int day = GetCurrentSaveDay();
    }
    
    [ContextMenu("Reset All Positions")]
    private void DebugResetPositions() => ResetToMainMenuPositions();
    
    [ContextMenu("Stop All Animations")]
    private void DebugStopAnimations() => StopAllAnimations();
    
    [ContextMenu("Refresh Main Menu")]
    private void DebugRefreshMainMenu() => RefreshMainMenu();
    
    #endregion
}
