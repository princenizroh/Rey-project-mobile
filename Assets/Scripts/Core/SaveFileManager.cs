using UnityEngine;
using System.IO;

public class SaveFileManager : MonoBehaviour
{
    [Header("Save Configuration")]
    [SerializeField] private string savesFolderPath = "Saves";
    [SerializeField] private string saveFileName = "save_data";
    [SerializeField] private CoreGameSaves targetSaveObject;
    
    [Header("Auto Restore")]
    [SerializeField] private bool restoreOnStart = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Local Save Path")]
    [SerializeField] private bool useLocalMyGamesPath = true;
    
    // Serializable class to match JSON structure
    [System.Serializable]
    public class SaveData
    {
        public int day;
        public int mother_stress_level;
        public TimeOfDay timeOfDay;
        
        // Constructor for easy initialization
        public SaveData()
        {
            day = 0;
            mother_stress_level = 0;
            timeOfDay = TimeOfDay.Morning; // Default to Morning
        }
        
        public SaveData(int day, int motherStress, TimeOfDay timeOfDay)
        {
            this.day = day;
            this.mother_stress_level = motherStress;
            this.timeOfDay = timeOfDay;
        }
    }

    // Temporary class for parsing JSON with string enums
    [System.Serializable]
    public class TempSaveData
    {
        public int day;
        public int mother_stress_level;
        public string timeOfDay;
    }
    
    /// <summary>
    /// Get the local My Games save path: Documents/My Games/Rey/saves
    /// </summary>
    private string GetLocalMyGamesSavePath()
    {
        // Get the user's Documents folder
        string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        
        // Build the path: Documents/My Games/Rey/saves
        return Path.Combine(documentsPath, "My Games", "Rey", "saves");
    }
    
    /// <summary>
    /// Get the full path to save_data.json in My Games folder
    /// </summary>
    private string GetLocalSaveFilePath()
    {
        return Path.Combine(GetLocalMyGamesSavePath(), "save_data.json");
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (restoreOnStart)
        {
            RestoreSaveFromJSON();
        }
    }

