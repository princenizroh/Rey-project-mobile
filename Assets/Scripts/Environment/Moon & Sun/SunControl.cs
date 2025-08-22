using UnityEngine;

public class SunControl : MonoBehaviour
{
    public float distance = -1000.0f;
    public float scale = 15.0f;

    void Start()
    {
        // Mengatur posisi matahari berdasarkan jarak positif
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, -distance);
        
        // Mengatur skala matahari
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
