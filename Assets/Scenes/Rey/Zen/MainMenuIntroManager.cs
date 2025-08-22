using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuIntroManager : MonoBehaviour
{
    public GameObject MainMenuMainframe;
    public Image MainMenuDimmer;
    public GameObject MainMenuDimmer_Gameobject;
    public GameObject infolog;
    public GameObject devLogo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Starting phase
        MainMenuMainframe.SetActive(true);
        MainMenuDimmer_Gameobject.SetActive(false);

        // Start the coroutine to handle the timer and opacity change
        StartCoroutine(IntroDimmerAfterDelay());
    }

    // Coroutine to handle the 5-second delay and opacity change
    private IEnumerator ActivateDimmerAfterDelay()
    {
        yield return new WaitForSeconds(1.5f); // Wait for 5 seconds

        MainMenuDimmer_Gameobject.SetActive(true);

        // Animate the opacity of MainMenuDimmer with ease-out effect
        if (MainMenuDimmer != null)
        {
            Color dimmerColor = MainMenuDimmer.color;
            float duration = 1f; // Animation duration in seconds
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic interpolation
                dimmerColor.a = Mathf.Lerp(0f, 1f, t);
                MainMenuDimmer.color = dimmerColor;
                yield return null;
            }

            // Ensure the final opacity is set to 100%
            dimmerColor.a = 1f;
            MainMenuDimmer.color = dimmerColor;
            devLogo.SetActive(false);
        }

        infolog.SetActive(true);
        yield return new WaitForSeconds(1.5f);

        Color dimmerColor2 = MainMenuDimmer.color;
        dimmerColor2.a = 1f; // Start with full opacity
        float duration2 = 1f; // Animation duration in seconds
        float elapsedTime2 = 0f;

        while (elapsedTime2 < duration2)
        {
            elapsedTime2 += Time.deltaTime;
            float t = elapsedTime2 / duration2;
            t = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic interpolation
            dimmerColor2.a = Mathf.Lerp(1f, 0f, t);
            MainMenuDimmer.color = dimmerColor2;
            yield return null;
        }

        dimmerColor2.a = 0f;
        MainMenuDimmer.color = dimmerColor2;

        yield return new WaitForSeconds(1.5f);

        MainMenuDimmer_Gameobject.SetActive(true);

        // Animate the opacity of MainMenuDimmer with ease-out effect
        if (MainMenuDimmer != null)
        {
            Color dimmerColor = MainMenuDimmer.color;
            float duration = 1f; // Animation duration in seconds
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic interpolation
                dimmerColor.a = Mathf.Lerp(0f, 1f, t);
                MainMenuDimmer.color = dimmerColor;
                yield return null;
            }

            // Ensure the final opacity is set to 100%
            dimmerColor.a = 1f;
            MainMenuDimmer.color = dimmerColor;
            devLogo.SetActive(false);
            infolog.SetActive(true);
        }

        goToMainMenu();
    }
    
    private IEnumerator IntroDimmerAfterDelay()
    {
        MainMenuDimmer_Gameobject.SetActive(true);

        // Animate the opacity of MainMenuDimmer with ease-out effect
        if (MainMenuDimmer != null)
        {
            Color dimmerColor = MainMenuDimmer.color;
            dimmerColor.a = 1f; // Start with full opacity
            float duration = 1f; // Animation duration in seconds
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic interpolation
                dimmerColor.a = Mathf.Lerp(1f, 0f, t);
                MainMenuDimmer.color = dimmerColor;
                yield return null;
            }

            dimmerColor.a = 0f;
            MainMenuDimmer.color = dimmerColor;

            MainMenuDimmer_Gameobject.SetActive(false);
            StartCoroutine(ActivateDimmerAfterDelay());
        }
    }

    private void goToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // Update is called once per frame
    void Update()
    {
    }
}
