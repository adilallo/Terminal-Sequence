using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace LobbyScene
{
    public class LobbySceneManager : MonoBehaviour
    {
        [SerializeField] CanvasGroup uiCanvasGroup;
        [SerializeField] Material uiMaterial;
        [SerializeField] float fadeDuration = 2f;

        [Header("Audio")]
        [SerializeField] List<AudioClip> startSceneAudioClips;

        SceneChanger sceneChanger;

        bool firstClickReceived;

        readonly Material[] fadeMats = new Material[1];

        /* ─── life-cycle ─────────────────────────────────────────────────── */

        void Awake()
        {
            fadeMats[0] = uiMaterial;
            sceneChanger = FindFirstObjectByType<SceneChanger>();
        }

        void Start()
        {
            uiCanvasGroup.interactable = false;

            InitAudio();
            SetAlphaAll(0); 
            uiCanvasGroup.alpha = 0;

            if (SceneTransitionContext.ShouldInitializeLobbyUI)
            {  
                StartCoroutine(WaitForFirstInput());
            }
            else
            {
                StartCoroutine(FadeInUI());
            }
        }

        /* ─── input wait (replaces Update polling) ───────────────────────── */

        IEnumerator WaitForFirstInput()
        {
            while (!firstClickReceived)
            {
                if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                    firstClickReceived = true;
                yield return null;
            }

            StartCoroutine(FadeInUI());
        }

        /* ─── buttons ───────────────────────────────────────────────────── */

        public void OnPlayButton() => StartCoroutine(FadeOutAndLoad(sceneChanger.LoadFirstScene));
        public void OnNPCButton() => StartCoroutine(FadeOutAndLoad(sceneChanger.LoadNPCStocksScene));

        /* ─── audio ─────────────────────────────────────────────────────── */

        void InitAudio()
        {
            if (AudioManager.Instance == null) { Debug.LogWarning("AudioManager missing"); return; }

            if (SceneTransitionContext.ShouldInitializeLobbyAudio)
            {
                AudioManager.Instance.PlayPlaylist(startSceneAudioClips, false);
            }
            // else keep playing – returning from NPCStocks
        }

        /* ─── fades ─────────────────────────────────────────────────────── */

        IEnumerator FadeInUI()
        {
            float t = 0;
            while (t < fadeDuration)
            {
                float a = t / fadeDuration;
                uiCanvasGroup.alpha = a;
                SetAlphaAll(a);
                t += Time.deltaTime;
                yield return null;
            }
            uiCanvasGroup.alpha = 1;
            SetAlphaAll(1);
            uiCanvasGroup.interactable = true;
        }

        IEnumerator FadeOutAndLoad(System.Action loadScene)
        {
            uiCanvasGroup.interactable = false;

            float t = 0;
            while (t < fadeDuration)
            {
                float a = 1f - t / fadeDuration;
                uiCanvasGroup.alpha = a;
                SetAlphaAll(a);
                t += Time.deltaTime;
                yield return null;
            }
            uiCanvasGroup.alpha = 0;
            SetAlphaAll(0);
            loadScene.Invoke();
        }

        void SetAlphaAll(float a)
        {
            for (int i = 0; i < fadeMats.Length; i++)
                if (fadeMats[i]) fadeMats[i].SetFloat("_CanvasGroupAlpha", a);
        }
    }
}
