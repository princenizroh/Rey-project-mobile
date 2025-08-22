using UnityEngine;
using System.Collections;

public class NarratorDay4 : NarratorBase
{
    [Header("Charge Meter")]
    public GameObject chargeMeterObject;    
    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(3, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        DisableNavMeshAgent(CharacterType.Ghost);
        // SetObjectsActive(gameObjects.activeObjects, true);
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 13.0f; 
        SetCharacterSpawn(CharacterType.Baby, 0);   
        SetCharacterSpawn(CharacterType.Mother, 0);
        SetCharacterSpawn(CharacterType.Father, 0);
        
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 4\n Tempat Berbeda";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);

        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq1TempatBerbeda", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        // ChargeMeter untuk "menangis makin keras" - Seq1 TempatBerbeda
        yield return StartCoroutine(PlayChargeMeterSequence(chargeMeterObject));
        
        yield return new WaitForSeconds(1f);

       // PlayCharacterAnimation(CharacterType.Object, "OpenTheDoor");
        
        yield return new WaitForSeconds(1f);
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));

        StartCoroutine(SwitchLights.Instance.SwitchToBright());

        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq2IbuMarah", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        
        yield return new WaitForSeconds(1f);
        
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq3GangguanTelpon", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);
        
        yield return new WaitForSeconds(1f);
        StartCoroutine(ResetHeadTracking());
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 1));
        PlayCharacterAnimation(CharacterType.Mother, "Talking On Phone");
        
        bool seq4Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq4Telephone", 
            () => { seq4Complete = true; });
        yield return new WaitUntil(() => seq4Complete);
        
        yield return new WaitForSeconds(1f);
        FadeCloseEyes(); 
        yield return new WaitForSeconds(5f);

        bool seq5Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq5Kerja", 
            () => { seq5Complete = true; });
        yield return new WaitUntil(() => seq5Complete);
        
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayEveningSequence()
    {
        saveFileManager.UpdateCoreGameSaves(3, 2);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        // AppearObjects();
        TimeManager.instance.TimeOfDay = 18.0f; 
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        yield return new WaitForSeconds(3f);
        
        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);
        
        bool seq6Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq6Lapar", 
            () => { seq6Complete = true; });
        yield return new WaitUntil(() => seq6Complete);
        
        yield return new WaitForSeconds(1f);
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby)); 
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));

        StartCoroutine(SwitchLights.Instance.SwitchToBright());
        
        bool seq7Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq7IbuMarahLagi", 
            () => { seq7Complete = true; });
        yield return new WaitUntil(() => seq7Complete);
        
        yield return new WaitForSeconds(1f);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(SetCameraPanRangeBack());
        SetCharacterSpawn(CharacterType.Baby, 1);
        SetCharacterSpawn(CharacterType.Mother, 1);

        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby)); 
        yield return new WaitForSeconds(2f);
        FadeOpenEyes(); 
        
        bool seq8Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq8Stres", 
            () => { seq8Complete = true; });
        yield return new WaitUntil(() => seq8Complete);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(3, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 1.0f; 
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 2);
        SetCharacterSpawn(CharacterType.Ghost, 0);

        yield return new WaitForSeconds(1f);
        
        // PlayAudio("wind_light");
        bool seq9_1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/SFX/Seq9GangguanJendela", 
            () => { seq9_1Complete = true; });
        yield return new WaitUntil(() => seq9_1Complete);


        yield return new WaitForSeconds(1f);
        // PlayAudio("Ketukan Jendela");
        
        yield return new WaitForSeconds(2f);
        FadeOpenEyes(); 
 
        SetRaycastContext("Day4", "Night");
        
        this.EnableRaycastInteraction();

        bool correctInteraction = false;
        while (!correctInteraction)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) => {
                
                if (characterIdentity == "Window") 
                {
                    correctInteraction = true;
                }
                else if (characterIdentity == "Environment")
                {
                    correctInteraction = false;
                }
            }, "Day4", "Night"));
            
            if (!correctInteraction)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        this.DisableRaycastInteraction();
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(MoveCharacterToPosition(CharacterType.Ghost, 0, 0.5f));
        bool seq9Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq9GangguanSetanRingan", 
            () => { seq9Complete = true; });
        yield return new WaitUntil(() => seq9Complete);
        
        yield return new WaitForSeconds(1f);
        
        
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));

        StartCoroutine(SwitchLights.Instance.SwitchToBright());
         
        bool seq10Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day4/Seq10Maaf", 
            () => { seq10Complete = true; });
        yield return new WaitUntil(() => seq10Complete);
        
        // if (audioSource != null && audioSource.isPlaying)
        // {
        //     StartCoroutine(FadeOutAudio(audioSource, 3f)); 
        // }
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextDay();
    }
}
