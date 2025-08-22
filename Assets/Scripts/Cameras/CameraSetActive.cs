using System;
using UnityEngine;

namespace DS 
{
    public class CameraSetActive : MonoBehaviour
    {
        private CameraUIMovement cameraUIMovement;
        public GameObject[] cameras;
        public GameObject startCamera;
        private GameObject currentCam;

        private void Start()
        {
            // Initialize CameraUIMovement component
            cameraUIMovement = FindFirstObjectByType<CameraUIMovement>();
            if (cameraUIMovement == null)
            {
                Debug.LogError("CameraUIMovement component not found in scene!");
            }

            // Set kamera awal
            currentCam = startCamera;

            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].SetActive(cameras[i] == currentCam);
            }

            // Initialize UI camera with the starting camera
            if (cameraUIMovement != null && currentCam != null)
            {
                Camera startCameraComponent = currentCam.GetComponent<Camera>();
                if (startCameraComponent != null)
                {
                    cameraUIMovement.changeUICamera(startCameraComponent);
                }
                else
                {
                    Debug.LogError("Start camera GameObject does not have a Camera component!");
                }
            }
        }

        public void SwitchCamera(int cameraIndex)
        {
            if (cameraIndex < 0 || cameraIndex >= cameras.Length)
            {
                Debug.LogError($"Kamera dengan indeks {cameraIndex} tidak valid!");
                return;
            }

            // Matikan kamera saat ini
            currentCam.SetActive(false);

            // Aktifkan kamera baru
            currentCam = cameras[cameraIndex];
            currentCam.SetActive(true);

            // Update UI camera if CameraUIMovement is available
            if (cameraUIMovement != null)
            {
                Camera newCameraComponent = currentCam.GetComponent<Camera>();
                if (newCameraComponent != null)
                {
                    cameraUIMovement.changeUICamera(newCameraComponent);
                }
                else
                {
                    Debug.LogError($"Camera GameObject at index {cameraIndex} does not have a Camera component!");
                }
            }
            else
            {
                Debug.LogWarning("CameraUIMovement component is not initialized!");
            }
        }
    }
}

