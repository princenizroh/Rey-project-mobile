using UnityEngine;

[System.Serializable]
public class CoreGameBlock
{
    public enum CoreType { Dialog, Cutscene }
    public CoreType Type;
    public CoreGameDialog Dialog;
    public CoreGameAnimation Animation;
}
