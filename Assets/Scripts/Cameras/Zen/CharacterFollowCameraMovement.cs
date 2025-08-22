using Unity.Cinemachine;
using UnityEngine;

public class CharacterFollowCameraMovement : MonoBehaviour
{
    [System.Obsolete]
    private CinemachineVirtualCamera mainCamera;
    public GameObject playerModel;
    private bool allowCamMovement = true;

    [System.Obsolete]
    void Start()
    {
        mainCamera = FindObjectOfType<CinemachineVirtualCamera>();
        if (mainCamera == null)
        {
            Debug.LogError("CinemachineCamera not found in the scene.");
        }

    }

    [System.Obsolete]
    void Update()
    {
        if (allowCamMovement)
        {
            if (playerModel != null && mainCamera != null)
            {
                // Only rotate the player to match the camera's Y (yaw) rotation
                var cameraY = mainCamera.transform.rotation.eulerAngles.y;
                var currentEuler = playerModel.transform.rotation.eulerAngles;
                playerModel.transform.rotation = Quaternion.Euler(0f, cameraY, 0f);
            }
        }

    }

    public void changePlayerTarget(string playerTag)
    {
        playerModel = GameObject.FindGameObjectWithTag("Player");
        if (playerModel == null)
        {
            Debug.LogError("PlayerModel not found in the scene.");
        }
        else
        {
            Debug.Log("PlayerModel found: " + playerModel.name);
        }
    }
    
    public void changePlayerMovement(bool status)
    {
        allowCamMovement = status;
        if (allowCamMovement)
        {
            Debug.Log("Player movement enabled.");
        }
        else
        {
            Debug.Log("Player movement disabled.");
        }
    }

}
