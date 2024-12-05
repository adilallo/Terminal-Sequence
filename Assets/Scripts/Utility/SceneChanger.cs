using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utility
{
    public class SceneChanger : MonoBehaviour
    {
        [SerializeField] private string lobbySceneName;
        [SerializeField] private string firstSceneName;
        [SerializeField] private string secondSceneName;
        [SerializeField] private string thirdSceneName;
        [SerializeField] private string npcStocksSceneName;

        public void LoadLobbyScene()
        {
            if (!string.IsNullOrEmpty(lobbySceneName))
            {
                // Set the flag to initialize audio when loading the lobby scene
                SceneTransitionContext.ShouldInitializeLobbyAudio = true;
                SceneManager.LoadSceneAsync(lobbySceneName);
            }
            else
            {
                Debug.LogWarning("Lobby scene name is not assigned.");
            }
        }

        public void LoadLobbySceneWithoutFade()
        {
            if (!string.IsNullOrEmpty(lobbySceneName))
            {
                // Set the flag to NOT initialize audio when loading the lobby scene
                SceneTransitionContext.ShouldInitializeLobbyAudio = false;
                StartCoroutine(LoadSceneAsync(lobbySceneName));
            }
            else
            {
                Debug.LogWarning("Lobby scene name is not assigned.");
            }
        }

        public void LoadFirstScene()
        {
            if (!string.IsNullOrEmpty(firstSceneName))
            {
                StartCoroutine(LoadSceneAsync(firstSceneName));
            }
            else
            {
                Debug.LogWarning("First scene name is not assigned.");
            }
        }

        public void LoadSecondScene()
        {
            if (!string.IsNullOrEmpty(secondSceneName))
            {
                StartCoroutine(LoadSceneAsync(secondSceneName));
            }
            else
            {
                Debug.LogWarning("Second scene name is not assigned.");
            }
        }

        public void LoadThirdScene()
        {
            if (!string.IsNullOrEmpty(thirdSceneName))
            {
                StartCoroutine(LoadSceneAsync(thirdSceneName));
            }
            else
            {
                Debug.LogWarning("Third scene name is not assigned.");
            }
        }

        public void LoadNPCStocksScene()
        {
            if (!string.IsNullOrEmpty(npcStocksSceneName))
            {
                StartCoroutine(LoadSceneAsync(npcStocksSceneName));
            }
            else
            {
                Debug.LogWarning("NPC Stocks scene name is not assigned.");
            }
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    public static class SceneTransitionContext
    {
        // Flag to determine if audio should be initialized when loading the lobby scene.
        public static bool ShouldInitializeLobbyAudio = true;
    }

}
