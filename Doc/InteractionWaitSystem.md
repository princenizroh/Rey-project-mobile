# Interaction Wait System Documentation

## Overview
The Interaction Wait System allows your narrative sequences (like NarratorDay1) to pause and wait for player interactions with objects in the scene before continuing. This solves the problem where linear narrative sequences would skip over interaction-dependent events.

## Key Components

### 1. InteractionWaitManager
- **Purpose**: Central manager that handles waiting for player interactions
- **Location**: `Assets/Scripts/Managers/InteractionWaitManager.cs`
- **Singleton**: Automatically manages itself as a singleton across scenes

### 2. Modified NarratorBase
- **New Methods**: Added interaction waiting methods to the base narrator class
- **Location**: `Assets/Scripts/Managers/NaratorManager/NarratorBase.cs`

### 3. Modified RaycastObjectCam
- **Integration**: Now notifies the InteractionWaitManager when interactions occur
- **Location**: `Assets/Scripts/Core/Raycast/RaycastObject.cs`

### 4. InteractionWaitSetup
- **Purpose**: Helper script to automatically setup the InteractionWaitManager
- **Location**: `Assets/Scripts/Setup/InteractionWaitSetup.cs`

## How It Works

1. **Narrator registers a dialog interaction**: "Wait for player to interact with Linda_Model and play 'GameData/Dialog/Day1/LindaDialog'"
2. **Narrative pauses**: The sequence stops and waits
3. **Player interacts**: Presses E while looking at Linda_Model
4. **RaycastObjectCam executes dialog**: Automatically starts the dialog sequence and waits for completion
5. **Dialog completes**: The dialog system finishes playing
6. **RaycastObjectCam notifies**: Tells InteractionWaitManager the interaction (including dialog) is complete
7. **Narrative continues**: The sequence resumes from where it paused

## Key Features

- **Automatic Dialog Execution**: When player presses E, the system automatically starts and waits for dialog completion
- **Seamless Integration**: Works with existing CoreGameManager dialog system
- **Multiple Interaction Types**: Support for simple interactions and dialog-based interactions
- **Flexible Waiting**: Can wait for specific objects or any interaction

## Setup Instructions

### Step 1: Add InteractionWaitManager to Scene
Option A - Automatic (Recommended):
```csharp
// Add InteractionWaitSetup component to any GameObject in your scene
// It will automatically create the InteractionWaitManager
```

Option B - Manual:
```csharp
// Create an empty GameObject named "InteractionWaitManager"
// Add the InteractionWaitManager component to it
```

### Step 2: Update Your Narrator Scripts
Replace your existing NarratorDay1.cs or create new narrator scripts using the new wait methods:

```csharp
// Instead of this linear approach:
yield return new WaitForSeconds(1f);
DoSomething();

// Use this interactive approach:
uiElements.narratorText.text = "Interact with Linda to continue...";
yield return StartCoroutine(WaitForPlayerInteraction("linda_interaction", "Linda_Model"));
uiElements.narratorText.text = "";
DoSomething();
```

## Available Methods in NarratorBase

### WaitForDialogInteraction (NEW - For Dialog Sequences)
```csharp
protected IEnumerator WaitForDialogInteraction(string waitId, string targetObjectName, string dialogPath, float timeoutSeconds = 0f)
```
- **waitId**: Unique identifier for this wait
- **targetObjectName**: Exact name of the GameObject to interact with
- **dialogPath**: Path to dialog file (e.g., "GameData/Dialog/Day1/Seq1DalamPerut")
- **timeoutSeconds**: Optional timeout (0 = wait forever)

**This is the main method for dialog-based interactions!**

### RegisterDialogForObject
```csharp
protected void RegisterDialogForObject(string objectName, string dialogPath)
```
- Register a dialog to play when player interacts with an object
- Used for setting up multiple objects with different dialogs

### WaitForPlayerInteraction
```csharp
protected IEnumerator WaitForPlayerInteraction(string waitId, string targetObjectName, string interactionType = "default", float timeoutSeconds = 0f)
```
- **waitId**: Unique identifier for this wait
- **targetObjectName**: Exact name of the GameObject to interact with
- **interactionType**: Type of interaction ("default", "keypress", etc.)
- **timeoutSeconds**: Optional timeout (0 = wait forever)

### WaitForAnyPlayerInteraction
```csharp
protected IEnumerator WaitForAnyPlayerInteraction(string waitId, float timeoutSeconds = 0f)
```
- Waits for interaction with any object
- Useful for exploration phases

### RegisterInteractionWait
```csharp
protected void RegisterInteractionWait(string waitId, string targetObjectName, string interactionType = "default", System.Action onCompleted = null, float timeoutSeconds = 0f)
```
- Registers a wait condition without immediately waiting
- Useful for complex scenarios with multiple conditions

### Other Helper Methods
- `CancelInteractionWait(string waitId)`: Cancel a wait condition
- `IsInteractionWaitCompleted(string waitId)`: Check if completed

## Usage Examples

### Example 1: Dialog Interaction (Most Common Use Case)
```csharp
// Wait for player to interact with Linda_Model and play a specific dialog
uiElements.narratorText.text = "Talk to Linda to continue...";
yield return StartCoroutine(WaitForDialogInteraction("linda_dialog", "Linda_Model", "GameData/Dialog/Day1/LindaDialog"));
uiElements.narratorText.text = "";
// The dialog will automatically play and complete before continuing
```

