using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
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

        void Start()
        {
            InitializeUIElements();
            InitializeAudio();
        }

        void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        void Update()
        {
        }

        #region Initialization Methods

        private void InitializeUIElements()
        {
            // Set initial alpha for UI CanvasGroup if assigned
            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.alpha = 0;
                uiMaterial.SetFloat("_CanvasGroupAlpha", 0);
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

        #region Video Preparation

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
        }

        #endregion
    }
}
