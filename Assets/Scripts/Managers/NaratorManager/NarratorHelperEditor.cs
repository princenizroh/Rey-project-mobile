#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NarratorHelper))]
public class NarratorHelperEditor : Editor
{
    private NarratorHelper narrator;
    
    // Variables for individual spawn index control
    private int motherSpawnIndex = 0;
    private int fatherSpawnIndex = 0;
    private int babySpawnIndex = 0;
    private int bidanSpawnIndex = 0;
    
    // Variable for batch spawn index
    private int batchSpawnIndex = 0;
    
    void OnEnable()
    {
        narrator = (NarratorHelper)target;
    }
    
    public override void OnInspectorGUI()
    {
        // === CUSTOM TOOLS FIRST (AT TOP) ===
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Position Setup Tools", EditorStyles.boldLabel);
        
        // Validation check
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate Spawn Positions", GUILayout.Height(20)))
        {
            bool isValid = narrator.ValidateSpawnPositions(5); // Check for at least 5 spawn positions
            if (isValid)
            {
                EditorUtility.DisplayDialog("Validation", "All characters have sufficient spawn positions!", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Individual Character Snap with Index Control
        EditorGUILayout.LabelField("Individual Character Snapping:", EditorStyles.miniBoldLabel);
        
        // Mother controls
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mother:", GUILayout.Width(60));
        motherSpawnIndex = EditorGUILayout.IntSlider(motherSpawnIndex, 0, narrator.GetMaxSpawnIndex(CharacterType.Mother));
        if (GUILayout.Button($"Snap to {motherSpawnIndex}", GUILayout.Width(80)))
        {
            narrator.SnapCharacterToSpawn(CharacterType.Mother, motherSpawnIndex);
        }
        EditorGUILayout.EndHorizontal();
        
        // Father controls
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Father:", GUILayout.Width(60));
        fatherSpawnIndex = EditorGUILayout.IntSlider(fatherSpawnIndex, 0, narrator.GetMaxSpawnIndex(CharacterType.Father));
        if (GUILayout.Button($"Snap to {fatherSpawnIndex}", GUILayout.Width(80)))
        {
            narrator.SnapCharacterToSpawn(CharacterType.Father, fatherSpawnIndex);
        }
        EditorGUILayout.EndHorizontal();
        
        // Baby controls
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Baby:", GUILayout.Width(60));
        babySpawnIndex = EditorGUILayout.IntSlider(babySpawnIndex, 0, narrator.GetMaxSpawnIndex(CharacterType.Baby));
        if (GUILayout.Button($"Snap to {babySpawnIndex}", GUILayout.Width(80)))
        {
            narrator.SnapCharacterToSpawn(CharacterType.Baby, babySpawnIndex);
        }
        EditorGUILayout.EndHorizontal();
        
        // Bidan controls
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Bidan:", GUILayout.Width(60));
        bidanSpawnIndex = EditorGUILayout.IntSlider(bidanSpawnIndex, 0, narrator.GetMaxSpawnIndex(CharacterType.Bidan));
        if (GUILayout.Button($"Snap to {bidanSpawnIndex}", GUILayout.Width(80)))
        {
            narrator.SnapCharacterToSpawn(CharacterType.Bidan, bidanSpawnIndex);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Multi-character snap button
        EditorGUILayout.LabelField("Multi-Character Control:", EditorStyles.miniBoldLabel);
        if (GUILayout.Button("Snap All Characters to Selected Indices", GUILayout.Height(25)))
        {
            narrator.SnapCharactersToMultipleSpawns(motherSpawnIndex, fatherSpawnIndex, babySpawnIndex, bidanSpawnIndex);
        }
        
        EditorGUILayout.Space(5);
        
        // Batch Operations
        EditorGUILayout.LabelField("Batch Operations:", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Batch Index:", GUILayout.Width(80));
        batchSpawnIndex = EditorGUILayout.IntSlider(batchSpawnIndex, 0, 4); // Assuming max 5 spawn positions
        if (GUILayout.Button($"Snap All to {batchSpawnIndex}", GUILayout.Width(100)))
        {
            narrator.SnapAllCharactersToSpawn(batchSpawnIndex);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Quick preset buttons
        EditorGUILayout.LabelField("Quick Presets:", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("All to Spawn 0"))
        {
            motherSpawnIndex = fatherSpawnIndex = babySpawnIndex = bidanSpawnIndex = 0;
            narrator.SnapAllCharactersToSpawn(0);
        }
        if (GUILayout.Button("All to Spawn 1"))
        {
            motherSpawnIndex = fatherSpawnIndex = babySpawnIndex = bidanSpawnIndex = 1;
            narrator.SnapAllCharactersToSpawn(1);
        }
        if (GUILayout.Button("All to Spawn 2"))
        {
            motherSpawnIndex = fatherSpawnIndex = babySpawnIndex = bidanSpawnIndex = 2;
            narrator.SnapAllCharactersToSpawn(2);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Time-specific Setup
        EditorGUILayout.LabelField("Time-specific Setup:", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Morning Setup"))
        {
            narrator.SetupMorningPositions();
            // Update UI to reflect the positions
            motherSpawnIndex = 0;
            fatherSpawnIndex = 0;
            babySpawnIndex = 0;
            bidanSpawnIndex = 1;
        }
        if (GUILayout.Button("Afternoon Setup"))
        {
            narrator.SetupAfternoonPositions();
            // Update UI to reflect the positions
            motherSpawnIndex = 1;
            fatherSpawnIndex = 2;
            babySpawnIndex = 1;
            bidanSpawnIndex = 3;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Evening Setup"))
        {
            narrator.SetupEveningPositions();
            // Update UI to reflect the positions
            motherSpawnIndex = 2;
            fatherSpawnIndex = 2;
            babySpawnIndex = 2;
            bidanSpawnIndex = 4;
        }
        if (GUILayout.Button("Default Day 2"))
        {
            // Update UI to reflect the positions
            motherSpawnIndex = 0;
            fatherSpawnIndex = 0;
            babySpawnIndex = 1;
            bidanSpawnIndex = 2;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Reset button
        if (GUILayout.Button("Reset All Characters to Spawn 0", GUILayout.Height(25)))
        {
            narrator.ResetAllCharacterPositions();
            // Reset UI values
            motherSpawnIndex = fatherSpawnIndex = babySpawnIndex = bidanSpawnIndex = 0;
        }
        
        EditorGUILayout.Space(5);
        
        // Instructions
        EditorGUILayout.LabelField("Instructions:", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox(
            "IMPROVED FEATURES:\n" +
            "• Individual spawn index control for each character\n" +
            "• Time-specific position presets (Morning, Afternoon, Evening)\n" +
            "• Multi-character snap with different indices\n" +
            "• Spawn position validation\n" +
            "• Quick preset buttons for common scenarios\n\n" +
            "USAGE:\n" +
            "1. Use sliders to set individual spawn indices\n" +
            "2. Click 'Snap to X' buttons for individual characters\n" +
            "3. Use time-specific setup buttons for story sequences\n" +
            "4. Validate spawn positions before testing", 
            MessageType.Info
        );
        
        // Show current spawn info
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Current Settings:", EditorStyles.miniBoldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.LabelField($"Mother: Spawn {motherSpawnIndex}, Father: Spawn {fatherSpawnIndex}");
        EditorGUILayout.LabelField($"Baby: Spawn {babySpawnIndex}, Bidan: Spawn {bidanSpawnIndex}");
        EditorGUI.EndDisabledGroup();
        
        // === SEPARATOR ===
        EditorGUILayout.Space(10);
        GUILayout.Box("", new GUILayoutOption[]{GUILayout.ExpandWidth(true), GUILayout.Height(1)});
        EditorGUILayout.Space(5);
        
        // === DEFAULT INSPECTOR AT BOTTOM ===
        DrawDefaultInspector();
        
        // Save changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(narrator);
        }
    }
}
#endif