### Example 2: Multiple Objects with Different Dialogs
```csharp
// Set up multiple objects with different dialogs
RegisterDialogForObject("Mother", "GameData/Dialog/Day1/MotherDialog");
RegisterDialogForObject("Father", "GameData/Dialog/Day1/FatherDialog");
RegisterDialogForObject("Bidan", "GameData/Dialog/Day1/BidanDialog");

uiElements.narratorText.text = "Explore and talk to people around you...";

// Wait for any interaction (any of the above dialogs to complete)
yield return StartCoroutine(WaitForAnyPlayerInteraction("exploration_phase", 60f));

// Clean up
UnregisterDialogForObject("Mother");
UnregisterDialogForObject("Father");
UnregisterDialogForObject("Bidan");
```

### Example 3: Sequential Dialog Requirements
```csharp
// Player must talk to Mother first
uiElements.narratorText.text = "Talk to your mother first...";
yield return StartCoroutine(WaitForDialogInteraction("mother_first", "Mother", "GameData/Dialog/Day1/MotherFirst"));

// Then talk to Father
uiElements.narratorText.text = "Now talk to your father...";
yield return StartCoroutine(WaitForDialogInteraction("father_second", "Father", "GameData/Dialog/Day1/FatherSecond"));

uiElements.narratorText.text = "";
// Continue with story...
```

### Example 4: Simple Object Interaction (No Dialog)
```csharp
// Wait for player to interact with a cradle (no dialog, just simple interaction)
uiElements.narratorText.text = "Examine the cradle...";
yield return StartCoroutine(WaitForPlayerInteraction("cradle_interaction", "Baby_Cradle"));
uiElements.narratorText.text = "";
// Continue with narrative...
```

### Example 2: Multiple Choice Interaction
```csharp
// Register multiple options
RegisterInteractionWait("choose_mother", "Mother", "keypress");
RegisterInteractionWait("choose_father", "Father", "keypress");

uiElements.narratorText.text = "Choose who to approach...";

// Wait for either interaction
yield return new WaitUntil(() => 
    IsInteractionWaitCompleted("choose_mother") || 
    IsInteractionWaitCompleted("choose_father"));

// Check which was chosen and act accordingly
if (IsInteractionWaitCompleted("choose_mother"))
{
    // Mother was chosen
    Debug.Log("Player chose mother");
}
else
{
    // Father was chosen
    Debug.Log("Player chose father");
}

// Cancel remaining waits
CancelInteractionWait("choose_mother");
CancelInteractionWait("choose_father");
```

### Example 3: Timed Interaction
```csharp
// Wait for interaction with 30-second timeout
uiElements.narratorText.text = "You have 30 seconds to examine the room...";
yield return StartCoroutine(WaitForAnyPlayerInteraction("explore_timeout", 30f));

if (IsInteractionWaitCompleted("explore_timeout"))
{
    uiElements.narratorText.text = "Good, you found something interesting.";
}
else
{
    uiElements.narratorText.text = "Time's up! Moving on...";
}
```

## Migrating Existing NarratorDay Scripts

### Option 1: Create New Scripts (Recommended)
1. Copy your existing NarratorDay1.cs to NarratorDay1Interactive.cs
2. Add interaction waits where needed
3. Update the NarratorManager to use the new script

### Option 2: Conditional Waiting
If you must keep the original NarratorDay1.cs unchanged, you can create a wrapper:

```csharp
public class NarratorDay1Wrapper : MonoBehaviour
{
    public NarratorDay1 originalNarrator;
    
    public IEnumerator StartInteractiveNarration()
    {
        // Pre-interaction setup
        yield return StartCoroutine(WaitForPlayerInteraction("start_interaction", "StartObject"));
        
        // Run original sequence
        yield return StartCoroutine(originalNarrator.StartNarration());
        
        // Post-interaction cleanup
        yield return StartCoroutine(WaitForPlayerInteraction("end_interaction", "EndObject"));
    }
}
```

## Troubleshooting

### Common Issues

1. **"InteractionWaitManager not found" Error**
   - Make sure InteractionWaitSetup is added to a GameObject in the scene
   - Or manually create InteractionWaitManager GameObject

2. **Interactions Not Detected**
   - Ensure objects have "RaycastObject" tag
   - Check that RaycastObjectCam is properly configured
   - Verify LayerMask settings on the raycast system

3. **Narrative Gets Stuck**
   - Use timeout parameters to prevent infinite waiting
   - Check that waitId strings match exactly
   - Use debug logs to trace execution

### Debug Tips

1. Enable debug logs in InteractionWaitManager
2. Use unique waitId strings for each wait condition
3. Check the console for interaction notifications

## Best Practices

1. **Use Descriptive Wait IDs**: `"day1_linda_first_meeting"` instead of `"wait1"`
2. **Provide Player Feedback**: Always show UI text explaining what to do
3. **Use Timeouts**: Prevent players from getting stuck
4. **Clear Old Waits**: Cancel unused wait conditions to prevent memory leaks
5. **Test Thoroughly**: Test all interaction paths and edge cases

## Integration with Existing Systems

This system is designed to work alongside your existing:
- Dialog systems (CoreGameManager)
- Animation systems
- Audio systems
- Character movement systems

It simply adds the ability to pause and wait for interactions without affecting other functionality.