    /// <summary>
    /// Restore save data from JSON file (checks local My Games folder first if enabled, then Resources folder)
    /// </summary>
    [ContextMenu("Restore Save From JSON")]
    public void RestoreSaveFromJSON()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }

        // Check local My Games folder first if enabled
        if (useLocalMyGamesPath)
        {
            LogDebug("Checking local My Games folder for save data...");
            SaveData localSaveData = GetLocalSaveData();
            
            if (localSaveData != null)
            {
                ApplySaveDataToScriptableObject(localSaveData);
                LogDebug("✓ Save restored from local My Games folder");
                return;
            }
            else
            {
                LogDebug("No valid save found in local My Games folder, checking Resources...");
            }
        }

        try
        {
            // Fallback to Resources folder
            string resourcePath = $"{savesFolderPath}/{saveFileName}";
            
            // Load the JSON file from Resources
            TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
            
            if (jsonFile == null)
            {
                LogError($"Save file not found at Resources/{resourcePath}.json");
                CreateDefaultSaveFile();
                return;
            }

            // Parse JSON data
            string jsonContent = jsonFile.text;
            SaveData saveData = ParseSaveDataFromJSON(jsonContent);
            
            if (saveData == null)
            {
                LogError("Failed to parse JSON data. Creating default save.");
                CreateDefaultSaveFile();
                return;
            }

            // Apply data to ScriptableObject
            ApplySaveDataToScriptableObject(saveData);
            
            LogDebug($"✓ Save restored successfully from {resourcePath}.json");
            LogDebug($"  - Day: {saveData.day}");
            LogDebug($"  - Mother Stress Level: {saveData.mother_stress_level}");
            LogDebug($"  - Time of Day: {saveData.timeOfDay}");
        }
        catch (System.Exception e)
        {
            LogError($"Error restoring save: {e.Message}");
            CreateDefaultSaveFile();
        }
    }
    
    /// <summary>
    /// Apply loaded save data to the target ScriptableObject
    /// </summary>
    private void ApplySaveDataToScriptableObject(SaveData saveData)
    {
        if (targetSaveObject == null)
        {
            LogError("Target ScriptableObject is null!");
            return;
        }
        
        targetSaveObject.day = saveData.day;
        targetSaveObject.mother_stress_level = saveData.mother_stress_level;
        targetSaveObject.timeOfDay = saveData.timeOfDay;
        
        // Mark as dirty for Unity to save changes in editor
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(targetSaveObject);
        #endif
        
        LogDebug($"ScriptableObject updated with save data - Day: {saveData.day}, Stress: {saveData.mother_stress_level}, TimeOfDay: {saveData.timeOfDay}");
    }

    /// <summary>
    /// Parse JSON with custom handling for TimeOfDay enum (supports both string and integer values)
    /// </summary>
    private SaveData ParseSaveDataFromJSON(string jsonContent)
    {
        try
        {
            // First try standard Unity JsonUtility
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonContent);
            
            if (saveData != null)
            {
                LogDebug("Successfully parsed JSON with standard JsonUtility");
                return saveData;
            }
        }
        catch (System.Exception e)
        {
            LogDebug($"Standard JsonUtility failed: {e.Message}, trying custom parsing...");
        }

        // If standard parsing fails, try custom parsing for enum strings
        try
        {
            return ParseSaveDataWithCustomEnum(jsonContent);
        }
        catch (System.Exception e)
        {
            LogError($"Custom enum parsing also failed: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Custom parser that handles TimeOfDay as string values
    /// </summary>
    private SaveData ParseSaveDataWithCustomEnum(string jsonContent)
    {
        TempSaveData tempData = JsonUtility.FromJson<TempSaveData>(jsonContent);
        
        if (tempData == null)
        {
            throw new System.Exception("Failed to parse JSON even with custom parser");
        }

        SaveData saveData = new SaveData();
        saveData.day = tempData.day;
        saveData.mother_stress_level = tempData.mother_stress_level;

        // Convert string timeOfDay to enum
        if (System.Enum.TryParse<TimeOfDay>(tempData.timeOfDay, true, out TimeOfDay parsedTimeOfDay))
        {
            saveData.timeOfDay = parsedTimeOfDay;
            LogDebug($"Successfully converted TimeOfDay string '{tempData.timeOfDay}' to enum: {parsedTimeOfDay}");
        }
        else
        {
            LogError($"Failed to parse TimeOfDay string: '{tempData.timeOfDay}', defaulting to Morning");
            saveData.timeOfDay = TimeOfDay.Morning;
        }

        return saveData;
    }
    
    /// <summary>
    /// Create a default save file if none exists
    /// </summary>
    private void CreateDefaultSaveFile()
    {
        try
        {
            SaveData defaultSave = new SaveData();
            string jsonContent = JsonUtility.ToJson(defaultSave, true);
            
            // For Resources folder, we need to save to StreamingAssets or persistent data path
            // Since Resources is read-only at runtime, we'll save to persistent data path
            string persistentPath = Path.Combine(Application.persistentDataPath, "Saves");
            
            if (!Directory.Exists(persistentPath))
            {
                Directory.CreateDirectory(persistentPath);
            }
            
            string filePath = Path.Combine(persistentPath, $"{saveFileName}.json");
            File.WriteAllText(filePath, jsonContent);
            
            LogDebug($"Default save file created at: {filePath}");
            LogDebug("Note: For runtime loading, place save files in Resources folder manually.");
            
            // Also apply default values to ScriptableObject
            ApplySaveDataToScriptableObject(defaultSave);
        }
        catch (System.Exception e)
        {
            LogError($"Failed to create default save file: {e.Message}");
        }
    }
    
    /// <summary>
    /// Check if save_data.json exists in the local My Games folder
    /// </summary>
    public bool CheckLocalSaveFileExists()
    {
        string saveFilePath = GetLocalSaveFilePath();
        bool exists = File.Exists(saveFilePath);
        
        LogDebug($"Checking local save file at: {saveFilePath}");
        LogDebug($"File exists: {exists}");
        
        return exists;
    }
    
    /// <summary>
    /// Load save data from local My Games folder
    /// </summary>
    [ContextMenu("Load from Local My Games Folder")]
    public void LoadFromLocalMyGamesFolder()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }
        
        string saveFilePath = GetLocalSaveFilePath();
        
        if (!File.Exists(saveFilePath))
        {
            LogError($"Save file not found at: {saveFilePath}");
            return;
        }
        
        try
        {
            string jsonContent = File.ReadAllText(saveFilePath);
            SaveData saveData = ParseSaveDataFromJSON(jsonContent);
            
            if (saveData == null)
            {
                LogError("Failed to parse JSON data from local My Games folder.");
                return;
            }
            
            ApplySaveDataToScriptableObject(saveData);
            
            LogDebug($"✓ Save loaded from local My Games folder: {saveFilePath}");
            LogDebug($"  - Day: {saveData.day}");
            LogDebug($"  - Mother Stress Level: {saveData.mother_stress_level}");
            LogDebug($"  - Time of Day: {saveData.timeOfDay}");
        }
        catch (System.Exception e)
        {
            LogError($"Error loading from local My Games folder: {e.Message}");
        }
    }
    
    /// <summary>
    /// Save current data to local My Games folder
    /// </summary>
    [ContextMenu("Save to Local My Games Folder")]
    public void SaveToLocalMyGamesFolder()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }
        
        try
        {
            // Create save data from current ScriptableObject
            SaveData currentData = new SaveData(targetSaveObject.day, targetSaveObject.mother_stress_level, targetSaveObject.timeOfDay);
            
            // Convert to JSON with pretty formatting
            string jsonContent = JsonUtility.ToJson(currentData, true);
            
            // Get the local My Games save path
            string saveDirectoryPath = GetLocalMyGamesSavePath();
            string saveFilePath = GetLocalSaveFilePath();
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
                LogDebug($"Created directory: {saveDirectoryPath}");
            }
            
            // Write the JSON file
            File.WriteAllText(saveFilePath, jsonContent);
            
            LogDebug($"✓ Save data written to local My Games folder: {saveFilePath}");
            LogDebug($"JSON Content:\n{jsonContent}");
        }
        catch (System.Exception e)
        {
            LogError($"Error saving to local My Games folder: {e.Message}");
        }
    }
    
    /// <summary>
    /// Get save data from local My Games folder (without applying to ScriptableObject)
    /// Returns null if file doesn't exist or can't be parsed
    /// </summary>
    public SaveData GetLocalSaveData()
    {
        string saveFilePath = GetLocalSaveFilePath();
        
        if (!File.Exists(saveFilePath))
        {
            LogDebug($"Save file not found at: {saveFilePath}");
            return null;
        }
        
        try
        {
            string jsonContent = File.ReadAllText(saveFilePath);
            SaveData saveData = ParseSaveDataFromJSON(jsonContent);
            
            LogDebug($"✓ Save data retrieved from: {saveFilePath}");
            if (saveData != null)
            {
                LogDebug($"  - Day: {saveData.day}");
                LogDebug($"  - Mother Stress Level: {saveData.mother_stress_level}");
                LogDebug($"  - Time of Day: {saveData.timeOfDay}");
            }
            
            return saveData;
        }
        catch (System.Exception e)
        {
            LogError($"Error reading local save data: {e.Message}");
            return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// Validate that save_data.json exists in local My Games folder
    /// </summary>
    [ContextMenu("Validate Local My Games Save File")]
    public void ValidateLocalMyGamesSaveFile()
    {
        string saveFilePath = GetLocalSaveFilePath();
        string saveDirectoryPath = GetLocalMyGamesSavePath();
        
        LogDebug($"Checking local My Games save file at: {saveFilePath}");
        
        if (!Directory.Exists(saveDirectoryPath))
        {
            LogError($"✗ Save directory not found: {saveDirectoryPath}");
            LogDebug("Use 'Save to Local My Games Folder' to create the directory and save file.");
            return;
        }
        
        if (File.Exists(saveFilePath))
        {
            LogDebug($"✓ Save file found at: {saveFilePath}");
            
            try
            {
                string jsonContent = File.ReadAllText(saveFilePath);
                LogDebug($"Content preview:\n{jsonContent}");
                
                // Validate JSON structure
                SaveData testData = ParseSaveDataFromJSON(jsonContent);
                if (testData != null)
                {
                    LogDebug($"✓ JSON structure is valid:");
                    LogDebug($"  - Day: {testData.day}");
                    LogDebug($"  - Mother Stress Level: {testData.mother_stress_level}");
                    LogDebug($"  - Time of Day: {testData.timeOfDay}");
                }
                else
                {
                    LogError("✗ JSON structure is invalid - could not parse SaveData");
                }
            }
            catch (System.Exception e)
            {
                LogError($"✗ JSON parsing error: {e.Message}");
            }
        }
        else
        {
            LogError($"✗ Save file not found at: {saveFilePath}");
            LogDebug("Use 'Save to Local My Games Folder' to create the save file.");
        }
    }
    
    /// <summary>
    /// Show information about local My Games save path
    /// </summary>
    [ContextMenu("Show Local My Games Path Info")]
    public void ShowLocalMyGamesPathInfo()
    {
        string saveDirectoryPath = GetLocalMyGamesSavePath();
        string saveFilePath = GetLocalSaveFilePath();
        
        LogDebug($"Local My Games save directory: {saveDirectoryPath}");
        LogDebug($"Local My Games save file path: {saveFilePath}");
        LogDebug($"Directory exists: {Directory.Exists(saveDirectoryPath)}");
        LogDebug($"Save file exists: {File.Exists(saveFilePath)}");
        
        if (Directory.Exists(saveDirectoryPath))
        {
            try
            {
                string[] files = Directory.GetFiles(saveDirectoryPath, "*.json");
                LogDebug($"JSON files in directory: {files.Length}");
                
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    long fileSize = new FileInfo(file).Length;
                    string lastModified = File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm:ss");
                    LogDebug($"  - {fileName} ({fileSize} bytes, modified: {lastModified})");
                }
            }
            catch (System.Exception e)
            {
                LogError($"Error listing files: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Save current ScriptableObject data to JSON (for testing/debugging)
    /// </summary>
    [ContextMenu("Save Current Data to JSON")]
    public void SaveCurrentDataToJSON()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }
        
        try
        {
            // Create save data from current ScriptableObject
            SaveData currentData = new SaveData(targetSaveObject.day, targetSaveObject.mother_stress_level, targetSaveObject.timeOfDay);
            
            // Convert to JSON
            string jsonContent = JsonUtility.ToJson(currentData, true);
            
            // Save to persistent data path
            string persistentPath = Path.Combine(Application.persistentDataPath, "Saves");
            
            if (!Directory.Exists(persistentPath))
            {
                Directory.CreateDirectory(persistentPath);
            }
            
            string filePath = Path.Combine(persistentPath, $"{saveFileName}.json");
            File.WriteAllText(filePath, jsonContent);
            
            LogDebug($"✓ Current data saved to: {filePath}");
            LogDebug($"  - Day: {currentData.day}");
            LogDebug($"  - Mother Stress Level: {currentData.mother_stress_level}");
            LogDebug($"  - Time of Day: {currentData.timeOfDay}");
        }
        catch (System.Exception e)
        {
            LogError($"Error saving current data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Save CoreGameSaves data to coregamesaves.json in persistent data path
    /// This creates a specifically named file for the CoreGameSaves ScriptableObject
    /// </summary>
    [ContextMenu("Save to CoreGameSaves.json")]
    public void SaveToCoreGameSavesJSON()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }
        
        try
        {
            // Create save data from current ScriptableObject
            SaveData currentData = new SaveData(targetSaveObject.day, targetSaveObject.mother_stress_level, targetSaveObject.timeOfDay);
            
            // Convert to JSON with pretty formatting
            string jsonContent = JsonUtility.ToJson(currentData, true);
            
            // Save to persistent data path with specific filename
            string persistentPath = Path.Combine(Application.persistentDataPath, "Saves");
            
            if (!Directory.Exists(persistentPath))
            {
                Directory.CreateDirectory(persistentPath);
            }
            
            string filePath = Path.Combine(persistentPath, "coregamesaves.json");
            File.WriteAllText(filePath, jsonContent);
            
            LogDebug($"✓ CoreGameSaves data saved to: {filePath}");
            LogDebug($"JSON Content:\n{jsonContent}");
            LogDebug($"To use in Resources folder, copy this file to: Assets/Resources/Saves/coregamesaves.json");
            
            // Also try to save to StreamingAssets if it exists (accessible at runtime)
            SaveToStreamingAssets(jsonContent, "coregamesaves.json");
            
        }
        catch (System.Exception e)
        {
            LogError($"Error saving to CoreGameSaves.json: {e.Message}");
        }
    }
    
    /// <summary>
    /// Save to StreamingAssets folder (if it exists) for runtime access
    /// </summary>
    private void SaveToStreamingAssets(string jsonContent, string fileName)
    {
        try
        {
            string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "Saves");
            
            // Only try to save if StreamingAssets exists
            if (Directory.Exists(Application.streamingAssetsPath))
            {
                if (!Directory.Exists(streamingAssetsPath))
                {
                    Directory.CreateDirectory(streamingAssetsPath);
                }
                
                string filePath = Path.Combine(streamingAssetsPath, fileName);
                File.WriteAllText(filePath, jsonContent);
                
                LogDebug($"✓ Also saved to StreamingAssets: {filePath}");
            }
        }
        catch (System.Exception e)
        {
            LogDebug($"Could not save to StreamingAssets: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load specifically from coregamesaves.json file
    /// </summary>
    [ContextMenu("Load from CoreGameSaves.json")]
    public void LoadFromCoreGameSavesJSON()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }
        
        try
        {
            // First try Resources folder
            string resourcePath = $"{savesFolderPath}/coregamesaves";
            TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
            
            if (jsonFile != null)
            {
                // Parse JSON data from Resources
                string jsonContent = jsonFile.text;
                SaveData saveData = ParseSaveDataFromJSON(jsonContent);
                
                if (saveData != null)
                {
                    ApplySaveDataToScriptableObject(saveData);
                    LogDebug($"✓ CoreGameSaves loaded from Resources/{resourcePath}.json");
                    LogDebug($"  - Day: {saveData.day}");
                    LogDebug($"  - Mother Stress Level: {saveData.mother_stress_level}");
                    LogDebug($"  - Time of Day: {saveData.timeOfDay}");
                    return;
                }
            }
            
            // Fallback to persistent data path
            LoadCoreGameSavesFromPersistentPath();
            
        }
        catch (System.Exception e)
        {
            LogError($"Error loading CoreGameSaves.json: {e.Message}");
            LoadCoreGameSavesFromPersistentPath();
        }
    }
    
    /// <summary>
    /// Load coregamesaves.json from persistent data path
    /// </summary>
    private void LoadCoreGameSavesFromPersistentPath()
    {
        try
        {
            string persistentPath = Path.Combine(Application.persistentDataPath, "Saves");
            string filePath = Path.Combine(persistentPath, "coregamesaves.json");
            
            if (!File.Exists(filePath))
            {
                LogError($"coregamesaves.json not found at: {filePath}");
                LogDebug("Use 'Save to CoreGameSaves.json' to create the file first.");
                return;
            }
            
            string jsonContent = File.ReadAllText(filePath);
            SaveData saveData = ParseSaveDataFromJSON(jsonContent);
            
            if (saveData == null)
            {
                LogError("Failed to parse coregamesaves.json from persistent path.");
                return;
            }
            
            ApplySaveDataToScriptableObject(saveData);
            
            LogDebug($"✓ CoreGameSaves loaded from persistent path: {filePath}");
            LogDebug($"  - Day: {saveData.day}");
            LogDebug($"  - Mother Stress Level: {saveData.mother_stress_level}");
            LogDebug($"  - Time of Day: {saveData.timeOfDay}");
            
        }
        catch (System.Exception e)
        {
            LogError($"Error loading coregamesaves.json from persistent path: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load save data from persistent data path (alternative to Resources)
    /// </summary>
    [ContextMenu("Load from Persistent Data Path")]
    public void LoadFromPersistentDataPath()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }
        
        try
        {
            string persistentPath = Path.Combine(Application.persistentDataPath, "Saves");
            string filePath = Path.Combine(persistentPath, $"{saveFileName}.json");
            
            if (!File.Exists(filePath))
            {
                LogError($"Save file not found at: {filePath}");
                CreateDefaultSaveFile();
                return;
            }
            
            string jsonContent = File.ReadAllText(filePath);
            SaveData saveData = ParseSaveDataFromJSON(jsonContent);
            
            if (saveData == null)
            {
                LogError("Failed to parse JSON data from persistent path.");
                return;
            }
            
            ApplySaveDataToScriptableObject(saveData);
            
            LogDebug($"✓ Save loaded from persistent path: {filePath}");
            LogDebug($"  - Day: {saveData.day}");
            LogDebug($"  - Mother Stress Level: {saveData.mother_stress_level}");
            LogDebug($"  - Time of Day: {saveData.timeOfDay}");
        }
        catch (System.Exception e)
        {
            LogError($"Error loading from persistent data path: {e.Message}");
        }
    }
    
    /// <summary>
    /// Reset ScriptableObject to default values
    /// </summary>
    [ContextMenu("Reset to Default Values")]
    public void ResetToDefaultValues()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }
        
        SaveData defaultSave = new SaveData();
        ApplySaveDataToScriptableObject(defaultSave);
        
        LogDebug("ScriptableObject reset to default values");
    }

    /// <summary>
    /// Convert string-based TimeOfDay JSON to integer-based format for Unity compatibility
    /// </summary>
    [ContextMenu("Convert String TimeOfDay JSON to Integer Format")]
    public void ConvertTimeOfDayJSONFormat()
    {
        string saveFilePath = GetLocalSaveFilePath();
        
        if (!File.Exists(saveFilePath))
        {
            LogError($"Save file not found at: {saveFilePath}");
            return;
        }
        
        try
        {
            string jsonContent = File.ReadAllText(saveFilePath);
            LogDebug($"Original JSON:\n{jsonContent}");
            
            // Parse with custom parser that handles string enums
            SaveData saveData = ParseSaveDataFromJSON(jsonContent);
            
            if (saveData != null)
            {
                // Convert back to JSON with integer enum values
                string convertedJson = JsonUtility.ToJson(saveData, true);
                
                // Save the converted version
                File.WriteAllText(saveFilePath, convertedJson);
                
                LogDebug($"✓ JSON converted and saved with integer TimeOfDay format:");
                LogDebug($"Converted JSON:\n{convertedJson}");
            }
            else
            {
                LogError("Failed to parse the JSON file for conversion");
            }
        }
        catch (System.Exception e)
        {
            LogError($"Error converting JSON format: {e.Message}");
        }
    }
    
    /// <summary>
    /// Get current save data as JSON string (for debugging)
    /// </summary>
    public string GetCurrentSaveAsJSON()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return "{}";
        }
        
        SaveData currentData = new SaveData(targetSaveObject.day, targetSaveObject.mother_stress_level, targetSaveObject.timeOfDay);
        return JsonUtility.ToJson(currentData, true);
    }
    
    /// <summary>
    /// Validate save file exists in Resources folder
    /// </summary>
    [ContextMenu("Validate Resources Save File")]
    public void ValidateResourcesSaveFile()
    {
        string resourcePath = $"{savesFolderPath}/{saveFileName}";
        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
        
        if (jsonFile != null)
        {
            LogDebug($"✓ Save file found at Resources/{resourcePath}.json");
            LogDebug($"Content preview:\n{jsonFile.text}");
        }
        else
        {
            LogError($"✗ Save file not found at Resources/{resourcePath}.json");
            LogDebug("Make sure to place your JSON save file in the Resources folder!");
        }
    }
    
    /// <summary>
    /// Validate that coregamesaves.json exists in Resources folder
    /// </summary>
    [ContextMenu("Validate CoreGameSaves.json in Resources")]
    public void ValidateCoreGameSavesInResources()
    {
        string resourcePath = $"{savesFolderPath}/coregamesaves";
        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
        
        if (jsonFile != null)
        {
            LogDebug($"✓ coregamesaves.json found at Resources/{resourcePath}.json");
            LogDebug($"Content:\n{jsonFile.text}");
            
            // Also validate the JSON structure
            try
            {
                SaveData testData = ParseSaveDataFromJSON(jsonFile.text);
                if (testData != null)
                {
                    LogDebug($"✓ JSON structure is valid:");
                    LogDebug($"  - Day: {testData.day}");
                    LogDebug($"  - Mother Stress Level: {testData.mother_stress_level}");
                    LogDebug($"  - Time of Day: {testData.timeOfDay}");
                }
                else
                {
                    LogError("✗ JSON structure is invalid - could not parse SaveData");
                }
            }
            catch (System.Exception e)
            {
                LogError($"✗ JSON parsing error: {e.Message}");
            }
        }
        else
        {
            LogError($"✗ coregamesaves.json not found at Resources/{savesFolderPath}/coregamesaves.json");
            LogDebug("Use 'Save to CoreGameSaves.json' and manually copy the file to Resources folder.");
            
            // Check if file exists in persistent data path
            string persistentPath = Path.Combine(Application.persistentDataPath, "Saves", "coregamesaves.json");
            if (File.Exists(persistentPath))
            {
                LogDebug($"Found coregamesaves.json in persistent data path: {persistentPath}");
                LogDebug("Copy this file to Assets/Resources/Saves/ folder for Resources loading.");
            }
        }
    }
    
    /// <summary>
    /// Show all available save files in persistent data path
    /// </summary>
    [ContextMenu("List All Save Files")]
    public void ListAllSaveFiles()
    {
        try
        {
            string persistentPath = Path.Combine(Application.persistentDataPath, "Saves");
            
            if (!Directory.Exists(persistentPath))
            {
                LogDebug("No save files found - Saves directory doesn't exist in persistent data path.");
                return;
            }
            
            string[] jsonFiles = Directory.GetFiles(persistentPath, "*.json");
            
            if (jsonFiles.Length == 0)
            {
                LogDebug("No JSON save files found in persistent data path.");
                return;
            }
            
            LogDebug($"Found {jsonFiles.Length} save file(s) in {persistentPath}:");
            
            foreach (string filePath in jsonFiles)
            {
                string fileName = Path.GetFileName(filePath);
                long fileSize = new FileInfo(filePath).Length;
                string lastModified = File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd HH:mm:ss");
                
                LogDebug($"  - {fileName} ({fileSize} bytes, modified: {lastModified})");
                
                // Show preview for small files
                if (fileSize < 1000)
                {
                    try
                    {
                        string content = File.ReadAllText(filePath);
                        LogDebug($"    Preview: {content.Replace("\n", " ").Replace("\r", "")}");
                    }
                    catch (System.Exception e)
                    {
                        LogDebug($"    Could not read file: {e.Message}");
                    }
                }
            }
            
            // Also check StreamingAssets
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Saves");
            if (Directory.Exists(streamingPath))
            {
                string[] streamingFiles = Directory.GetFiles(streamingPath, "*.json");
                if (streamingFiles.Length > 0)
                {
                    LogDebug($"\nAlso found {streamingFiles.Length} file(s) in StreamingAssets/Saves:");
                    foreach (string filePath in streamingFiles)
                    {
                        string fileName = Path.GetFileName(filePath);
                        LogDebug($"  - {fileName}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            LogError($"Error listing save files: {e.Message}");
        }
    }
    
    /// <summary>
    /// Copy save files from persistent data path to a easily accessible location
    /// (Desktop or Documents folder for manual copying to Resources)
    /// </summary>
    [ContextMenu("Export Save Files to Desktop")]
    public void ExportSaveFilesToDesktop()
    {
        try
        {
            string persistentPath = Path.Combine(Application.persistentDataPath, "Saves");
            
            if (!Directory.Exists(persistentPath))
            {
                LogError("No save files to export - Saves directory doesn't exist.");
                return;
            }
            
            // Create export folder on Desktop
            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            string exportPath = Path.Combine(desktopPath, "Unity_Save_Files_Export");
            
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }
            
            string[] jsonFiles = Directory.GetFiles(persistentPath, "*.json");
            int exportedCount = 0;
            
            foreach (string sourceFile in jsonFiles)
            {
                string fileName = Path.GetFileName(sourceFile);
                string destinationFile = Path.Combine(exportPath, fileName);
                
                File.Copy(sourceFile, destinationFile, true);
                exportedCount++;
                
                LogDebug($"Exported: {fileName}");
            }
            
            if (exportedCount > 0)
            {
                LogDebug($"✓ Exported {exportedCount} save file(s) to: {exportPath}");
                LogDebug("You can now manually copy these files to Assets/Resources/Saves/ folder.");
                
                // Try to open the folder (Windows only)
                #if UNITY_EDITOR_WIN
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", exportPath);
                }
                catch
                {
                    // Ignore if can't open folder
                }
                #endif
            }
            else
            {
                LogDebug("No JSON files found to export.");
            }
        }
        catch (System.Exception e)
        {
            LogError($"Error exporting save files: {e.Message}");
        }
    }
    
    /// <summary>
    /// Test JSON serialization format for TimeOfDay enum
    /// </summary>
    [ContextMenu("Test JSON Enum Format")]
    public void TestJSONEnumFormat()
    {
        LogDebug("=== Testing JSON Enum Format ===");
        
        // Test all TimeOfDay values
        TimeOfDay[] allTimes = { TimeOfDay.Morning, TimeOfDay.Afternoon, TimeOfDay.Evening, TimeOfDay.Night };
        
        for (int i = 0; i < allTimes.Length; i++)
        {
            SaveData testData = new SaveData();
            testData.day = 1;
            testData.mother_stress_level = 100;
            testData.timeOfDay = allTimes[i];
            
            string json = JsonUtility.ToJson(testData, true);
            LogDebug($"TimeOfDay.{allTimes[i]} (should be {i}) serializes as:");
            LogDebug(json);
            LogDebug("");
        }
        
        LogDebug("=== JSON Enum Format Test Complete ===");
    }

    /// <summary>
    /// Force save with integer enum format (ensures no string conversion)
    /// </summary>
    [ContextMenu("Force Save with Integer Enum Format")]
    public void ForceSaveWithIntegerEnumFormat()
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }
        
        try
        {
            // Create save data with explicit enum values
            SaveData currentData = new SaveData();
            currentData.day = targetSaveObject.day;
            currentData.mother_stress_level = targetSaveObject.mother_stress_level;
            currentData.timeOfDay = targetSaveObject.timeOfDay;
            
            // Use Unity's JsonUtility which should serialize enums as integers
            string jsonContent = JsonUtility.ToJson(currentData, true);
            
            // Get the local My Games save path
            string saveFilePath = GetLocalSaveFilePath();
            string saveDirectoryPath = GetLocalMyGamesSavePath();
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
                LogDebug($"Created directory: {saveDirectoryPath}");
            }
            
            // Write the JSON file
            File.WriteAllText(saveFilePath, jsonContent);
            
            LogDebug($"✓ Save data written with INTEGER enum format to: {saveFilePath}");
            LogDebug($"TimeOfDay enum value: {currentData.timeOfDay} = {(int)currentData.timeOfDay}");
            LogDebug($"JSON Content:\n{jsonContent}");
            
        }
        catch (System.Exception e)
        {
            LogError($"Error saving with integer enum format: {e.Message}");
        }
    }

    #region Logging Helpers
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[RestoreSaves] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[RestoreSaves] {message}");
    }
    
    #endregion
    
    #region Public Getters/Setters
    
    /// <summary>
    /// Check if a valid save file exists in the local My Games folder
    /// This method is designed to be called by UI systems like MainMenuManager
    /// </summary>
    public bool HasValidLocalSaveFile()
    {
        try
        {
            SaveData saveData = GetLocalSaveData();
            
            if (saveData == null)
            {
                return false;
            }
            
            // Check if save data has meaningful values (not default/empty save)
            // A save is considered valid if day > 1 OR mother_stress_level > 0
            bool isValidSave = saveData.day > 1 || saveData.mother_stress_level > 0;
            
            LogDebug($"Local save file validation - Day: {saveData.day}, Stress: {saveData.mother_stress_level}, TimeOfDay: {saveData.timeOfDay}, Valid: {isValidSave}");
            
            return isValidSave;
        }
        catch (System.Exception e)
        {
            LogError($"Error checking local save file validity: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get the full path to the local My Games save file (for external use)
    /// </summary>
    public string GetLocalSaveFileFullPath()
    {
        return GetLocalSaveFilePath();
    }
    
    /// <summary>
    /// Change the saves folder path at runtime
    /// </summary>
    public void SetSavesFolderPath(string newPath)
    {
        savesFolderPath = newPath;
        LogDebug($"Saves folder path changed to: {newPath}");
    }
    
    /// <summary>
    /// Change the save file name at runtime
    /// </summary>
    public void SetSaveFileName(string newFileName)
    {
        saveFileName = newFileName;
        LogDebug($"Save file name changed to: {newFileName}");
    }
    
    /// <summary>
    /// Get current saves folder path
    /// </summary>
    public string GetSavesFolderPath()
    {
        return savesFolderPath;
    }
    
    /// <summary>
    /// Get current save file name
    /// </summary>
    public string GetSaveFileName()
    {
        return saveFileName;
    }
    
    /// <summary>
    /// Set target ScriptableObject at runtime
    /// </summary>
    public void SetTargetSaveObject(CoreGameSaves newTarget)
    {
        targetSaveObject = newTarget;
        LogDebug($"Target ScriptableObject changed to: {(newTarget ? newTarget.name : "null")}");
    }
    
    /// <summary>
    /// Update CoreGameSaves ScriptableObject data directly
    /// </summary>
    /// <param name="day">The day value to set (e.g., 1, 2, 3, etc.)</param>
    /// <param name="timeOfDay">The time of day as integer (0=Morning, 1=Afternoon, 2=Evening, 3=Night)</param>
    /// <param name="mother_stress_level">The mother stress level value</param>
    public void UpdateCoreGameSaves(int day, int timeOfDay)
    {
        if (targetSaveObject == null)
        {
            LogError("Target CoreGameSaves ScriptableObject is not assigned!");
            return;
        }
        
        // Validate timeOfDay parameter (0-3 range)
        if (timeOfDay < 0 || timeOfDay > 3)
        {
            LogError($"Invalid timeOfDay value: {timeOfDay}. Must be 0-3 (0=Morning, 1=Afternoon, 2=Evening, 3=Night)");
            return;
        }
        
        // Store previous values for logging
        int prevDay = targetSaveObject.day;
        TimeOfDay prevTimeOfDay = targetSaveObject.timeOfDay;
        
        // Update the ScriptableObject values
        targetSaveObject.day = day;
        targetSaveObject.timeOfDay = (TimeOfDay)timeOfDay; // Cast int to TimeOfDay enum
        
        // Mark as dirty for Unity Editor
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(targetSaveObject);
        #endif
        
        LogDebug($"✓ CoreGameSaves updated successfully:");
        LogDebug($"  Day: {prevDay} → {day}");
        LogDebug($"  TimeOfDay: {prevTimeOfDay} ({(int)prevTimeOfDay}) → {(TimeOfDay)timeOfDay} ({timeOfDay})");
    }
    
    /// <summary>
    /// Update CoreGameSaves and automatically save to JSON file
    /// </summary>
    /// <param name="day">The day value to set</param>
    /// <param name="timeOfDay">The time of day as integer (0=Morning, 1=Afternoon, 2=Evening, 3=Night)</param>
    /// <param name="mother_stress_level">The mother stress level value</param>
    public void UpdateAndSaveToJSON(int day, int timeOfDay, int mother_stress_level)
    {
        // First update the ScriptableObject
        UpdateCoreGameSaves(day, timeOfDay);
        
        // Then save to JSON file
        if (targetSaveObject != null)
        {
            SaveToLocalMyGamesFolder();
            LogDebug("✓ Data updated and saved to JSON file");
        }
    }
    
    #endregion
}
