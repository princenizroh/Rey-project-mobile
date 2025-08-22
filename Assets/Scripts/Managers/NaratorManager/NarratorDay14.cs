using UnityEngine;
using System.Collections;

public class NarratorDay14 : NarratorBase
{
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(13, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return null;
    }
}
