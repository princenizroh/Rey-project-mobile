using UnityEngine;

[CreateAssetMenu(fileName = "CoreGameSaves", menuName = "Scriptable Objects/CoreGameSaves")]
public class CoreGameSaves : ScriptableObject
{
    public int day;
    public TimeOfDay timeOfDay;
    public int mother_stress_level;
}
