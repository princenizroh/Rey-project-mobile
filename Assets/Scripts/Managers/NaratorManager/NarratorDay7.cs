using UnityEngine;
using System.Collections;

public class NarratorDay7 : NarratorBase
{
    [Header("Charge Meter")]
    public GameObject chargeMeterObject;
    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(6, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 13.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 7\nSendirian";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);

        FadeOpenEyes();
        yield return new WaitForSeconds(1f);

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day7/Seq1Lapar", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        // ChargeMeter untuk "menangis makin keras" - Seq1 Lapar  
        yield return StartCoroutine(PlayChargeMeterSequence(chargeMeterObject));
        
        yield return new WaitForSeconds(1f);
        
        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day7/Seq2Sendirian", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        
        // ChargeMeter untuk "menangis makin keras" - Seq2 Sendirian
        yield return StartCoroutine(PlayChargeMeterSequence(chargeMeterObject));
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayEveningSequence()
    {
        saveFileManager.UpdateCoreGameSaves(6, 2);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 18.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        yield return new WaitForSeconds(1f);
        
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day7/Seq3IbuPulang", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);
        
        yield return new WaitForSeconds(1f);
        
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(6, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 1.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        SetCharacterSpawn(CharacterType.Ghost, 0);
        PlayCharacterAnimation(CharacterType.Mother, "Sit To Stand");
        
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        
        bool seq4_1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day7/SFX/Seq4TeriakanKeras", 
            () => { seq4_1Complete = true; });
        yield return new WaitUntil(() => seq4_1Complete);
        yield return new WaitForSeconds(1f);
         
        bool seq4Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day7/Seq4Kelaparan", 
            () => { seq4Complete = true; });
        yield return new WaitUntil(() => seq4Complete);
        
        yield return new WaitForSeconds(1f);
        FadeOpenEyes(); 
        yield return new WaitForSeconds(6f);
        
        yield return StartCoroutine(MoveCharacterToPosition(CharacterType.Ghost, 0, 0.5f));
        StartCoroutine(SetHeadTarget(CharacterType.Ghost, CharacterTarget.Baby));
         
        bool seq5Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day7/Seq5SosokMenyeramkan", 
            () => { seq5Complete = true; });
        yield return new WaitUntil(() => seq5Complete);
        SetObjectsActive(gameObjects.activeObjects, true);
        bool seq5_1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day7/SFX/Seq5TeriakanMakinKeras", 
            () => { seq5_1Complete = true; });
        yield return new WaitUntil(() => seq5_1Complete);
        SetCharacterSpawn(CharacterType.Ghost, 1);
        SetObjectsActive(gameObjects.inActiveObjects, false);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));

        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));

        yield return new WaitForSeconds(1f);
        bool seq6Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day7/Seq6Khawatir", 
            () => { seq6Complete = true; });
        yield return new WaitUntil(() => seq6Complete);

        yield return new WaitForSeconds(1f);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextDay();
    }
}
