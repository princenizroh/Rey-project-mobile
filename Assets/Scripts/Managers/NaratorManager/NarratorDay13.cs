using UnityEngine;
using System.Collections;

public class NarratorDay13 : NarratorBase
{
    [System.Obsolete]
    protected override IEnumerator PlayNightSequence()
    {
        saveFileManager.UpdateCoreGameSaves(12, 3);
        saveFileManager.SaveToLocalMyGamesFolder();
        
        yield return null;
    }
}
