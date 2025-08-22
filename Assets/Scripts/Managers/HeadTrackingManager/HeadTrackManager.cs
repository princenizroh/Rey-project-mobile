using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;

public class HeadTrackingManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterHeadRig
    {
        [Header("Character Info")]
        public CharacterType characterType;
        public MultiAimConstraint headConstraint;
        
        [Header("Default Settings")]
        [Range(0f, 1f)]
        public float defaultWeight = 1f;
        [Tooltip("Which target index should be active by default (-1 for none)")]
        public int defaultTargetIndex = -1;
    }

    public CharacterHeadRig[] characterHeadRig;
    public CharacterType activeCharacterType;
    public float transitionDuration = 0.5f;
    
    [Header("Global Settings")]
    [Range(0f, 1f)]
    public float globalDefaultWeight = 1f;
    public bool initializeOnStart = true;

    void Start()
    {
        if (initializeOnStart)
        {
            InitializeDefaultWeights();
        }
    }

    /// <summary>
    /// Initialize all character head rigs with their default weights
    /// </summary>
    [ContextMenu("Initialize Default Weights")]
    public void InitializeDefaultWeights()
    {
        foreach (var rig in characterHeadRig)
        {
            if (rig.headConstraint != null)
            {
                var sources = rig.headConstraint.data.sourceObjects;
                
                // Reset all weights to 0 first
                for (int i = 0; i < sources.Count; i++)
                {
                    sources.SetWeight(i, 0f);
                }
                
                // Set default target if specified
                if (rig.defaultTargetIndex >= 0 && rig.defaultTargetIndex < sources.Count)
                {
                    float weightToUse = rig.defaultWeight > 0 ? rig.defaultWeight : globalDefaultWeight;
                    sources.SetWeight(rig.defaultTargetIndex, weightToUse);
                    Debug.Log($"[HeadTrackingManager] {rig.characterType} default target: {rig.defaultTargetIndex} with weight: {weightToUse}");
                }
                
                rig.headConstraint.data.sourceObjects = sources;
            }
        }
        
        Debug.Log("[HeadTrackingManager] Default weights initialized!");
    }

    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Q))
    //     {
    //         StartCoroutine(SmoothSetHeadTarget(activeCharacterType, CharacterTarget.Mother));
    //     }
    //     else if (Input.GetKeyDown(KeyCode.W))
    //     {
    //         StartCoroutine(SmoothSetHeadTarget(activeCharacterType, CharacterTarget.Father));
    //     }
    // }

    public void SetHeadTarget(CharacterType type, CharacterTarget target)
    {
        StartCoroutine(SmoothSetHeadTarget(type, target));
    }

    /// <summary>
    /// Set head target with custom weight
    /// </summary>
    public void SetHeadTarget(CharacterType type, CharacterTarget target, float weight)
    {
        StartCoroutine(SmoothSetHeadTarget(type, target, weight));
    }

    /// <summary>
    /// Set head target immediately without smooth transition
    /// </summary>

    /// <summary>
    /// Reset all characters head tracking to neutral - sets ALL possible combinations to 0 weight
    /// </summary>
    public void ResetAllHeadTracking()
    {
        // Reset ALL possible CharacterType -> CharacterTarget combinations to 0 weight
        CharacterType[] allCharacters = { CharacterType.Mother, CharacterType.Father, CharacterType.Bidan, CharacterType.Baby, CharacterType.Ghost };
        CharacterTarget[] allTargets = { CharacterTarget.Mother, CharacterTarget.Father, CharacterTarget.Bidan, CharacterTarget.Baby, CharacterTarget.Object, CharacterTarget.Ghost };
        
        foreach (var character in allCharacters)
        {
            foreach (var target in allTargets)
            {
                SetHeadTarget(character, target, 0f);
            }
        }
        
        Debug.Log("[HeadTrackingManager] All head tracking combinations reset to 0 weight");
    }

    /// <summary>
    /// Reset specific character head tracking to neutral - sets all targets for one character to 0 weight
    /// </summary>
    public void ResetCharacterHeadTracking(CharacterType characterType)
    {
        CharacterTarget[] allTargets = { CharacterTarget.Mother, CharacterTarget.Father, CharacterTarget.Bidan, CharacterTarget.Baby, CharacterTarget.Object, CharacterTarget.Ghost };
        
        foreach (var target in allTargets)
        {
            SetHeadTarget(characterType, target, 0f);
        }
        
        Debug.Log($"[HeadTrackingManager] {characterType} head tracking reset to 0 weight for all targets");
    }

    public IEnumerator SmoothSetHeadTarget(CharacterType type, CharacterTarget target)
    {
        foreach (var rig in characterHeadRig)
        {
            if (rig.characterType != type) continue;

            var sources = rig.headConstraint.data.sourceObjects;
            int targetIndex = (int)target;
            float time = 0f;

            // Capture initial weights
            float[] initialWeights = new float[sources.Count];
            for (int i = 0; i < sources.Count; i++)
                initialWeights[i] = sources.GetWeight(i);

            while (time < transitionDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / transitionDuration);

                for (int i = 0; i < sources.Count; i++)
                {
                    float targetWeight = (i == targetIndex) ? 1f : 0f;
                    float newWeight = Mathf.Lerp(initialWeights[i], targetWeight, t);
                    sources.SetWeight(i, newWeight);
                }

                rig.headConstraint.data.sourceObjects = sources;
                yield return null;
            }

            // Finalize weights
            for (int i = 0; i < sources.Count; i++)
                sources.SetWeight(i, i == targetIndex ? 1f : 0f);

            rig.headConstraint.data.sourceObjects = sources;
            break;
        }
    }
    
    /// <summary>
    /// Smooth head target transition with custom weight
    /// </summary>
    public IEnumerator SmoothSetHeadTarget(CharacterType type, CharacterTarget target, float customWeight)
    {
        foreach (var rig in characterHeadRig)
        {
            if (rig.characterType != type) continue;

            var sources = rig.headConstraint.data.sourceObjects;
            int targetIndex = (int)target;
            float time = 0f;

            // Capture initial weights
            float[] initialWeights = new float[sources.Count];
            for (int i = 0; i < sources.Count; i++)
                initialWeights[i] = sources.GetWeight(i);

            while (time < transitionDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / transitionDuration);

                for (int i = 0; i < sources.Count; i++)
                {
                    float targetWeight = (i == targetIndex) ? customWeight : 0f;
                    float newWeight = Mathf.Lerp(initialWeights[i], targetWeight, t);
                    sources.SetWeight(i, newWeight);
                }

                rig.headConstraint.data.sourceObjects = sources;
                yield return null;
            }

            // Finalize weights
            for (int i = 0; i < sources.Count; i++)
                sources.SetWeight(i, i == targetIndex ? customWeight : 0f);

            rig.headConstraint.data.sourceObjects = sources;
            break;
        }
    }
}
