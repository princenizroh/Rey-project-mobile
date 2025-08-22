
using UnityEngine;
using System.Collections;

public class NarratorHelper : NarratorBase
{
    [Header("Position Setup Tools")]
    private Vector3[] lastSpawnPositions;
    private Quaternion[] lastSpawnRotations;

    // [System.Obsolete]
    // protected override IEnumerator PlayMorningSequence()
    // {    
    //     Debug.Log("Playing narration for Day 2 Morning sequence.");
    //     PlayCharacterAnimation(CharacterType.Mother, "Sit");
    //     PlayCharacterAnimation(CharacterType.Father, "Sit");
    //     yield return new WaitForSeconds(5f);
    //
    //     yield return StartCoroutine(MoveCharacterToPosition(CharacterType.Baby, 0, 2f));
    //     yield return new WaitForSeconds(5f);
    //
    //     yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Bidan, 0));
    // }
    // 
    // [System.Obsolete]
    // protected override IEnumerator PlayAfternoonSequence()
    // {
    //     TimeManager.instance.TimeOfDay = 13.0f;
    //     PlayCharacterAnimation(CharacterType.Mother, "Sit");
    //     Debug.Log("Playing narration for Day 2 Afternoon sequence.");
    //     yield return null;
    // }
    // 
    // [System.Obsolete]
    // protected override IEnumerator PlayEveningSequence()
    // {
    //     TimeManager.instance.TimeOfDay = 19.0f;
    //     PlayCharacterAnimation(CharacterType.Mother, "Sit");
    //     Debug.Log("Playing narration for Day 2 Evening sequence.");
    //     yield return null;
    // }
    // 
    // [System.Obsolete]
    // protected override IEnumerator PlayNightSequence()
    // {
    //     TimeManager.instance.TimeOfDay = 1.0f;
    //     PlayCharacterAnimation(CharacterType.Mother, "Sit");
    //     Debug.Log("Playing narration for Day 2 Night sequence.");
    //     yield return null;
    // }

    public void SnapCharacterToSpawn(CharacterType characterType, int spawnIndex)
    {
        #if UNITY_EDITOR
        var characterData = System.Array.Find(charactersDataArray, c => c.characterType == characterType);
        if (characterData != null && characterData.HasValidSpawnPosition(spawnIndex))
        {
            characterData.characterObject.transform.position = characterData.spawnPositions[spawnIndex].position;
            characterData.characterObject.transform.rotation = characterData.spawnPositions[spawnIndex].rotation;
            
            Debug.Log($"Snapped {characterType} to spawn position {spawnIndex}");
        }
        else
        {
            Debug.LogWarning($"Cannot snap {characterType} to spawn position {spawnIndex}. Check if spawn position exists.");
        }
        #endif
    }
    
    public void SnapAllCharactersToSpawn(int spawnIndex)
    {
        #if UNITY_EDITOR
        foreach (var characterData in charactersDataArray)
        {
            if (characterData.HasValidSpawnPosition(spawnIndex))
            {
                SnapCharacterToSpawn(characterData.characterType, spawnIndex);
            }
        }
        #endif
    }
    
    public void SnapCharactersToMultipleSpawns(int motherIndex, int fatherIndex, int babyIndex, int bidanIndex)
    {
        #if UNITY_EDITOR
        SnapCharacterToSpawn(CharacterType.Mother, motherIndex);
        SnapCharacterToSpawn(CharacterType.Father, fatherIndex);
        SnapCharacterToSpawn(CharacterType.Baby, babyIndex);
        SnapCharacterToSpawn(CharacterType.Bidan, bidanIndex);
        
        Debug.Log($"Snapped characters to multiple positions - Mother:{motherIndex}, Father:{fatherIndex}, Baby:{babyIndex}, Bidan:{bidanIndex}");
        #endif
    }
    
    
    public void SetupMorningPositions()
    {
        #if UNITY_EDITOR
        SnapCharacterToSpawn(CharacterType.Mother, 0); 
        SnapCharacterToSpawn(CharacterType.Father, 0); 
        SnapCharacterToSpawn(CharacterType.Baby, 0);   
        SnapCharacterToSpawn(CharacterType.Bidan, 1);  
        
        Debug.Log("Morning positions setup complete!");
        #endif
    }
    
    public void SetupAfternoonPositions()
    {
        #if UNITY_EDITOR
        // Afternoon specific positions
        SnapCharacterToSpawn(CharacterType.Mother, 1); 
        SnapCharacterToSpawn(CharacterType.Father, 2); 
        SnapCharacterToSpawn(CharacterType.Baby, 1);   
        SnapCharacterToSpawn(CharacterType.Bidan, 3);  
        
        Debug.Log("Afternoon positions setup complete!");
        #endif
    }
    
    public void SetupEveningPositions()
    {
        #if UNITY_EDITOR
        SnapCharacterToSpawn(CharacterType.Mother, 2); 
        SnapCharacterToSpawn(CharacterType.Father, 2); 
        SnapCharacterToSpawn(CharacterType.Baby, 2);   
        SnapCharacterToSpawn(CharacterType.Bidan, 4);  
        
        Debug.Log("Evening positions setup complete!");
        #endif
    }
    
    public void ResetAllCharacterPositions()
    {
        #if UNITY_EDITOR
        foreach (var characterData in charactersDataArray)
        {
            if (characterData.HasValidSpawnPosition(0))
            {
                SnapCharacterToSpawn(characterData.characterType, 0);
            }
        }
        Debug.Log("All characters reset to default positions (spawn index 0)");
        #endif
    }
    
    public int GetMaxSpawnIndex(CharacterType characterType)
    {
        #if UNITY_EDITOR
        var characterData = System.Array.Find(charactersDataArray, c => c.characterType == characterType);
        if (characterData != null && characterData.spawnPositions != null)
        {
            return characterData.spawnPositions.Length - 1;
        }
        return 0;
        #else
        return 0;
        #endif
    }
    
    public bool ValidateSpawnPositions(int requiredPositions)
    {
        #if UNITY_EDITOR
        foreach (var characterData in charactersDataArray)
        {
            if (characterData.spawnPositions == null || characterData.spawnPositions.Length < requiredPositions)
            {
                Debug.LogWarning($"Character {characterData.characterType} doesn't have {requiredPositions} spawn positions!");
                return false;
            }
        }
        return true;
        #else
        return false;
        #endif
    }
}
