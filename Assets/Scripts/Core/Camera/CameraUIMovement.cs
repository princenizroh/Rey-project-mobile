using UnityEngine;

public class CameraUIMovement : MonoBehaviour
{
    public Canvas canvasMainObject;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void changeUICamera(Camera camera)
    {
        if (canvasMainObject.renderMode == RenderMode.ScreenSpaceCamera)
        {
            canvasMainObject.worldCamera = camera;
        }
    }
}
