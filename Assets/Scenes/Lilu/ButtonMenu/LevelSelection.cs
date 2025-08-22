using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Make sure to include this if you're working with UI buttons

public class LevelSelection : MonoBehaviour
{
    public Button[] lvlButtons; // Assuming you have an array of Button objects

    // Start is called before the first frame update
    void Start()
    {
        int levelAt = PlayerPrefs.GetInt("levelAt", 2);
        for (int i = 0; i < lvlButtons.Length; i++)
        {
            if (i + 2 > levelAt)
                lvlButtons[i].interactable = false; // Corrected line
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
