using UnityEngine;
using System.Collections;

public class NarratorDay3 : NarratorBase
{
    [System.Obsolete]
    protected override IEnumerator PlayMorningSequence()
    {
        saveFileManager.UpdateCoreGameSaves(2, 0);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        DisableNavMeshAgent(CharacterType.Mother);
        DisableNavMeshAgent(CharacterType.Father);
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 8.00f; 
        AppearObjects();
        SetCharacterSpawn(CharacterType.Mother, 0);  
        SetCharacterSpawn(CharacterType.Father, 0);    
        SetCharacterSpawn(CharacterType.Baby, 0);
        PlayCharacterAnimation(CharacterType.Father, "Idle");
        PlayCharacterAnimation(CharacterType.Mother, "Idle");
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        StartCoroutine(SetHeadTarget(CharacterType.Father, CharacterTarget.Baby));
        
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 3\nPagi Kedua";
        yield return new WaitForSeconds(5f);
        uiElements.narratorText.gameObject.SetActive(false);

        bool seq0Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq0PagiKedua", 
            () => { seq0Complete = true; });
        yield return new WaitUntil(() => seq0Complete);
        
        yield return new WaitForSeconds(0.5f);

        FadeOpenEyes();

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq1PagiKedua", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        yield return new WaitForSeconds(1f);

        FadeCloseEyes();

        yield return new WaitForSeconds(2f);
        EnableNavMeshAgent(CharacterType.Father);
        EnableNavMeshAgent(CharacterType.Mother);
        SetCharacterSpawn(CharacterType.Baby, 1); 
        SetCharacterSpawn(CharacterType.Father, 1);
        SetCharacterSpawn(CharacterType.Mother, 1);

        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(SetCameraPanRangeFront());
        FadeOpenEyes(); 

