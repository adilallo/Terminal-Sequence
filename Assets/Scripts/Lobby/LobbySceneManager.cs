using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Utility;

namespace LobbyScene
{
    public class LobbySceneManager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup uiCanvasGroup;
        [SerializeField] private Material uiMaterial;
        [SerializeField] private float fadeDuration = 2f;

        [Header("Audio")]
        [SerializeField] private List<AudioClip> startSceneAudioClips;

        private SceneChanger sceneChanger;
        private bool hasInitializedUI = false;

        void Start()
        {
            uiCanvasGroup.interactable = false;
            sceneChanger = FindFirstObjectByType<SceneChanger>();

            InitializeAudio();

            if (SceneTransitionContext.ShouldInitializeLobbyUI)
            {
                uiCanvasGroup.alpha = 0;
                uiMaterial.SetFloat("_CanvasGroupAlpha", 0);
            }
            else
            {
                StartCoroutine(FadeInUI());
            }
        }

        void Update()
        {
            if (SceneTransitionContext.ShouldInitializeLobbyUI && !hasInitializedUI && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
            {
                hasInitializedUI = true;
                InitializeUIElements();
            }
        }

        #region Initialization Methods

        private void InitializeUIElements()
        {
            // Set initial alpha for UI CanvasGroup if assigned
            if (uiCanvasGroup != null)
            {
                StartCoroutine(FadeInUI());
            }
            else
            {
                Debug.LogError("UI CanvasGroup is not assigned! Please check the Inspector.");
            }
        }

        private void InitializeAudio()
        {
            if (AudioManager.Instance != null)
            {
                // Check the SceneTransitionContext to determine if audio should be initialized
                if (SceneTransitionContext.ShouldInitializeLobbyAudio)
                {
                    AudioManager.Instance.PlayPlaylist(startSceneAudioClips, false);
                    Debug.Log("Initializing lobby audio.");
                }
                else
                {
                    // Do not initialize audio when returning from NPCStocks
                    Debug.Log("Returning to lobby from NPCStocks. Audio will continue without reinitialization.");
                }
            }
            else
            {
                Debug.LogError("AudioManager instance is missing.");
            }
        }

        #endregion

        #region Buttons
        public async void OnPlayButton()
        {
            uiCanvasGroup.interactable = false;
            await FadeOutUICoroutine();
            sceneChanger.LoadFirstScene();
        }

        public async void OnNPCButton()
        {
            uiCanvasGroup.interactable = false;
            await FadeOutUICoroutine();
            sceneChanger.LoadNPCStocksScene();
        }

        #endregion

        #region Coroutines

        private IEnumerator FadeInUI()
        {
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);

                uiCanvasGroup.alpha = alpha;
                uiMaterial.SetFloat("_CanvasGroupAlpha", alpha);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            uiCanvasGroup.alpha = 1;
            uiMaterial.SetFloat("_CanvasGroupAlpha", 1);
            uiCanvasGroup.interactable = true;
        }

        private async Task FadeOutUICoroutine()
        {
            var tcs = new TaskCompletionSource<bool>();

            StartCoroutine(FadeOutUI(tcs));

            await tcs.Task;
        }

        private IEnumerator FadeOutUI(TaskCompletionSource<bool> tcs)
        {
            float elapsedTime = 0f;

            uiCanvasGroup.alpha = 1;
            uiMaterial.SetFloat("_CanvasGroupAlpha", 1);

            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);

                uiCanvasGroup.alpha = alpha;
                uiMaterial.SetFloat("_CanvasGroupAlpha", alpha);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            uiCanvasGroup.alpha = 0;
            uiMaterial.SetFloat("_CanvasGroupAlpha", 0);

            tcs.SetResult(true);
        }

        #endregion
    }
}
