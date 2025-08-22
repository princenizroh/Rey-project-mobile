using UnityEngine;

public class MoonControl : MonoBehaviour
{
    public float distance = 1000.0f;
    public float scale = 15.0f;

    void Start()
    {
        // Mengatur posisi bulan berdasarkan jarak negatif
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, -distance);
        
        // Mengatur skala bulan
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