        StartCoroutine(ResetHeadTracking());
        yield return new WaitForSeconds(1f);
        SetCharacterSpawn(CharacterType.Baby, 1); 
        SetCharacterSpawn(CharacterType.Father, 1);
        SetCharacterSpawn(CharacterType.Mother, 1);
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 0));
        SetCharacterSpawn(CharacterType.Father, 2); 
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 1));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 1));

        PlayCharacterAnimation(CharacterType.Father, "Left Turn");
        PlayCharacterAnimation(CharacterType.Mother, "Right Turn");
        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq2KepergianAyah", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 2));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 2));
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        

        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq3Mandi", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);

        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(2, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 13.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 3);
        
        yield return new WaitForSeconds(1f);
        
        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);
        
        bool seq4Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq4Lapar", 
            () => { seq4Complete = true; });
        yield return new WaitUntil(() => seq4Complete);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 3));
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        yield return new WaitForSeconds(1f);
        
        bool seq5Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq5Rewel", 
            () => { seq5Complete = true; });
        yield return new WaitUntil(() => seq5Complete);
        
        yield return new WaitForSeconds(1f);
        
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayEveningSequence()
    {
        saveFileManager.UpdateCoreGameSaves(2, 2);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 18.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 3);
        yield return new WaitForSeconds(1f);

        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);

        bool seq6Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq6Mengompol", 
            () => { seq6Complete = true; });
        yield return new WaitUntil(() => seq6Complete);
        
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 3));
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        bool seq7Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq7Kesal", 
            () => { seq7Complete = true; });
        yield return new WaitUntil(() => seq7Complete);
        
        yield return new WaitForSeconds(1f);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(SetCameraPanRangeFront());
        SetCharacterSpawn(CharacterType.Baby, 1); 
        SetCharacterSpawn(CharacterType.Father, 3);
        SetCharacterSpawn(CharacterType.Mother, 1);
        yield return new WaitForSeconds(2f);
        FadeOpenEyes(); 

        StartCoroutine(ResetHeadTracking());
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 3));
        StartCoroutine(SetHeadTarget(CharacterType.Father, CharacterTarget.Baby));
        
        bool seq8Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq8AyahPulang", 
            () => { seq8Complete = true; });
        yield return new WaitUntil(() => seq8Complete);
        
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 4));
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Father, 0.2f));
        StartCoroutine(SetHeadTarget(CharacterType.Father, CharacterTarget.Mother, 0.2f));
        
        bool seq9Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq9MenyambutAyah", 
            () => { seq9Complete = true; });
        yield return new WaitUntil(() => seq9Complete);

        yield return new WaitForSeconds(1f);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(4f);
        
        GoToNextTimeOfDay();
    }

    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        StartCoroutine(ResetHeadTracking());
        DisableNavMeshAgent(CharacterType.Mother);
        DisableNavMeshAgent(CharacterType.Father);
        yield return StartCoroutine(SetCameraPanRangeFront());
        TimeManager.instance.TimeOfDay = 1.0f;
        SetCharacterSpawn(CharacterType.Baby, 1);
        SetCharacterSpawn(CharacterType.Mother, 4);
        SetCharacterSpawn(CharacterType.Father, 4);
        PlayCharacterAnimation(CharacterType.Mother, "Sitting_Talking");
        PlayCharacterAnimation(CharacterType.Father, "Sitting_Kaki_Gerak");
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Father, 0.2f));
        StartCoroutine(SetHeadTarget(CharacterType.Father, CharacterTarget.Mother, 0.2f));

        yield return new WaitForSeconds(1f);
        FadeOpenEyes();
        yield return new WaitForSeconds(1f);

        bool seq10Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq10MakanMalam",
            () => { seq10Complete = true; });
        yield return new WaitUntil(() => seq10Complete);

        yield return new WaitForSeconds(1f);

        SetRaycastContext("Day3", "Night");
        
        this.EnableRaycastInteraction();

        bool correctInteraction = false;
        while (!correctInteraction)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) => {
                Debug.Log($"[Day3] Player interacted with: {characterIdentity}");
                
                if (characterIdentity == "Mulyono") 
                {
                    Debug.Log("[Day3] CORRECT! Father interaction - continuing to next sequence");
                    correctInteraction = true; // Exit the loop
                }
                else if (characterIdentity == "Linda") // Mother - WRONG interaction  
                {
                    Debug.Log("[Day3] WRONG! Mother interaction - player must interact with Father instead");
                    // correctInteraction remains false - loop continues
                }
                else
                {
                    Debug.Log($"[Day3] Unknown character: {characterIdentity} - loop continues");
                    // correctInteraction remains false - loop continues
                }
            }, "Day3", "Night"));
            
            // Small delay before allowing next interaction attempt
            if (!correctInteraction)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        this.DisableRaycastInteraction();
        SetObjectsActive(gameObjects.activeObjects, true);

        yield return new WaitForSeconds(1f);
        PlayCharacterAnimation(CharacterType.Father, "Sit To Stand");
        EnableNavMeshAgent(CharacterType.Father);
         
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 3));

        StartCoroutine(SetHeadTarget(CharacterType.Father, CharacterTarget.Baby));
        yield return new WaitForSeconds(1f);

        SetObjectsActive(gameObjects.inActiveObjects, false);

        bool seq12Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq12SusuBotol",
            () => { seq12Complete = true; });
        yield return new WaitUntil(() => seq12Complete);
        yield return new WaitForSeconds(1f);

        FadeCloseEyes();
        yield return new WaitForSeconds(2f);

        StartCoroutine(PlayMidnightSequence());
        yield break;
    }
    
    [System.Obsolete]
    protected IEnumerator PlayMidnightSequence()
    {
        StartCoroutine(ResetHeadTracking());
        DisableNavMeshAgent(CharacterType.Mother);
        DisableNavMeshAgent(CharacterType.Father);
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 1.0f; 
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 5);
        SetCharacterSpawn(CharacterType.Father, 5);        
        PlayCharacterAnimation(CharacterType.Mother, "Laying Sleeping");
        PlayCharacterAnimation(CharacterType.Father, "Laying Sleeping");
 
        yield return new WaitForSeconds(3f);
        FadeOpenEyes(); 
        yield return new WaitForSeconds(3f);
         
        bool seq13Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day3/Seq13Terbangun", 
            () => { seq13Complete = true; });
        yield return new WaitUntil(() => seq13Complete);
        yield return new WaitForSeconds(1f);

        // Enable raycast interaction system for player choice
        yield return new WaitForSeconds(1f);
        SetCharacterSpawn(CharacterType.Mother, 6);
        EnableNavMeshAgent(CharacterType.Mother);
        yield return new WaitForSeconds(1f);
        
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 3));
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        GoToNextDay();
    }
}
