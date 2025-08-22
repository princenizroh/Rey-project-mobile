using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class SwitchLights : MonoBehaviour
{
    public static SwitchLights Instance;
    [Header("Lightmap Textures")]
    public Texture2D[] darkLightmapDir, darkLightmapColor;
    public Texture2D[] brightLightmapDir, brightLightmapColor;

    private LightmapData[] darkLightmap, brightLightmap;

    [Header("Optional Realtime Lights")]
    public Light[] lights;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        darkLightmap = BuildLightmapArray(darkLightmapDir, darkLightmapColor);
        brightLightmap = BuildLightmapArray(brightLightmapDir, brightLightmapColor);
    }

    private LightmapData[] BuildLightmapArray(Texture2D[] dir, Texture2D[] color)
    {
        List<LightmapData> result = new List<LightmapData>();
        for (int i = 0; i < dir.Length; i++)
        {
            LightmapData lmdata = new LightmapData
            {
                lightmapDir = dir[i],
                lightmapColor = color[i]
            };
            result.Add(lmdata);
        }
        return result.ToArray();
    }

    public IEnumerator SwitchToDark()
    {
        LightmapSettings.lightmaps = darkLightmap;
        yield return null;
        Debug.Log("Switched to dark lightmap");
    }


    public IEnumerator SwitchToBright()
    {
        LightmapSettings.lightmaps = brightLightmap;
        yield return null;
        Debug.Log("Switched to bright lightmap");
    }

    public void ToggleLight(int index)
    {
        if (index >= 0 && index < lights.Length)
        {
            lights[index].enabled = !lights[index].enabled;
        }
    }
}
