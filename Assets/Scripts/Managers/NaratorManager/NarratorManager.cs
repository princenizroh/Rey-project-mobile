using UnityEngine;
using System.Collections.Generic;

public class NarratorManager : MonoBehaviour
{
    public static NarratorManager Instance;
    public NarratorDay currentDay;
    public TimeOfDay currentTime;
    public CoreGameSaves coreGameSaves;

    [Header("Narrators")]
    [SerializeField] private NarratorBase[] dayNarrators;
    private Dictionary<NarratorDay, NarratorBase> narratorDict;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeNarrators();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeNarrators()
    {
        narratorDict = new Dictionary<NarratorDay, NarratorBase>();

        for (int i = 0; i < dayNarrators.Length; i++)
        {
            if (dayNarrators[i] != null)
            {
                // Handle all narrator types including MainMenu and Helper
                if (i < 15) // Day1-Day14 + Helper
                {
                    narratorDict[(NarratorDay)i] = dayNarrators[i];
                }
                else if (i == 15) // DayMainMenu
                {
                    narratorDict[NarratorDay.DayMainMenu] = dayNarrators[i];
                }
            }
        }
    }

    [System.Obsolete]
    public void Start()
    {
        currentDay = (NarratorDay)coreGameSaves.day;
        currentTime = (TimeOfDay)coreGameSaves.timeOfDay;

        Debug.Log("its time for" + currentTime);

        StartNarration(currentDay, currentTime);
    }

    [System.Obsolete]
    public void StartNarration(NarratorDay day, TimeOfDay time)
    {
        Debug.Log($"=== NarratorManager.StartNarration({day}, {time}) ===");
        currentDay = day;
        currentTime = time;

        if (narratorDict.TryGetValue(day, out NarratorBase narrator))
        {
            Debug.Log($"Found narrator for {day}: {narrator.name}");
            
            // Check if the requested time sequence is available
            if (narrator.HasTimeOfDaySequence(time))
            {
                Debug.Log($"Starting {day} {time} sequence");
                StartCoroutine(narrator.StartNarration());
            }
            else
            {
                Debug.LogWarning($"{day} does not have {time} sequence. Finding first available...");
                TimeOfDay firstAvailable = narrator.GetFirstAvailableTimeOfDay();
                currentTime = firstAvailable;
                Debug.Log($"Starting {day} with first available time: {firstAvailable}");
                StartCoroutine(narrator.StartNarration());
            }
        }
        else
        {
            Debug.LogError($"Narrator for {day} not found in dictionary!");
            Debug.Log($"Available narrators: {string.Join(", ", narratorDict.Keys)}");
        }
    }


    [System.Obsolete]
    public void ChangeNarrator(NarratorDay newDay, TimeOfDay newTime)
    {
        StartNarration(newDay, newTime);
    }

    [System.Obsolete]
    public void NextDay()
    {
        if ((int)currentDay < 13) 
        {
            NarratorDay nextDay = currentDay + 1;

            if (narratorDict.TryGetValue(nextDay, out NarratorBase nextNarrator))
            {
                TimeOfDay firstAvailableTime = nextNarrator.GetFirstAvailableTimeOfDay();
                StartNarration(nextDay, firstAvailableTime);
                Debug.Log($"Starting {nextDay} with first available time: {firstAvailableTime}");
            }
            else
            {
                Debug.LogError($"Narrator for {nextDay} not found!");
            }
        }
        else
        {
            Debug.Log("Story completed - reached final day!");
        }
    }
    
    [System.Obsolete]
    public void NextTimeOfDay()
    {
        if (narratorDict.TryGetValue(currentDay, out NarratorBase currentNarrator))
        {
            TimeOfDay nextTime = currentNarrator.GetNextAvailableTimeOfDay(currentTime);
            
            // If nextTime is Morning, it means no more sequences for current day
            if (nextTime == TimeOfDay.Morning && currentTime != TimeOfDay.Night)
            {
                // Skip to next day instead
                NextDay();
            }
            else if (nextTime == TimeOfDay.Morning && currentTime == TimeOfDay.Night)
            {
                // End of current day, go to next day
                NextDay();
            }
            else
            {
                // Stay on current day, move to next available time
                StartNarration(currentDay, nextTime);
                Debug.Log($"Moving to next available time: {nextTime} on {currentDay}");
            }
        }
        else
        {
            Debug.LogError($"Current narrator for {currentDay} not found!");
        }
    }

    /// <summary>
    /// Start Main Menu narrator (for main menu scenes)
    /// </summary>
    [System.Obsolete]
    public void StartMainMenu()
    {
        Debug.Log("=== Starting Main Menu ===");
        
        if (narratorDict.TryGetValue(NarratorDay.DayMainMenu, out NarratorBase mainMenuNarrator))
        {
            currentDay = NarratorDay.DayMainMenu;
            currentTime = TimeOfDay.Morning; // Default time for main menu
            
            // Use reflection to call PlayMainMenuSequence if it exists
            var playMethod = mainMenuNarrator.GetType().GetMethod("PlayMainMenuSequence");
            if (playMethod != null)
            {
                playMethod.Invoke(mainMenuNarrator, null);
                Debug.Log("Main Menu sequence started");
            }
            else
            {
                Debug.LogError("PlayMainMenuSequence method not found in MainMenu narrator!");
            }
        }
        else
        {
            Debug.LogError("MainMenu narrator not found! Make sure to assign NarratorMainMenu in the inspector.");
        }
    }

    /// <summary>
    /// Refresh main menu (useful after save changes)
    /// </summary>
    [System.Obsolete]
    public void RefreshMainMenu()
    {
        if (currentDay == NarratorDay.DayMainMenu)
        {
            StartMainMenu();
        }
    }
}
