using UnityEngine;
[System.Serializable]
public class CoreGameDialogChoices
{
    public string playerChoice;
    public bool correctChoice;
    public CoreGameDialogChoicesResponse[] dialogResponses;
    public AudioClip audioDialogResponse;
}
