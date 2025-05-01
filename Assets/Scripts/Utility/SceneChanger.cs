using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utility
{
    public class SceneChanger : MonoBehaviour
    {
        [SerializeField] string lobbySceneName;
        [SerializeField] string firstSceneName;
        [SerializeField] string secondSceneName;
        [SerializeField] string thirdSceneName;
        [SerializeField] string npcStocksSceneName;

        /* ── public API ─────────────────────────────────────────── */

        public void LoadLobbyScene() => Load(lobbySceneName, true, false);
        public void LoadLobbySceneWithoutFade() => Load(lobbySceneName, false, false);
        public void LoadFirstScene() => Load(firstSceneName);
        public void LoadSecondScene() => Load(secondSceneName);
        public void LoadThirdScene() => Load(thirdSceneName);
        public void LoadNPCStocksScene() => Load(npcStocksSceneName);

        /* ── core helper ────────────────────────────────────────── */

        void Load(string scene, bool initAudioFlag = false, bool initUIFlag = false)
        {
            if (string.IsNullOrEmpty(scene))
            {
                Debug.LogWarning("Scene name not assigned.");
                return;
            }

            // set lobby flags only if we’re going to the lobby
            if (scene == lobbySceneName)
            {
                SceneTransitionContext.ShouldInitializeLobbyAudio = initAudioFlag;
                SceneTransitionContext.ShouldInitializeLobbyUI = initUIFlag;
            }

            StartCoroutine(LoadAsync(scene));
        }

        /* uses a cached WaitUntil so no per-call alloc */
        static readonly WaitUntil waitFrame = new(() => false); // reused

        static IEnumerator LoadAsync(string scene)
        {
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return waitFrame;
        }

        /* optional Exit */
        public void Exit()
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
        public static bool ShouldInitializeLobbyAudio = true;
        public static bool ShouldInitializeLobbyUI = true;
    }
}
