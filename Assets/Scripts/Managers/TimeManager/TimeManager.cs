using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class TimeManager : MonoBehaviour
{
    public static TimeManager instance { get; private set; }
        [SerializeField] private Texture2D skyboxNight;
        [SerializeField] private Texture2D skyboxSunrise;
        [SerializeField] private Texture2D skyboxDay;
        [SerializeField] private Texture2D skyboxSunset;

        [SerializeField] private Gradient gradientNightToSunrise;
        [SerializeField] private Gradient gradientSunriseToDay;
        [SerializeField] private Gradient gradientDayToSunset;
        [SerializeField] private Gradient gradientSunsetToNight;

        [SerializeField] private Light globalLightDay;
        [SerializeField] private Light globalLightNight;

        [SerializeField, Range(0, 24)] private float timeOfDay;
        [SerializeField] private bool isTimeOfDayEnabled = false;
        [SerializeField] private float sunRotationSpeed;

        public float TimeOfDay
        {
            get { return timeOfDay; }
            set
            {
                timeOfDay = value;
                UpdateTime();
            }
        }
        private void Start()
        {
            // TimeOfDay = 6.0f; // 6 AM
            DetectScene();
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void DetectScene()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            switch (sceneName)
            {
                case "Builder House":
                    break;
            }
        }

        private void Update()
        {
            if (isTimeOfDayEnabled == true)
            {
                SetTimeOfDay();
            }
        }

        public void SetTimeOfDay()
        {
            // Update TimeOfDay based on sun rotation speed
            TimeOfDay += sunRotationSpeed * Time.deltaTime;

            if (TimeOfDay >= 24)
            {
                // Reset TimeOfDay to 0 and advance to the next day
                TimeOfDay = 0.0f;
            }

        }


        private void UpdateTime()
        {
            float timeInHours = timeOfDay % 24f;
            int hours = Mathf.FloorToInt(timeInHours);
            int minutes = Mathf.FloorToInt((timeInHours - hours) * 60);

            Quaternion lightRotation = Quaternion.Euler((timeInHours / 24f) * 360f - 90f, 170f, 0);
            globalLightDay.transform.rotation = lightRotation;
            globalLightNight.transform.rotation = lightRotation * Quaternion.Euler(0, 180, 0);

            UpdateSkyboxAndLight(hours);

            // Aktifkan atau nonaktifkan Global Light berdasarkan waktu
            if (hours >= 6 && hours < 19)
            {
                globalLightDay.enabled = true;
                globalLightNight.enabled = false;
            }
            else
            {
                globalLightDay.enabled = false;
                globalLightNight.enabled = true;
            }
        }

        private void UpdateSkyboxAndLight(int hours)
        {
            if (hours >= 5 && hours < 8)
            {
                LerpSkybox(skyboxNight, skyboxSunrise, gradientNightToSunrise, hours);
            }
            else if (hours >= 8 && hours < 17)
            {
                LerpSkybox(skyboxSunrise, skyboxDay, gradientSunriseToDay, hours);
            }
            else if (hours >= 17 && hours < 19)
            {
                LerpSkybox(skyboxDay, skyboxSunset, gradientDayToSunset, hours);
            }
            else if (hours >= 19 && hours < 24)
            {
                LerpSkybox(skyboxSunset, skyboxNight, gradientSunsetToNight, hours);
            }
            else
            {
                LerpSkybox(skyboxNight, skyboxNight, gradientSunsetToNight, hours); // Handle transition between night and early morning
            }
        }

        private void LerpSkybox(Texture2D a, Texture2D b, Gradient lightGradient, int hours)
        {
            float blendFactor = 0f;

            if (hours >= 5 && hours < 8)
            {
                blendFactor = Mathf.InverseLerp(5, 8, timeOfDay);
            }
            else if (hours >= 8 && hours < 17)
            {
                blendFactor = Mathf.InverseLerp(8, 17, timeOfDay);
            }
            else if (hours >= 17 && hours < 19)
            {
                blendFactor = Mathf.InverseLerp(17, 19, timeOfDay);
            }
            else if (hours >= 19 && hours < 24)
            {
                blendFactor = Mathf.InverseLerp(19, 24, timeOfDay);
            }
            else
            {
                blendFactor = Mathf.InverseLerp(24, 5, timeOfDay); // Handle transition between night and early morning
            }

            RenderSettings.skybox.SetTexture("_Texture1", a);
            RenderSettings.skybox.SetTexture("_Texture2", b);
            RenderSettings.skybox.SetFloat("_Blend", blendFactor);

            globalLightDay.color = lightGradient.Evaluate(blendFactor);
            globalLightNight.color = lightGradient.Evaluate(blendFactor);
            RenderSettings.fogColor = globalLightDay.color;
        }

}
