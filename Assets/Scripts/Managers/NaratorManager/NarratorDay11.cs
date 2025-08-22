using UnityEngine;
using System.Collections;

public class NarratorDay11 : NarratorBase
{
    [Header("Charge Meter")]
    public GameObject chargeMeterObject;
    
    [SerializeField] protected Rigidbody rigidbodyIbu;

    protected override void Awake()
    {
        base.Awake(); 
        
        InitializeRigidbodyIbu();
    }
    
    private void InitializeRigidbodyIbu()
    {
        if (rigidbodyIbu == null)
        {
            foreach (var characterData in charactersDataArray)
            {
                if (characterData.characterType == CharacterType.Mother)
                {
                    rigidbodyIbu = characterData.characterObject.GetComponent<Rigidbody>();
                    if (rigidbodyIbu == null)
                    {
                        rigidbodyIbu = characterData.characterObject.AddComponent<Rigidbody>();
                    }
                    break;
                }
            }
        }
    }
    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(10, 1);
        saveFileManager.SaveToLocalMyGamesFolder();
        DisableNavMeshAgent(CharacterType.Mother);
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 13.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        SetFreezePosition(true); 
        
        PlayCharacterAnimation(CharacterType.Mother, "Sitting_Sexy");
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 11\nKehilangan";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);

        FadeOpenEyes(); 
        yield return new WaitForSeconds(1f);

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day11/Seq1BerbicaraAneh", 
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);
        
        // ChargeMeter untuk "menangis makin keras" - Seq1 BerbicaraAneh  
        yield return StartCoroutine(PlayChargeMeterSequence(chargeMeterObject));
        
        yield return new WaitForSeconds(1f);
        
        SetFreezePosition(false); 
        EnableNavMeshAgent(CharacterType.Mother);
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));

        yield return StartCoroutine(MoveAgentToMovementPosition(CharacterType.Mother, 0));
        
        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day11/Seq2Selamat", 
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);
        
        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);
        
        GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(10, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return StartCoroutine(SetCameraPanRangeBack());
        TimeManager.instance.TimeOfDay = 1.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        
        // Mother looks at baby with concern about hunger and survival
        StartCoroutine(SetHeadTarget(CharacterType.Mother, CharacterTarget.Baby));
        
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day11/Seq3Kelaparan", 
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);
        
        yield return new WaitForSeconds(2f);
        
        GoToNextDay();
    }

    public void SetFreezePosition(bool freeze)
    {
        // Ensure rigidbody is initialized
        if (rigidbodyIbu == null)
        {
            InitializeRigidbodyIbu();
        }
        
        if (rigidbodyIbu == null) 
        {
            Debug.LogError("Cannot set freeze position: rigidbodyIbu is still null!");
            return;
        }

        if (freeze)
        {
            Debug.Log("Freezing Mother's position");
            rigidbodyIbu.constraints |= RigidbodyConstraints.FreezePositionX |
                              RigidbodyConstraints.FreezePositionY |
                              RigidbodyConstraints.FreezePositionZ;
        }
        else
        {
            Debug.Log("Unfreezing Mother's position");
            rigidbodyIbu.constraints &= ~RigidbodyConstraints.FreezePositionX;
            rigidbodyIbu.constraints &= ~RigidbodyConstraints.FreezePositionY;
            rigidbodyIbu.constraints &= ~RigidbodyConstraints.FreezePositionZ;
        }
    }
}
