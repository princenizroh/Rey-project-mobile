using UnityEngine;
using System.Collections.Generic;
using static CoreGameDialog;

[System.Serializable]
public struct AnimationSchedule
{
    public NpcName npcName;
    public NarratorDay day;
    public TimeOfDay time;
    public string animationName;
}

[System.Serializable]
public struct AnimationResetSchedule
{
    public NpcName npcName;
    public NarratorDay day;
    public TimeOfDay time;
    public string resetAnimationName;
}

public class DialogCharacterAnimatorManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterAnimatorEntry
    {
        public NpcName npcName;
        public Animator animator;
    }

    [Header("Assign all character animators here")]
    public List<CharacterAnimatorEntry> characterAnimators;

    [Header("Define animation per day and time")]
    public List<AnimationSchedule> animationSchedules;

    [Header("Define reset animation per day and time")]
    public List<AnimationResetSchedule> resetSchedules;

    private NpcName? lastNpc = null;

    public static class NarrativeState
    {
        public static NarratorDay CurrentDay = NarratorDay.Day1;
        public static TimeOfDay CurrentTime = TimeOfDay.Night;
    }

    public void HandleCharacterAnimation(NpcName npc)
    {
        if (NarratorManager.Instance == null)
        {
            Debug.LogError("NarratorManager.Instance is null!");
            return;
        }

        NarratorDay day = NarratorManager.Instance.currentDay;
        TimeOfDay time = NarratorManager.Instance.currentTime;

        // Step 1: Reset semua NPC kecuali yang sedang bicara
        PlayResetAnimations(day, time, excludeNpc: npc);

        // Step 2: Mainkan animasi untuk NPC yang sedang bicara
        PlayAnimation(npc);
    }

    public void PlayAnimation(NpcName npc)
    {
        if (NarratorManager.Instance == null)
        {
            Debug.LogError("NarratorManager.Instance is null!");
            return;
        }

        NarratorDay day = NarratorManager.Instance.currentDay;
        TimeOfDay time = NarratorManager.Instance.currentTime;

        string animationName = GetAnimationName(npc, day, time);

        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"No animation defined for {npc} on {day} at {time}.");
            return;
        }

        Animator animator = GetAnimator(npc);
        if (animator != null)
        {
            animator.Play(animationName);
            Debug.Log($"Playing animation '{animationName}' for NPC '{npc}' on {day} {time}");
            lastNpc = npc;
        }
        else
        {
            Debug.LogWarning($"Animator not found for NPC '{npc}'");
        }
    }

    public void PlayResetAnimations(NarratorDay day, TimeOfDay time, NpcName? excludeNpc = null)
    {
        foreach (var reset in resetSchedules)
        {
            if (reset.day == day && reset.time == time)
            {
                if (excludeNpc.HasValue && reset.npcName == excludeNpc.Value)
                    continue;

                Animator animator = GetAnimator(reset.npcName);
                if (animator != null)
                {
                    animator.Play(reset.resetAnimationName);
                    Debug.Log($"Resetting '{reset.npcName}' with animation '{reset.resetAnimationName}' for {day} {time}");
                }
                else
                {
                    Debug.LogWarning($"Animator not found for NPC '{reset.npcName}' during reset");
                }
            }
        }
    }

    public void ResetToIdle(NpcName npc)
    {
        Animator animator = GetAnimator(npc);
        if (animator != null)
        {
            animator.Play("Idle");
        }
    }

    public void ResetAllToIdle()
    {
        foreach (var entry in characterAnimators)
        {
            if (entry.animator != null)
            {
                entry.animator.Play("Idle");
            }
        }
        lastNpc = null;
    }

    private Animator GetAnimator(NpcName npc)
    {
        foreach (var entry in characterAnimators)
        {
            if (entry.npcName == npc)
                return entry.animator;
        }
        return null;
    }

    private string GetAnimationName(NpcName npc, NarratorDay day, TimeOfDay time)
    {
        foreach (var schedule in animationSchedules)
        {
            if (schedule.npcName == npc && schedule.day == day && schedule.time == time)
            {
                return schedule.animationName;
            }
        }

        Debug.LogWarning($"No animation found for NPC '{npc}' on {day} {time}");
        return null;
    }
}