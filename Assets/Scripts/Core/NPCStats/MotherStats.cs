using System;
using TMPro;
using UnityEngine;

public class MotherStats : MonoBehaviour
{
    private GameObject stressBar;
    private TMP_Text stressIndicator;
    private int stressLevel;
    private int stressHealth;

    void Start()
    {
        stressBar = GameObject.Find("StressBar");
        if (stressBar == null)
        {
            Debug.LogError("StressBar GameObject not found!");
            return;
        }

        stressIndicator = stressBar.GetComponentInChildren<TMP_Text>();
        if (stressIndicator == null)
        {
            Debug.LogError("TMP_Text component not found in StressBar!");
            return;
        }
    }

    public void increaseStress(int amount)
    {
        stressLevel += amount;
    }

    public void decreaseStress(int amount)
    {
        stressLevel -= amount;
    }

    public (string StressLevel, string StressHealth) getStressData()
    {
        return (stressLevel.ToString(), stressHealth.ToString());
    }

    void Update()
    {
        if (stressIndicator != null)
        {
            stressIndicator.text = "Stress Level: " + stressLevel;
        }
    }
}