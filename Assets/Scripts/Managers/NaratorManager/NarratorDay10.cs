using UnityEngine;
using System.Collections;

public class NarratorDay10 : NarratorBase
{

    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(9, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        

        DisableNavMeshAgent(CharacterType.Mother);
        TimeManager.instance.TimeOfDay = 13.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 10\nKeanehan";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);

        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day10/Seq1Lapar", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        
        EnableNavMeshAgent(CharacterType.Mother);
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));
        
        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day10/Seq2Keanehan", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(9, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 1.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
         
        yield return new WaitForSeconds(1f);
        
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day10/Seq3Curhatan", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);
        yield return new WaitForSeconds(2f);
        
        GoToNextDay();
    }


}
