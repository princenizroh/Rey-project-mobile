using UnityEngine;
using System.Collections;

public class NarratorDay6 : NarratorBase
{
    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(5, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 13.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 6\nKeraguan";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);

        FadeOpenEyes();
        yield return new WaitForSeconds(1f);

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day6/Seq1Keraguan", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        FadeCloseEyes();
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayEveningSequence()
    {
        saveFileManager.UpdateCoreGameSaves(5, 2);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 18.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        
        yield return new WaitForSeconds(1f);
        
        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day6/Seq2Heran", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        
       yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(5, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 1.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        SetCharacterSpawn(CharacterType.Ghost, 0);
        bool seq3_1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day6/SFXSeq3Teriakan", 
            () => { seq3_1Complete = true; });
        yield return new WaitUntil(() => seq3_1Complete);
        
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day6/Seq3Gangguan", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);
        
        yield return new WaitForSeconds(1f);
        FadeOpenEyes();
        yield return new WaitForSeconds(4f);
        
        StartCoroutine(SetHeadTarget(CharacterType.Ghost, CharacterTarget.Baby));
        
        bool seq4Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day6/Seq4Sosok", 
            () => { seq4Complete = true; });
        yield return new WaitUntil(() => seq4Complete);
        
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(MoveCharacterToPosition(CharacterType.Ghost, 0, 0.5f));
        bool seq5Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day6/Seq5Mendekat", 
            () => { seq5Complete = true; });
        yield return new WaitUntil(() => seq5Complete);
        SetCharacterSpawn(CharacterType.Ghost, 1);
         
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));
        
        bool seq6Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day6/Seq6Khawatir", 
            () => { seq6Complete = true; });
        yield return new WaitUntil(() => seq6Complete);
        
        yield return new WaitForSeconds(1f);
        FadeCloseEyes();
        yield return new WaitForSeconds(2f);
        
        GoToNextDay();
    }
}
