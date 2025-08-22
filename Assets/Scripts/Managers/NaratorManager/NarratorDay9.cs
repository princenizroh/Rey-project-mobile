using UnityEngine;
using System.Collections;

public class NarratorDay9 : NarratorBase
{
    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(8, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        DisableNavMeshAgent(CharacterType.Mother);
        PlayCharacterAnimation(CharacterType.Mother, "Sitting_Sexy");
        PlayCharacterAnimation(CharacterType.Father, "Sitting");
        PlayCharacterAnimation(CharacterType.Mother, "Idle");
        TimeManager.instance.TimeOfDay = 13.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 9\nBerubah";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);

        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day9/Seq1Lapar", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        yield return new WaitForSeconds(1f);
        PlayCharacterAnimation(CharacterType.Mother, "Sit To Stand");
        EnableNavMeshAgent(CharacterType.Mother);
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));
        
        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day9/Seq2Diam", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        yield return new WaitForSeconds(1f);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(8, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 1.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        
        
        yield return new WaitForSeconds(1f);
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day9/Seq3PelampiasanEmosi", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);
        
        yield return new WaitForSeconds(2f);
        
        GoToNextDay();
    }
}
