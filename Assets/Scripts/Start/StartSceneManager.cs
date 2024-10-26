using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace StartScene
{
    public class StartSceneManager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup uiCanvasGroup;
        [SerializeField] private float fadeDuration = 2f;

        [HeaderAttribute("Intro Assets")]
        [SerializeField] private RawImage introVideo;
        [SerializeField] private GameObject introImage;
        [SerializeField] private VideoPlayer introVideoPlayer;

        [HeaderAttribute("UI")]
        [SerializeField] private GameObject UI;
        [SerializeField] private VideoPlayer stockVideoPlayer;
        [SerializeField] private RawImage stockRawImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private VideoPlayer avatarVideoPlayer;
        [SerializeField] private GameObject selectButton;

        [HeaderAttribute("Audio")]
        [SerializeField] private List<AudioClip> startSceneAudioClips;

        private bool mouseClicked = false;
        private bool selectButtonVisible = false;
        private bool stockVideoStarted = false;

        private bool videosPrepared = false;

        private Color initialStockRawImageColor;
        private Color initialFrameImageColor;

        void Start()
        {
            InitializeFlags();
            InitializeUIElements();
            CacheInitialColors();

            Cursor.visible = false;

            InitializeAudio();

            if (introVideoPlayer != null)
            {
                introVideoPlayer.Prepare();
            }
            else
            {
                Debug.LogError("Intro VideoPlayer is not assigned! Please check the Inspector.");
            }
        }

        void OnEnable()
        {
            if (introVideoPlayer != null && !videosPrepared)
            {
                introVideoPlayer.loopPointReached += OnVideoFinished;
                introVideoPlayer.Prepare();
                videosPrepared = true;
            }
        }

        void OnDisable()
        {
            if (introVideoPlayer != null)
            {
                introVideoPlayer.loopPointReached -= OnVideoFinished;
                introVideoPlayer.Stop();
            }

            if (stockVideoPlayer != null)
            {
                stockVideoPlayer.Stop();
            }

            if (avatarVideoPlayer != null)
            {
                avatarVideoPlayer.Stop();
            }
        }

        void Update()
        {
            HandleMouseClick();
            HandleAvatarVideoState();
        }

        #region Initialization Methods

        private void InitializeFlags()
        {
            mouseClicked = false;
            selectButtonVisible = false;
            stockVideoStarted = false;
            videosPrepared = false;
        }

        private void InitializeUIElements()
        {
            // Activate introImage and deactivate UI elements
            introImage.SetActive(true);
            UI.SetActive(false);
            selectButton.SetActive(false);

            // Set initial alpha for UI CanvasGroup if assigned
            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.alpha = 0;
            }
            else
            {
                Debug.LogError("UI CanvasGroup is not assigned! Please check the Inspector.");
            }

            // Set initial alpha for stockRawImage and frameImage
            if (stockRawImage != null && frameImage != null)
            {
                stockRawImage.color = new Color(stockRawImage.color.r, stockRawImage.color.g, stockRawImage.color.b, 0f);
                frameImage.color = new Color(frameImage.color.r, frameImage.color.g, frameImage.color.b, 0f);
            }
            else
            {
                Debug.LogError("Stock RawImage or Frame Image is not assigned! Please check the Inspector.");
            }
        }

        private void CacheInitialColors()
        {
            if (stockRawImage != null && frameImage != null)
            {
                initialStockRawImageColor = stockRawImage.color;
                initialFrameImageColor = frameImage.color;
            }
        }

        private void InitializeAudio()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlaylist(startSceneAudioClips, false);
                // If there are events related to AudioManager, handle them here
            }
            else
            {
                Debug.LogError("AudioManager instance is missing.");
            }
        }

        #endregion

        #region Event Handlers

        private void OnVideoFinished(VideoPlayer vp)
        {
            // Reset introVideoPlayer time to loop if necessary
            introVideoPlayer.time = 0;

            // Start fading in the UI and play avatar and stock videos
            StartCoroutine(FadeInUI());

            if (avatarVideoPlayer != null && stockVideoPlayer != null)
            {
                avatarVideoPlayer.Play();
                stockVideoPlayer.Play();
                UI.SetActive(true);
            }
            else
            {
                Debug.LogError("Avatar VideoPlayer or Stock VideoPlayer is not assigned! Please check the Inspector.");
            }
        }

        #endregion

        #region Update Methods

        private void HandleMouseClick()
        {
            if (Input.GetMouseButtonDown(0) && !mouseClicked)
            {
                mouseClicked = true;
                StartCoroutine(PlayIntroVideoWhenReady());
            }
        }

        private void HandleAvatarVideoState()
        {
            if (avatarVideoPlayer == null)
                return;

            if (avatarVideoPlayer.isPlaying)
            {
                if (!selectButtonVisible)
                {
                    CheckAvatarVideoEnd();
                }

                if (!stockVideoStarted)
                {
                    CheckStartStockVideo();
                }
            }
        }

        #endregion

        #region Coroutines


        private IEnumerator PlayIntroVideoWhenReady()
        {
            if (introVideoPlayer == null)
            {
                yield break;
            }

            // Wait until the intro video is prepared
            while (!introVideoPlayer.isPrepared)
            {
                yield return null;
            }

            // Deactivate intro image and play the intro video
            introImage.SetActive(false);
            introVideoPlayer.Play();
        }

        private IEnumerator FadeInUI()
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                uiCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            uiCanvasGroup.alpha = 1;
        }

        private IEnumerator FadeInStockVideo()
        {
            if (stockVideoPlayer == null || stockRawImage == null || frameImage == null)
            {
                yield break;
            }

            float elapsedTime = 0f;
            stockVideoPlayer.Play();

            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
                stockRawImage.color = new Color(initialStockRawImageColor.r, initialStockRawImageColor.g, initialStockRawImageColor.b, alpha);
                frameImage.color = new Color(initialFrameImageColor.r, initialFrameImageColor.g, initialFrameImageColor.b, alpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure alpha is set to 1
            stockRawImage.color = new Color(initialStockRawImageColor.r, initialStockRawImageColor.g, initialStockRawImageColor.b, 1f);
            frameImage.color = new Color(initialFrameImageColor.r, initialFrameImageColor.g, initialFrameImageColor.b, 1f);
        }

        #endregion

        #region Helper Methods

        private void CheckAvatarVideoEnd()
        {
            if (avatarVideoPlayer == null)
                return;

            if (avatarVideoPlayer.time >= avatarVideoPlayer.length * 0.93f)
            {
                selectButton.SetActive(true);
                selectButtonVisible = true;
            }
        }

        private void CheckStartStockVideo()
        {
            if (avatarVideoPlayer == null)
                return;

            if (avatarVideoPlayer.time >= avatarVideoPlayer.length * 0.5625f)
            {
                StartCoroutine(FadeInStockVideo());
                stockVideoStarted = true;
            }
        }

        #endregion
    }
}
