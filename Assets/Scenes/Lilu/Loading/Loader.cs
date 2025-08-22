using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lilu
{
    public class Loader : MonoBehaviour
    {
      public static Loader Instance;

      [SerializeField] private GameObject _loaderCanvas;
      [SerializeField] private GameObject _destroyCanvas;
      [SerializeField] private Image _progressBar;


      public void LoadScene(string sceneName)
      {
        StartCoroutine(LoadSceneAsync(sceneName));
        Destroy(_destroyCanvas);
      }


      public void LoadSceneContinue(string sceneName)
      {
        StartCoroutine(LoadSceneAsync(sceneName));
        Destroy(_destroyCanvas);
      }

      private IEnumerator LoadSceneAsync(string sceneName)
      {
          AsyncOperation scene = SceneManager.LoadSceneAsync(sceneName);
          scene.allowSceneActivation = false;

          _loaderCanvas.SetActive(true);

          while (scene.progress < 0.9f)
          {
              _progressBar.fillAmount = scene.progress;
              yield return null;
          }

          scene.allowSceneActivation = true;
          _loaderCanvas.SetActive(false);

          // Destroy(_loaderCanvas);
      }

    }
}
