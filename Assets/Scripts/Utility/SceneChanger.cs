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

        private readonly int targetWidth = 800;
        private readonly int targetHeight = 1280;

        void Start()
        {
            if (Screen.width != targetWidth || Screen.height != targetHeight || !Screen.fullScreen)
            {
                // Uncomment and implement if you want to set resolution
                // SetResolution(targetWidth, targetHeight, true);
            }
        }

        public void LoadLobbyScene()
        {
            if (!string.IsNullOrEmpty(lobbySceneName))
            {
                // Set the flag to initialize audio when loading the lobby scene
                SceneTransitionContext.ShouldInitializeLobbyAudio = true;
                StartCoroutine(FadeOutAndLoadScene(lobbySceneName));
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
                StartCoroutine(FadeOutAndLoadScene(firstSceneName));
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
                SceneManager.LoadSceneAsync(secondSceneName);
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
                SceneManager.LoadSceneAsync(thirdSceneName);
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
                SceneManager.LoadSceneAsync(npcStocksSceneName);
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

        private IEnumerator FadeOutAndLoadScene(string sceneName)
        {
            // Fade out current audio
            if (AudioManager.Instance != null)
            {
                yield return AudioManager.Instance.FadeOutCurrentTrack();  // Wait for the fade-out to complete
            }

            // Load the next scene asynchronously after fading out
            yield return SceneManager.LoadSceneAsync(sceneName);
        }

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetResolution(int targetWidth, int targetHeight, bool fullscreen)
        {
            // Get the current screen width and height
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Calculate the aspect ratios
            float targetAspect = (float)targetWidth / (float)targetHeight;
            float screenAspect = screenWidth / screenHeight;

            // Determine if we need to adjust width or height to preserve the aspect ratio
            if (screenAspect > targetAspect)
            {
                // Screen is wider than target, adjust width
                int adjustedWidth = Mathf.RoundToInt(targetHeight * screenAspect);
                Screen.SetResolution(adjustedWidth, targetHeight, fullscreen);
            }
            else
            {
                // Screen is taller than target, adjust height
                int adjustedHeight = Mathf.RoundToInt(targetWidth / screenAspect);
                Screen.SetResolution(targetWidth, adjustedHeight, fullscreen);
            }

            Screen.fullScreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        }
    }

    public static class SceneTransitionContext
    {
        // Flag to determine if audio should be initialized when loading the lobby scene.
        public static bool ShouldInitializeLobbyAudio = true;
    }

}
