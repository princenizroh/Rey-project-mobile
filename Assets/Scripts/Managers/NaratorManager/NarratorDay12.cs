using UnityEngine;
using System.Collections;

public class NarratorDay12 : NarratorBase
{
    [Header("Choice UI Elements - Assign in Inspector")]
    [SerializeField] private Animator animator;
    [SerializeField] private Animator animator2;
    [SerializeField] private Animator animator3;
    [SerializeField] private Animator animator4;
    
    private string selectedPlayerChoice = "";
    private bool choiceReceived = false;

    [System.Obsolete]
    protected override IEnumerator PlayAfternoonSequence()
    {
        saveFileManager.UpdateCoreGameSaves(11, 1);
        saveFileManager.SaveToLocalMyGamesFolder();

        AppearObjects();
        TimeManager.instance.TimeOfDay = 13.0f;
        SetCharacterSpawn(CharacterType.Baby, 0);
        SetCharacterSpawn(CharacterType.Mother, 0);
        SetCharacterSpawn(CharacterType.Father, 0);
        PlayCharacterAnimation(CharacterType.Mother, "Sitting Idle");
        yield return new WaitForSeconds(1f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Day 12\nKekacauan";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);

        FadeOpenEyes();

        // PlayCharacterAnimation(CharacterType.Object, "OpenTheDoor");
        yield return new WaitForSeconds(1f);

        bool seq1Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day12/Seq1KepulanganAyah",
            () => { seq1Complete = true; });
        yield return new WaitUntil(() => seq1Complete);

        yield return new WaitForSeconds(1f);

        SetRaycastContext("Day12", "Afternoon");

        this.EnableRaycastInteraction();

        bool correctInteraction = false;
        while (!correctInteraction)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) =>
            {

                if (characterIdentity == "Object")
                {
                    correctInteraction = true;
                }
            }, "Day12", "Afternoon"));

            if (!correctInteraction)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        this.DisableRaycastInteraction();

        animator.Play("OpenTheDoor");

        yield return new WaitForSeconds(1f);

        bool seq2Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day12/Seq2Berantakan",
            () => { seq2Complete = true; });
        yield return new WaitUntil(() => seq2Complete);

        CoreGameManager.OnPlayerChoiceSelected += OnPlayerChoiceReceived;
        choiceReceived = false;

        bool seq3ABComplete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day12/Seq3A-B",
            () => { seq3ABComplete = true; });
        yield return new WaitUntil(() => seq3ABComplete);

        CoreGameManager.OnPlayerChoiceSelected -= OnPlayerChoiceReceived;

        yield return StartCoroutine(HandlePlayerChoiceAndPlayTimeline());
        yield return StartCoroutine(ReturnToMainMenuAfterEnding());

        // Komen bawah ini untuk multiple ending nantinya
        // yield return new WaitForSeconds(2f);

        // GoToNextTimeOfDay();
    }
    
    [System.Obsolete]
    private IEnumerator HandlePlayerChoiceAndPlayTimeline()
    {
        yield return new WaitForSeconds(0.5f);
        
        string selectedChoice = GetPlayerChoice();

        if (selectedChoice == "Marah" || selectedChoice.Contains("Marah"))
        {
            yield return StartCoroutine(PlayAngryTimeline());
        }
        else if (selectedChoice == "Kawatir" || selectedChoice.Contains("Kawatir"))
        {
            yield return StartCoroutine(PlayConcernedTimeline());
        }
        else
        {
            yield return StartCoroutine(PlayConcernedTimeline());
        }
    }
    
    private string GetPlayerChoice()
    {
        if (choiceReceived && !string.IsNullOrEmpty(selectedPlayerChoice))
        {
            return selectedPlayerChoice;
        }
        
        if (PlayerPrefs.HasKey("LastPlayerChoice"))
        {
            return PlayerPrefs.GetString("LastPlayerChoice");
        }
        
        return "Kawatir";
    }
    
    private void OnPlayerChoiceReceived(string choice)
    {
        selectedPlayerChoice = choice;
        choiceReceived = true;
        Debug.Log($"NarratorDay12: Received player choice: {choice}");
    }
    
    protected override void Start()
    {
        Animator animator = GetComponent<Animator>();
    }
    
    private void OnDestroy()
    {
        if (CoreGameManager.OnPlayerChoiceSelected != null)
        {
            CoreGameManager.OnPlayerChoiceSelected -= OnPlayerChoiceReceived;
        }
    }

    /// <summary>
    /// Simple method to return to main menu after ending
    /// </summary>
    private IEnumerator ReturnToMainMenuAfterEnding()
    {
        yield return new WaitForSeconds(1f);
        ReturnToMainMenu();
    }


    [System.Obsolete]
    private IEnumerator PlayAngryTimeline()
    {
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day12/Seq3AKemarahanAyah",
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);

        SetRaycastContext("Day12", "Afternoon-1");

        this.EnableRaycastInteraction();

        bool correctInteraction = false;
        while (!correctInteraction)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) =>
            {

                if (characterIdentity == "Object-Dapur")
                {
                    correctInteraction = true;
                }
            }, "Day12", "Afternoon-1"));

            if (!correctInteraction)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        this.DisableRaycastInteraction();

        animator2.Play("OpenTheDoor");

        SetRaycastContext("Day12", "Afternoon-2");
        this.EnableRaycastInteraction();

        bool correctInteraction_2 = false;
        while (!correctInteraction_2)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) =>
            {

                if (characterIdentity == "Object-KamarOrtu")
                {
                    correctInteraction_2 = true;
                }
            }, "Day12", "Afternoon-2"));

            if (!correctInteraction_2)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        this.DisableRaycastInteraction();
        animator3.Play("OpenTheDoor");
        yield return new WaitForSeconds(1f);

        bool seq4Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day12/Seq4AMencariIbu",
            () => { seq4Complete = true; });
        yield return new WaitUntil(() => seq4Complete);


        SetRaycastContext("Day12", "Afternoon-3");
        this.EnableRaycastInteraction();

        bool correctInteraction_3 = false;
        while (!correctInteraction_3)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) =>
            {

                if (characterIdentity == "Object-KamarRey")
                {
                    correctInteraction_3 = true;
                }
            }, "Day12", "Afternoon-3"));

            if (!correctInteraction_3)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        this.DisableRaycastInteraction();
        animator4.Play("OpenTheDoor");
        yield return new WaitForSeconds(1f);

        bool seq5Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day12/Seq5AMenemukanIbu",
            () => { seq5Complete = true; });
        yield return new WaitUntil(() => seq5Complete);
        yield return new WaitForSeconds(2f);

        FadeCloseEyes(); 
        yield return new WaitForSeconds(2f);

        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Bad Ending\n Memarahi istri disaat kondisi mental yang tidak baik baik saja";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);
    }

    [System.Obsolete]
    private IEnumerator PlayConcernedTimeline()
    {
        bool seq3Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day12/Seq3BKhawatir",
            () => { seq3Complete = true; });
        yield return new WaitUntil(() => seq3Complete);

        SetRaycastContext("Day12-1", "Afternoon-1v1");

        this.EnableRaycastInteraction();

        bool correctInteraction = false;
        while (!correctInteraction)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) =>
            {

                if (characterIdentity == "Object-Dapur")
                {
                    correctInteraction = true;
                }
            }, "Day12-1", "Afternoon-1v1"));

            if (!correctInteraction)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        this.DisableRaycastInteraction();

        animator2.Play("OpenTheDoor");

        SetRaycastContext("Day12-2", "Afternoon-2v1");
        this.EnableRaycastInteraction();

        bool correctInteraction_2 = false;
        while (!correctInteraction_2)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) =>
            {

                if (characterIdentity == "Object-KamarOrtu")
                {
                    correctInteraction_2 = true;
                }
            }, "Day12-2", "Afternoon-2v1"));

            if (!correctInteraction_2)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        this.DisableRaycastInteraction();
        animator3.Play("OpenTheDoor");

        yield return new WaitForSeconds(1f);

        bool seq4Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day12/Seq4BMencariIbu",
            () => { seq4Complete = true; });
        yield return new WaitUntil(() => seq4Complete);

        SetRaycastContext("Day12-3", "Afternoon-3v1");
        this.EnableRaycastInteraction();

        bool correctInteraction_3 = false;
        while (!correctInteraction_3)
        {
            yield return StartCoroutine(WaitForRaycastInteraction((characterIdentity) =>
            {

                if (characterIdentity == "Object-KamarRey")
                {
                    correctInteraction_3 = true;
                }
            }, "Day12-3", "Afternoon-3v1"));

            if (!correctInteraction_3)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        this.DisableRaycastInteraction();
        animator4.Play("OpenTheDoor");
        yield return new WaitForSeconds(1f);

        bool seq5Complete = false;
        dialogGameManager.StartCoreGame("GameData/Dialog/Day12/Seq5BMenemukanIbu",
            () => { seq5Complete = true; });
        yield return new WaitUntil(() => seq5Complete);

        FadeCloseEyes();
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(true);
        uiElements.narratorText.text = "Good Ending\n MengKhawatirkan istri disaat kondisi mental yang tidak baik baik saja";
        yield return new WaitForSeconds(2f);
        uiElements.narratorText.gameObject.SetActive(false);
        
    }
}
