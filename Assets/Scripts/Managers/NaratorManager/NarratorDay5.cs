using UnityEngine;
using System.Collections;

public class NarratorDay5 : NarratorBase
{
    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(4, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 13.0f; 
        SetCharacterSpawn(CharacterType.Baby, 0);   
        SetCharacterSpawn(CharacterType.Mother, 0); 
        
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 5\nIbu Marah Besar";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);

        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day5/Seq1Lapar", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));
        
        StartCoroutine(SwitchLights.Instance.SwitchToBright());

        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby)); 
        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day5/Seq2IbuMarah", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        
        yield return new WaitForSeconds(1f);
        
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day5/Seq3Stres", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);
        
        yield return new WaitForSeconds(1f);

        FadeCloseEyes();
        yield return new WaitForSeconds(2f);
        
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayEveningSequence()
    {
        saveFileManager.UpdateCoreGameSaves(4, 2);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 18.0f; 
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 1);
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        
        StartCoroutine(SwitchLights.Instance.SwitchToBright());
        bool seq4Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day5/Seq4Penyesalan", 
            () => { seq4Complete = true; });
        yield return new WaitUntil(() => seq4Complete);
        
        FadeOpenEyes();

        yield return new WaitForSeconds(2f); 
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(4, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeLeft());
        TimeManager.instance.TimeOfDay = 1.0f; 
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 2);
        SetCharacterSpawn(CharacterType.Ghost, 0);
        
        yield return new WaitForSeconds(1f);
        // PlayAudio("rain_heavy");
        // PlayAudio("wind_strong");
        bool seq5_1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day5/SFX/Seq5KetukanJendela", 
            () => { seq5_1Complete = true; });
        yield return new WaitUntil(() => seq5_1Complete);
        
        yield return new WaitForSeconds(2f);
        FadeOpenEyes(); 
        
        SetRaycastContext("Day5", "Night");
        
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
            }, "Day5", "Night"));
            
            if (!correctInteraction)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        this.DisableRaycastInteraction();
        yield return new WaitForSeconds(1f);
         
        SetCharacterSpawn(CharacterType.Ghost, 1);

        yield return new WaitForSeconds(1f);

        SetCharacterSpawn(CharacterType.Ghost, 0);
        
        bool seq5Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day5/Seq5GangguanSetanHujan", 
            () => { seq5Complete = true; });
        yield return new WaitUntil(() => seq5Complete);

        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));

        StartCoroutine(SwitchLights.Instance.SwitchToBright());

        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));

        bool seq6Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day5/Seq6Muak", 
            () => { seq6Complete = true; });
        yield return new WaitUntil(() => seq6Complete);

        
        // if (audioSource != null && audioSource.isPlaying)
        // {
        //     StartCoroutine(FadeOutAudio(audioSource, 3f)); 
        // }
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextDay();
    }
}
