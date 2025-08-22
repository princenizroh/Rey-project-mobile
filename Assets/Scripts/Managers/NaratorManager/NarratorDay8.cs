using UnityEngine;
using System.Collections;

public class NarratorDay8 : NarratorBase
{
    [Header("Charge Meter")]
    public GameObject chargeMeterObject;
    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(7, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 13.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 8\nIbu Marah Besar";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);

        FadeOpenEyes();
        yield return new WaitForSeconds(1f);

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/Seq1Lapar", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        yield return new WaitForSeconds(1f);
        
        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/Seq2MencariIbu", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        
        // ChargeMeter untuk "menangis makin keras" - Seq2 MencariIbu
        yield return StartCoroutine(PlayChargeMeterSequence(chargeMeterObject));
        
        yield return new WaitForSeconds(1f);
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 1));
        
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/Seq3MarahBesar", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(7, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 1.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        SetCharacterSpawn(CharacterType.Ghost, 0);
        SetObjectsActive(gameObjects.activeObjects, true);

        yield return new WaitForSeconds(1f);
        // PlayAudio("supernatural_intense");
        
        bool seq4_1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/SFX/Seq4Teriakan", 
            () => { seq4_1Complete = true; });
        yield return new WaitUntil(() => seq4_1Complete);
        yield return new WaitForSeconds(1f);
        FadeOpenEyes(); 
        yield return new WaitForSeconds(2f);
        
        
        bool seq4Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/Seq4GangguanSetanSangatParah", 
            () => { seq4Complete = true; });
        yield return new WaitUntil(() => seq4Complete);
        
        yield return new WaitForSeconds(4f);

        StartCoroutine(SetHeadTarget(CharacterType.Ghost, CharacterTarget.Baby));
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(MoveCharacterToPosition(CharacterType.Ghost, 0));
        yield return new WaitForSeconds(3f);
        SetCharacterSpawn(CharacterType.Ghost, 1);
        bool seq4_2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/SFX/Seq4Jendela", 
            () => { seq4_2Complete = true; });
        yield return new WaitUntil(() => seq4_2Complete);
        yield return new WaitForSeconds(3f);
        SetCharacterSpawn(CharacterType.Ghost, 2);
        bool seq4_3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/SFX/Seq4Jendela", 
            () => { seq4_3Complete = true; });
        yield return new WaitUntil(() => seq4_3Complete);
        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(MoveCharacterToPosition(CharacterType.Ghost, 1, 0.5f));
        yield return new WaitForSeconds(1f);
        SetCharacterSpawn(CharacterType.Ghost, 0);
        yield return new WaitForSeconds(2f);
        SetCharacterSpawn(CharacterType.Ghost, 3);
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(MoveCharacterToPosition(CharacterType.Ghost, 2, 0.5f));
        bool seq5Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/seq5DiaDisini", 
            () => { seq5Complete = true; });
        yield return new WaitUntil(() => seq5Complete);
        
        //Play animasi mencekik
        // Charge meter di sini
        yield return StartCoroutine(PlayChargeMeterSequence(chargeMeterObject));

        yield return new WaitForSeconds(1f);

        
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));
        bool seq5_1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/SFX/Seq5TeriakanKeras", 
            () => { seq5_1Complete = true; });
        yield return new WaitUntil(() => seq5_1Complete);

        SetCharacterSpawn(CharacterType.Ghost, 0);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 1));
        
        bool seq6Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day8/Seq6Keputusasaan", 
            () => { seq6Complete = true; });
        yield return new WaitUntil(() => seq6Complete);
        
        // if (audioSource != null && audioSource.isPlaying)
        // {
        //     StartCoroutine(FadeOutAudio(audioSource, 3f)); 
        // }
        
        SetObjectsActive(gameObjects.inActiveObjects, false);
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextDay();
    }
}
