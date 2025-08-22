using UnityEngine;
using System.Collections;

public class NarratorDay2 : NarratorBase
{
    [System.Obsolete]
    protected override IEnumerator PlayMorningSequence()
    {

        saveFileManager.UpdateCoreGameSaves(1,0);
        saveFileManager.SaveToLocalMyGamesFolder();

        DisableNavMeshAgent(CharacterType.Father);
        DisableNavMeshAgent(CharacterType.Mother);
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 8.00f; 
        AppearObjects();
        SetCharacterSpawn(CharacterType.Mother, 0);  
        SetCharacterSpawn(CharacterType.Father, 0);    
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Object, 1);
        PlayCharacterAnimation(CharacterType.Father, "Sitting_Talking");
        PlayCharacterAnimation(CharacterType.Mother, "Idle");
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 2\nHari Pertamaku";
        yield return new WaitForSeconds(5f);
        uiElements.narratorText.gameObject.SetActive(false);

        bool seq0Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq0PagiPertama", 
            () => { seq0Complete = true; });
        yield return new WaitUntil(() => seq0Complete);
        yield return new WaitForSeconds(0.5f);
        FadeOpenEyes();
        yield return new WaitForSeconds(2f);
        PlayCharacterAnimation(CharacterType.Mother, "Angry");
        yield return new WaitForSeconds(1f);
        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq1PagiPertama", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        yield return new WaitForSeconds(1f);

        FadeCloseEyes();
        
        StartCoroutine(ResetHeadTracking());
        yield return new WaitForSeconds(2f);
        
        SetCharacterSpawn(CharacterType.Baby, 1); 
        SetCharacterSpawn(CharacterType.Father, 1);
        SetCharacterSpawn(CharacterType.Mother, 1);

        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(SetCameraPanRangeFront());
        FadeOpenEyes(); 

        EnableNavMeshAgent(CharacterType.Father);
        EnableNavMeshAgent(CharacterType.Mother);
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 0));
        SetCharacterSpawn(CharacterType.Father, 2); 
        yield return new WaitForSeconds(1f);
        

        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 1));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 1));
        PlayCharacterAnimation(CharacterType.Father, "Left Turn");
        PlayCharacterAnimation(CharacterType.Mother, "Right Turn");
        
        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq2KeberangkatanAyah", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 2));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 2));
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq3Mandi", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        bool seq1_1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/SFX/Seq3Shower", 
            () => { seq1_1Complete = true; });
        yield return new WaitForSeconds(2f);
        yield return new WaitUntil(() => seq1_1Complete);
        StartCoroutine(ResetHeadTracking());
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        yield return StartCoroutine(SetCameraPanRangeRight());
        SetCharacterSpawn(CharacterType.Baby, 2); 
        SetCharacterSpawn(CharacterType.Mother, 2);
        yield return new WaitForSeconds(2f);

        FadeOpenEyes(); 

        yield return new WaitForSeconds(1f);
        
        bool seq4Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq4SelesaiMandi", 
            () => { seq4Complete = true; });
        yield return new WaitUntil(() => seq4Complete);
        
        yield return new WaitForSeconds(1f);
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(1, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 13.0f; 
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 3); 
        
        yield return new WaitForSeconds(1f);
        
        FadeOpenEyes(); 

        yield return new WaitForSeconds(1f);
        
        bool seq5Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq5Lapar", 
            () => { seq5Complete = true; });
        yield return new WaitUntil(() => seq5Complete);
        
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 3));
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        
        bool seq6Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq6Rewel", 
            () => { seq6Complete = true; });
        yield return new WaitUntil(() => seq6Complete);
        
        yield return new WaitForSeconds(1f);
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayEveningSequence()
    {
        saveFileManager.UpdateCoreGameSaves(1, 2);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 18.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 3);
        yield return new WaitForSeconds(1f);
        
        FadeOpenEyes();
        yield return new WaitForSeconds(1f);
        
        bool seq7Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq7Mengompol", 
            () => { seq7Complete = true; });
        yield return new WaitUntil(() => seq7Complete);
        
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 3));
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        bool seq8Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq8Kesal", 
            () => { seq8Complete = true; });
        yield return new WaitUntil(() => seq8Complete);
        
        yield return new WaitForSeconds(1f);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(SetCameraPanRangeFront());
        SetCharacterSpawn(CharacterType.Baby, 1);
        SetCharacterSpawn(CharacterType.Mother, 1);
        SetCharacterSpawn(CharacterType.Father, 3);
        FadeOpenEyes();

        StartCoroutine(ResetHeadTracking());
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 3));
        StartCoroutine(SetHeadTarget(CharacterType.Father, CharacterTarget.Baby));
        
        bool seq9Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq9AyahPulang", 
            () => { seq9Complete = true; });
        yield return new WaitUntil(() => seq9Complete);
        
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 4));
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Father, 0.2f));
        StartCoroutine(SetHeadTarget(CharacterType.Father, CharacterTarget.Mother, 0.2f));
        
        bool seq10Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq10MenyambutAyah", 
            () => { seq10Complete = true; });
        yield return new WaitUntil(() => seq10Complete);

        yield return new WaitForSeconds(1f);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(4f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        DisableNavMeshAgent(CharacterType.Mother);
        DisableNavMeshAgent(CharacterType.Father);
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 20.0f; 
        SetCharacterSpawn(CharacterType.Baby, 3);
        SetCharacterSpawn(CharacterType.Mother, 4);
        SetCharacterSpawn(CharacterType.Father, 4);
        SetObjectsActive(gameObjects.inActiveObjects, false); 
        // SetCharacterSpawn(CharacterType.Object, 0);


        yield return new WaitForSeconds(1f);
        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);

        bool seq11Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq11Memasak", 
            () => { seq11Complete = true; });
        yield return new WaitUntil(() => seq11Complete);

        // Set context for all raycast objects before enabling interaction
        SetRaycastContext("Day2", "Night");

        // Enable raycast interaction system for player choice
        this.EnableRaycastInteraction();

        // Wait for CORRECT parent interaction - loop until player interacts with Father (Mulyono)
        bool correctInteraction = false;
        while (!correctInteraction)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) => {
                Debug.Log($"[Day2] Player interacted with: {characterIdentity}");
                
                if (characterIdentity == "Mulyono") // Father - CORRECT interaction
                {
                    Debug.Log("[Day2] CORRECT! Father interaction - continuing to next sequence");
                    correctInteraction = true; // Exit the loop
                }
                else if (characterIdentity == "Linda") // Mother - WRONG interaction  
                {
                    Debug.Log("[Day2] WRONG! Mother interaction - player must interact with Father instead");
                    // correctInteraction remains false - loop continues
                }
                else
                {
                    Debug.Log($"[Day2] Unknown character: {characterIdentity} - loop continues");
                    // correctInteraction remains false - loop continues
                }
            }, "Day2", "Night"));
            
            // Small delay before allowing next interaction attempt
            if (!correctInteraction)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // SetCharacterSpawn(CharacterType.Object, 1);

        // Disable raycast interaction system after player made correct choice
        this.DisableRaycastInteraction();
        SetObjectsActive(gameObjects.activeObjects, true);

        yield return new WaitForSeconds(1f);
        
        EnableNavMeshAgent(CharacterType.Father);
        EnableNavMeshAgent(CharacterType.Mother);

        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Father, 4));
        yield return StartCoroutine(MoveCharacterToPosition(CharacterType.Object, 0, 1f));
        StartCoroutine(SetHeadTarget(CharacterType.Father, CharacterTarget.Baby, 0.5f));
        yield return new WaitForSeconds(1f);
        SetObjectsActive(gameObjects.inActiveObjects, false);
        
        
        bool seq13Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq13SusuBotol", 
            () => { seq13Complete = true; });
        yield return new WaitUntil(() => seq13Complete);
        
        yield return new WaitForSeconds(1f);
        FadeCloseEyes();
        yield return new WaitForSeconds(4f);

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
        PlayCharacterAnimation(CharacterType.Father, "Laying Sleeping");
        PlayCharacterAnimation(CharacterType.Mother, "Laying Sleeping");
        yield return new WaitForSeconds(3f);
        
        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);
        
        bool seq14Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day2/Seq14Terbangun", 
            () => { seq14Complete = true; });
        yield return new WaitUntil(() => seq14Complete);
        
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
