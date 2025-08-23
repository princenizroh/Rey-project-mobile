using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.AI;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public enum NarratorDay
{
    Day1, Day2, Day3, Day4, Day5, Day6, Day7, Day8, Day9, Day10, Day11, Day12, Day13, Day14, Helper, DayMainMenu
}

public enum TimeOfDay
{
    Morning, Afternoon, Evening, Night
}

public enum CharacterType
{
    Mother, Father, Bidan, Baby, Object, Ghost
}

public enum CharacterTarget
{
    Mother, Father, Bidan, Baby, Object, Ghost
}

[System.Serializable]
public class AudioClipData
{
    public string clipName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;
}

[System.Serializable]
public class CharacterData
{
    [Header("Character Info")]
    public CharacterType characterType;
    public GameObject characterObject;
    
    [Header("Positions")]
    public Transform[] spawnPositions;
    public Transform[] movementPositions;
    
    [System.NonSerialized] public NavMeshAgent agent;
    [System.NonSerialized] public Animator animator;
    
    public void Initialize()
    {
        if (characterObject != null)
        {
            agent = characterObject.GetComponent<NavMeshAgent>();
            animator = characterObject.GetComponentInChildren<Animator>();
        }
    }
    
    public bool HasValidSpawnPosition(int index)
    {
        return spawnPositions != null && 
               index >= 0 && 
               index < spawnPositions.Length && 
               spawnPositions[index] != null;
    }
    
    public bool HasValidMovementPosition(int index)
    {
        return movementPositions != null && 
               index >= 0 && 
               index < movementPositions.Length && 
               movementPositions[index] != null;
    }
}

[System.Serializable]
public class UIElements
{
    public TextMeshProUGUI narratorText;
    public Image backgroundImage;
    public CanvasGroup canvasGroup;
    public GameObject cursor;
}

[System.Serializable]
public class GameObjects
{
    [Header("Day 1 Setup")]
    public GameObject[] activeObjects;
    public GameObject[] inActiveObjects;    
}

public abstract class NarratorBase : MonoBehaviour
{
    [Header("Camera Control")]
    [SerializeField] protected CinemachineCamera cinemachineCamera;
    [SerializeField] protected RaycastObjectCam rayCastObject;
    [Header("UI Elements")]
    [SerializeField] protected UIElements uiElements;

    [Header("Game Objects")]
    [SerializeField] protected GameObjects gameObjects;

    [Header("Core Manager")]
    [SerializeField] protected CoreGameManager dialogGameManager;

    protected SaveFileManager saveFileManager;

    protected HeadTrackingManager headTrackingManager;

    [Header("Characters")]
    [SerializeField] protected CharacterData[] charactersDataArray;

    [Header("Audio Clips")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioClipData[] audioClips;

    private Dictionary<string, AudioClipData> audioDict;
    private Dictionary<CharacterType, CharacterData> characterDict;

    #region Unity Lifecycle 
    protected virtual void Awake()
    {
        InitializeAudioSystem();
        InitializeCharacterSystem();
        InitializeSaveFileManager();
        InitializeHeadTrackingManager();
        InitializeRaycastCamera();
    }

    protected virtual void Start()
    {
        InitializeCharacterComponents();
    }
    #endregion
    #region Initialization
    
    /// <summary>
    /// Replacement untuk Input.GetKeyDown(KeyCode.E) - now uses BaseInputHandler static method
    /// Also includes touch interaction for mobile support
    /// </summary>
    protected bool GetInteractionKeyDown()
    {
        // BaseInputHandler works on all platforms
        bool eKeyPressed = BaseInputHandler.InteractionKeyDown;
        
        // Mouse click (Editor/PC)
        #if UNITY_EDITOR
        bool mousePressed = Input.GetMouseButtonDown(0);
        #else
        bool mousePressed = false;
        #endif
        
        // Touch input (Mobile)
        bool touchPressed = false;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchPressed = true;
            }
        }
        
