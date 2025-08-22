using UnityEngine;
[System.Serializable]
public class CoreGameDialog
{
    public enum DialogType { ThreeD, TwoD }
    public enum CamChoices { Default_Engine, Rey, Mother, Father }
    public enum Dialog3DLocation { Rey, Mother, Father }
    public enum CutsceneType { None, FadeIn, FadeOut, StayIn, StayOut }
    public CutsceneType cutsceneType;
    public DialogType dialogType;
    public Dialog3DLocation dialog3DLocation;
    public CamChoices camChoice;
    public enum NpcName { Rey, Ibu, Ayah, Bidan, Hantu, None, Penelpon }
    public NpcName npcName;
    [TextArea(4, 5)]
    public string dialogEntry;
    public AudioClip audioDialogEntry;
    public CoreGameDialogChoices[] choices;
}