        return eKeyPressed || mousePressed || touchPressed;
    }

    private void InitializeAudioSystem()
    {
        audioDict = new Dictionary<string, AudioClipData>();
        foreach (var clipData in audioClips)
        {
            if (!string.IsNullOrEmpty(clipData.clipName))
            {
                audioDict[clipData.clipName] = clipData;
            }
        }
    }
    private void InitializeCharacterSystem()
    {
        characterDict = new Dictionary<CharacterType, CharacterData>();

        foreach (var characterData in charactersDataArray)
        {
            if (characterData != null)
            {
                characterDict[characterData.characterType] = characterData;
            }
        }
    }

    private void InitializeSaveFileManager()
    {
        if (saveFileManager == null)
        {
            saveFileManager = FindFirstObjectByType<SaveFileManager>();
        }
    }

    private void InitializeHeadTrackingManager()
    {
        if (headTrackingManager == null)
        {
            headTrackingManager = FindFirstObjectByType<HeadTrackingManager>();
        }
    }

    private void InitializeCharacterComponents()
    {
        foreach (var characterData in charactersDataArray)
        {
            characterData.Initialize();
        }
    }

    private void InitializeRaycastCamera()
    {
        if (rayCastObject == null)
        {
            rayCastObject = FindFirstObjectByType<RaycastObjectCam>();
        }
    }
    #endregion
    #region Sequence Detection
    public virtual TimeOfDay GetFirstAvailableTimeOfDay()
    {
        System.Type thisType = this.GetType();

        var morningMethod = thisType.GetMethod("PlayMorningSequence",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (morningMethod != null && morningMethod.DeclaringType != typeof(NarratorBase))
        {
            return TimeOfDay.Morning;
        }

        var afternoonMethod = thisType.GetMethod("PlayAfternoonSequence",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (afternoonMethod != null && afternoonMethod.DeclaringType != typeof(NarratorBase))
        {
            return TimeOfDay.Afternoon;
        }

        var eveningMethod = thisType.GetMethod("PlayEveningSequence",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (eveningMethod != null && eveningMethod.DeclaringType != typeof(NarratorBase))
        {
            return TimeOfDay.Evening;
        }

        return TimeOfDay.Night;
    }

    public virtual TimeOfDay GetNextAvailableTimeOfDay(TimeOfDay currentTime)
    {
        System.Type thisType = this.GetType();

        for (int i = (int)currentTime + 1; i <= (int)TimeOfDay.Night; i++)
        {
            TimeOfDay checkTime = (TimeOfDay)i;
            string methodName = $"Play{checkTime}Sequence";

            var method = thisType.GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null && method.DeclaringType != typeof(NarratorBase))
            {
                return checkTime;
            }
        }

        return TimeOfDay.Morning;
    }

    public virtual bool HasTimeOfDaySequence(TimeOfDay timeOfDay)
    {
        System.Type thisType = this.GetType();
        string methodName = $"Play{timeOfDay}Sequence";

        var method = thisType.GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return method != null && method.DeclaringType != typeof(NarratorBase);
    }
    #endregion
    #region Abstract Methods
    [System.Obsolete]
    public IEnumerator StartNarration()
    {
        yield return StartCoroutine(Narrate());
    }

    [System.Obsolete]
    protected virtual IEnumerator Narrate()
    {
        ResetUIState();
        
        yield return StartCoroutine(ResetHeadTracking());
        yield return StartCoroutine(TransitionScene());

        TimeOfDay targetTime = NarratorManager.Instance.currentTime;

        if (!HasTimeOfDaySequence(targetTime))
        {
            Debug.LogWarning($"{this.GetType().Name} does not have {targetTime}Sequence implemented. Finding next available sequence...");

            TimeOfDay nextAvailable = GetNextAvailableTimeOfDay(targetTime);
            if (nextAvailable != TimeOfDay.Morning || HasTimeOfDaySequence(TimeOfDay.Morning))
            {
                NarratorManager.Instance.currentTime = nextAvailable;
                targetTime = nextAvailable;
            }
            else
            {
                GoToNextDay();
                yield break;
            }
        }

        switch (targetTime)
        {
            case TimeOfDay.Morning:
                yield return StartCoroutine(PlayMorningSequence());
                break;
            case TimeOfDay.Afternoon:
                yield return StartCoroutine(PlayAfternoonSequence());
                break;
            case TimeOfDay.Evening:
                yield return StartCoroutine(PlayEveningSequence());
                break;
            case TimeOfDay.Night:
                yield return StartCoroutine(PlayNightSequence());
                break;
        }
    }
    [System.Obsolete]
    protected virtual IEnumerator PlayMorningSequence()
    {
        yield return null;
    }

    [System.Obsolete]
    protected virtual IEnumerator PlayAfternoonSequence()
    {
        yield return null;
    }

    [System.Obsolete]
    protected virtual IEnumerator PlayEveningSequence()
    {
        yield return null;
    }

    [System.Obsolete]
    protected virtual IEnumerator PlayNightSequence()
    {
        yield return null;
    }
    #endregion

    #region UI Management
    protected void ResetUIState()
    {
    }
    protected void CloseEyes()
    {
        Color newColor = Color.black;
        newColor.a = 1f;
        uiElements.backgroundImage.color = newColor;
        uiElements.canvasGroup.alpha = 1f;
    }

    protected void FadeOpenEyes()
    {
        StartCoroutine(FadeEyesCoroutine(1f, 0f, 2f));
        uiElements.cursor.SetActive(true);
    }

    protected void FadeCloseEyes()
    {
        StartCoroutine(FadeEyesCoroutine(0f, 1f, 2f));
        uiElements.cursor.SetActive(false);
    }

    private IEnumerator FadeEyesCoroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color currentColor = uiElements.backgroundImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;

            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);

            currentColor.a = currentAlpha;
            uiElements.backgroundImage.color = currentColor;

            uiElements.canvasGroup.alpha = Mathf.Lerp(1f, 1f, normalizedTime);

            yield return null;
        }

        currentColor.a = endAlpha;
        uiElements.backgroundImage.color = currentColor;

    }
    #endregion
    #region Audio Management
    protected void PlayAudio(string clipName)
    {
        if (audioDict.ContainsKey(clipName))
        {
            var clipData = audioDict[clipName];
            audioSource.clip = clipData.audioClip;
            audioSource.volume = clipData.volume;
            audioSource.loop = clipData.loop;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"Audio clip '{clipName}' not found!");
        }
    }

    private void StopAudio()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator PlayAudioForDuration(string clipName, float duration)
    {
        PlayAudio(clipName);
        yield return new WaitForSeconds(duration);
        StopAudio();
    }

    protected IEnumerator FadeOutAudio(AudioSource audioSource, float fadeTime)
    {
        float startVolume = audioSource.volume;
        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = startVolume;
    }
    #endregion

    #region Character Management
    protected void SetCharacterSpawn(CharacterType characterType, int spawnIndex)
    {
        if (characterDict.TryGetValue(characterType, out CharacterData characterData))
        {
            if (characterData.HasValidSpawnPosition(spawnIndex))
            {
                SetCharacterPosition(characterData.characterObject, characterData.spawnPositions[spawnIndex]);
            }
        }
    }
    private void SetCharacterPosition(GameObject character, Transform targetTransform)
    {
        if (character == null || targetTransform == null) return;

        character.SetActive(false);
        character.transform.position = targetTransform.position;
        character.transform.rotation = targetTransform.rotation;
        character.SetActive(true);
    }

    protected void PlayCharacterAnimation(CharacterType characterType, string animationName)
    {
        if (!EnsureCharacterInitialized(characterType))
        {
            Debug.LogError($"Failed to initialize {characterType} for animation!");
            return;
        }

        if (characterDict.TryGetValue(characterType, out CharacterData characterData))
        {
            if (characterData.animator != null)
            {
                if (!characterData.characterObject.activeInHierarchy)
                {
                    Debug.LogWarning($"{characterType} GameObject is not active!");
                    return;
                }

                if (!characterData.animator.enabled)
                {
                    Debug.LogWarning($"{characterType} Animator is not enabled!");
                    return;
                }
                characterData.animator.Play(animationName);

            }
            else
            {
                Debug.LogError($"Animator for {characterType} is still null after initialization!");
            }
        }
        else
        {
            Debug.LogError($"Character data for {characterType} not found!");
        }
    }

    private bool EnsureCharacterInitialized(CharacterType characterType)
    {
        if (characterDict.TryGetValue(characterType, out CharacterData characterData))
        {
            if (characterData.animator == null || characterData.agent == null)
            {
                Debug.Log($"Re-initializing {characterType}");

                bool wasActive = characterData.characterObject.activeInHierarchy;
                if (!wasActive)
                {
                    characterData.characterObject.SetActive(true);
                }

                characterData.Initialize();

                if (!wasActive)
                {
                    characterData.characterObject.SetActive(wasActive);
                }
            }

            return characterData.animator != null;
        }

        return false;
    }

    protected IEnumerator MoveAgentToTarget(CharacterType characterType, Transform target)
    {
        if (characterDict.TryGetValue(characterType, out CharacterData character))
        {
            character.agent.SetDestination(target.position);

            while (character.agent.pathPending)
            {
                yield return null;
            }

            PlayCharacterAnimation(characterType, "Walk");

            while (character.agent.remainingDistance > character.agent.stoppingDistance)
            {
                yield return null;
            }

            PlayCharacterAnimation(characterType, "Idle");
        }
    }

    private IEnumerator MoveObjectToPosition(Transform obj, Transform targetTransform, float duration)
    {
        var startPos = obj.position;
        var targetPos = targetTransform.position;
        var startRot = obj.rotation;
        var targetRot = targetTransform.rotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            obj.position = Vector3.Lerp(startPos, targetPos, t);
            obj.rotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        obj.position = targetPos;
        obj.rotation = targetRot;
    }
    #endregion
    #region Movement Management
    protected IEnumerator MoveCharacterToPosition(CharacterType characterType, int positionIndex, float duration = 1f)
    {
        if (characterDict.TryGetValue(characterType, out CharacterData character))
        {
            if (!character.HasValidMovementPosition(positionIndex))
            {
                Debug.LogError($"Invalid movement position index {positionIndex} for {characterType}");
                yield break;
            }

            Transform targetTransform = character.movementPositions[positionIndex];
            yield return StartCoroutine(MoveObjectToPosition(character.characterObject.transform, targetTransform, duration));
        }
    }

    protected IEnumerator MoveAgentToMovementPosition(CharacterType characterType, int positionIndex)
    {
        if (characterDict.TryGetValue(characterType, out CharacterData character))
        {
            if (!character.HasValidMovementPosition(positionIndex))
            {
                yield break;
            }
            Transform target = character.movementPositions[positionIndex];
            yield return StartCoroutine(MoveAgentToTarget(characterType, target));
        }
    }

    protected void EnableNavMeshAgent(CharacterType characterType)
    {
        if (characterDict.TryGetValue(characterType, out CharacterData character))
        {
            if (character.agent != null)
            {
                character.agent.enabled = true;
            }
        }
    }

    protected void DisableNavMeshAgent(CharacterType characterType)
    {
        if (characterDict.TryGetValue(characterType, out CharacterData character))
        {
            if (character.agent != null)
            {
                character.agent.enabled = false;
            }
        }
    }
    #endregion
    #region GameObject Management
    protected void AppearObjects()
    {
        SetObjectsActive(gameObjects.activeObjects, true);
        SetObjectsActive(gameObjects.inActiveObjects, false);
    }

    protected void SetObjectsActive(GameObject[] objects, bool active)
    {
        foreach (var obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    protected GameObject SpawnChargeMeter(Canvas targetCanvas = null)
    {
        GameObject chargeMeterPrefab = Resources.Load<GameObject>("ChargeMeter");

        if (chargeMeterPrefab == null)
        {
            return null;
        }

        GameObject chargeMeterInstance;

        if (targetCanvas != null)
        {
            chargeMeterInstance = Instantiate(chargeMeterPrefab, targetCanvas.transform);

            RectTransform rectTransform = chargeMeterInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }

        }
        else
        {
            chargeMeterInstance = Instantiate(chargeMeterPrefab, Vector3.zero, Quaternion.identity);
        }

        return chargeMeterInstance;
    }

    protected GameObject SpawnChargeMeterByCanvasName(string canvasName)
    {
        Canvas targetCanvas = FindCanvasByName(canvasName);

        if (targetCanvas == null)
        {
            Debug.LogWarning($"Canvas with name '{canvasName}' not found! Spawning in world space instead.");
            return SpawnChargeMeter(null);
        }

        return SpawnChargeMeter(targetCanvas);
    }

    private Canvas FindCanvasByName(string canvasName)
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.Equals(canvasName, System.StringComparison.OrdinalIgnoreCase))
            {
                return canvas;
            }
        }

        return null;
    }

    /// <summary>
    /// Play charge meter sequence for "menangis makin keras" mechanic
    /// </summary>
    protected IEnumerator PlayChargeMeterSequence(GameObject chargeMeterObject)
    {
        if (chargeMeterObject == null)
        {
            Debug.LogWarning("[NarratorBase] ChargeMeter GameObject not assigned!");
            yield break;
        }
        
        // Aktifkan charge meter
        chargeMeterObject.SetActive(true);
        
        // Get component
        ChargeMeter chargeMeter = chargeMeterObject.GetComponent<ChargeMeter>();
        
        // Setup callback
        bool completed = false;
        chargeMeter.OnChargeSuccess = () => { completed = true; };
        
        // Wait sampai selesai
        while (!completed)
        {
            yield return null;
        }
        
        // Matikan lagi
        chargeMeterObject.SetActive(false);
    }

    [System.Obsolete]
    protected void GoToNextTimeOfDay()
    {
        StartCoroutine(ResetHeadTracking());
        AutoSaveProgress();

        if (NarratorManager.Instance != null)
        {
            NarratorManager.Instance.NextTimeOfDay();
        }
    }

    [System.Obsolete]
    protected void GoToNextDay()
    {
        StartCoroutine(ResetHeadTracking());

        AutoSaveProgress();

        if (NarratorManager.Instance != null)
        {
            NarratorManager.Instance.NextDay();
        }
    }

    [System.Obsolete]
    protected void GoToSpecificNarrator(NarratorDay day, TimeOfDay time)
    {
        if (NarratorManager.Instance != null)
        {
            NarratorManager.Instance.ChangeNarrator(day, time);
        }
    }

    #endregion
    #region CameraManagement
    protected IEnumerator SetCameraPanRangeFront()
    {
        var panTilt = cinemachineCamera.GetComponent<CinemachinePanTilt>();
        panTilt.PanAxis.Range = new Vector2(0f, 180f);
        yield return null;
    }

    protected IEnumerator SetCameraPanRangeBack()
    {
        var panTilt = cinemachineCamera.GetComponent<CinemachinePanTilt>();
        panTilt.PanAxis.Range = new Vector2(180f, 360f);
        yield return null;
    }

    protected IEnumerator SetCameraPanRangeRight()
    {
        var panTilt = cinemachineCamera.GetComponent<CinemachinePanTilt>();
        panTilt.PanAxis.Range = new Vector2(90f, 270f);
        yield return null;
    }

    protected IEnumerator SetCameraPanRangeLeft()
    {
        var panTilt = cinemachineCamera.GetComponent<CinemachinePanTilt>();
        panTilt.PanAxis.Range = new Vector2(-90f, 90f);
        yield return null;
    }
    #endregion

    #region Save Management
    protected void AutoSaveProgress()
    {
        if (saveFileManager == null)
        {
            Debug.LogWarning("[NarratorBase] SaveFileManager not assigned, skipping auto-save.");
            return;
        }

        try
        {
            // Update day progress based on current narrator
            if (NarratorManager.Instance != null)
            {
                int currentDayNumber = (int)NarratorManager.Instance.currentDay + 1; // Convert enum to 1-based day number

                // Update ScriptableObject day value before saving
                UpdateSaveDataDay(currentDayNumber);

                // Save current progress to JSON
                saveFileManager.SaveToCoreGameSavesJSON();

                Debug.Log($"[NarratorBase] Auto-saved progress: Day {currentDayNumber}, Time: {NarratorManager.Instance.currentTime}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NarratorBase] Auto-save failed: {e.Message}");
        }
    }

    private void UpdateSaveDataDay(int dayNumber)
    {
        var saveDataField = saveFileManager.GetType().GetField("targetSaveObject",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (saveDataField != null)
        {
            var coreGameSaves = saveDataField.GetValue(saveFileManager);
            if (coreGameSaves != null)
            {
                // Update day field using reflection
                var dayField = coreGameSaves.GetType().GetField("day");
                if (dayField != null)
                {
                    dayField.SetValue(coreGameSaves, dayNumber);
                    Debug.Log($"[NarratorBase] Updated save data day to: {dayNumber}");
                }
            }
        }
    }

    protected void ManualSave()
    {
        AutoSaveProgress();
    }

    protected void SaveWithDay(int dayNumber)
    {
        try
        {
            UpdateSaveDataDay(dayNumber);
            saveFileManager.SaveToCoreGameSavesJSON();
            Debug.Log($"[NarratorBase] Saved progress with custom day: {dayNumber}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NarratorBase] Custom save failed: {e.Message}");
        }
    }
#endregion

#region Head Tracking Management
    protected IEnumerator SetHeadTarget(CharacterType characterType, CharacterTarget targetType)
    {
        headTrackingManager.SetHeadTarget(characterType, targetType);
        yield return null;
    }

    protected IEnumerator SetHeadTarget(CharacterType characterType, CharacterTarget targetType, float weight)
    {
        headTrackingManager.SetHeadTarget(characterType, targetType, weight);
        yield return null;
    }

    protected IEnumerator ResetHeadTracking()
    {
        headTrackingManager.ResetAllHeadTracking();
        
        yield return null;
    }
    protected IEnumerator ResetBidanTrack()
    {
        yield return null;
    }
#endregion
#region Raycast Interaction Management
    /// <summary>
    /// Wait for player to interact with any raycast object and execute appropriate dialog with context
    /// </summary>
    [System.Obsolete]
    protected IEnumerator WaitForRaycastInteraction(System.Action<string> onInteractionCallback = null, string dayContext = "", string sequenceContext = "")
    {
        bool interactionCompleted = false;
        bool wasStaring = false;
        
        while (!interactionCompleted)
        {
            if (rayCastObject != null && rayCastObject.raycastStatus)
            {
                if (!wasStaring)
                {
                    wasStaring = true;
                    Debug.Log("[NarratorBase] Player looking at interactive object");
                }

                if (GetInteractionKeyDown()) 
                {
                    if (rayCastObject.currentHitObject != null)
                    {
                        RaycastObjectBehaviour behaviour = rayCastObject.currentHitObject.GetComponent<RaycastObjectBehaviour>();
                        if (behaviour != null)
                        {
                            string characterIdentity = behaviour.GetCharacterIdentity();
                            string dialogPath = behaviour.GetInteractionDialogPath(dayContext, sequenceContext);
                            
                            Debug.Log($"[NarratorBase] Interacting with character: {characterIdentity} (Context: {dayContext}/{sequenceContext})");
                            
                            // Execute callback with character identity
                            onInteractionCallback?.Invoke(characterIdentity);
                            
                            // If dialog path is provided, play it automatically
                            if (!string.IsNullOrEmpty(dialogPath))
                            {
                                bool dialogComplete = false;
                                #pragma warning disable CS0618
                                dialogGameManager.StartCoreGame(dialogPath, () => { dialogComplete = true; });
                                #pragma warning restore CS0618
                                yield return new WaitUntil(() => dialogComplete);
                            }
                            
                            behaviour.OnInteraction();
                        }
                    }
                    
                    interactionCompleted = true;
                }
            }
            else
            {
                wasStaring = false;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Wait for player to interact with any raycast object and execute appropriate dialog (backward compatibility)
    /// </summary>
    [System.Obsolete]
    protected IEnumerator WaitForRaycastInteraction(System.Action<string> onInteractionCallback = null)
    {
        return WaitForRaycastInteraction(onInteractionCallback, "", "");
    }
    
    /// <summary>
    /// Wait for interaction with specific character identity
    /// </summary>
    [System.Obsolete]
    protected IEnumerator WaitForSpecificCharacterInteraction(string targetIdentity, System.Action onInteractionCallback = null)
    {
        bool interactionCompleted = false;
        
        while (!interactionCompleted)
        {
            if (rayCastObject != null && rayCastObject.raycastStatus)
            {
                if (GetInteractionKeyDown()) 
                {
                    if (rayCastObject.currentHitObject != null)
                    {
                        RaycastObjectBehaviour behaviour = rayCastObject.currentHitObject.GetComponent<RaycastObjectBehaviour>();
                        if (behaviour != null && behaviour.GetCharacterIdentity() == targetIdentity)
                        {
                            Debug.Log($"[NarratorBase] Specific interaction with: {targetIdentity}");
                            onInteractionCallback?.Invoke();
                            interactionCompleted = true;
                        }
                        else
                        {
                            Debug.Log($"[NarratorBase] Wrong character! Looking for: {targetIdentity}, found: {behaviour?.GetCharacterIdentity()}");
                        }
                    }
                }
            }
            
            yield return null;
        }
    }

    /// <summary>
    /// Enable raycast interaction system
    /// </summary>
    protected void EnableRaycastInteraction()
    {
        if (rayCastObject != null)
        {
            rayCastObject.enabled = true;
            Debug.Log("[NarratorBase] Raycast interaction system enabled");
        }
        else
        {
            Debug.LogWarning("[NarratorBase] Cannot enable raycast interaction - rayCastObject is null");
        }
    }

    /// <summary>
    /// Disable raycast interaction system
    /// </summary>
    protected void DisableRaycastInteraction()
    {
        if (rayCastObject != null)
        {
            rayCastObject.enabled = false;
            Debug.Log("[NarratorBase] Raycast interaction system disabled");
        }
        else
        {
            Debug.LogWarning("[NarratorBase] Cannot disable raycast interaction - rayCastObject is null");
        }
    }
    
    /// <summary>
    /// Set context for all RaycastObjectBehaviour components in the scene
    /// </summary>
    protected void SetRaycastContext(string dayContext, string sequenceContext)
    {
        RaycastObjectBehaviour[] allRaycastObjects = FindObjectsByType<RaycastObjectBehaviour>(FindObjectsSortMode.None);
        foreach (var raycastObject in allRaycastObjects)
        {
            raycastObject.SetCurrentContext(dayContext, sequenceContext);
        }
        Debug.Log($"[NarratorBase] Context set for {allRaycastObjects.Length} raycast objects: Day={dayContext}, Sequence={sequenceContext}");
    }
#endregion

#region Common Sequence Operations
    protected IEnumerator TransitionScene()
    {
        CloseEyes();
        StartCoroutine(SwitchLights.Instance.SwitchToDark());
        yield return null;
    }

    protected void ReturnToMainMenu()
    {        
        if (saveFileManager != null)
        {
            saveFileManager.SaveToLocalMyGamesFolder();
        }
        
        SceneManager.LoadScene("MainMenu");
    }
#endregion
}
