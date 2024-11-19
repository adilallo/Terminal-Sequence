using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace MiddleScene
{
    public class MiddleSceneManager : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private CanvasGroup uiCanvasGroup;
        [SerializeField] private Material avatarMaterial;
        [SerializeField] private Material npcMaterial;
        [SerializeField] private Material uiMaterial;
        [SerializeField] private float fadeDuration = 2f;

        [Header("UI")]
        [SerializeField] private VideoPlayer npcVideoPlayer;
        [SerializeField] private VideoPlayer avatarVideoPlayer;
        [SerializeField] private RectTransform npcRawImage;
        [SerializeField] private RectTransform avatarRawImage;
        [SerializeField] private VideoPlayer arrowVideoPlayer;
        [SerializeField] private GameObject arrowLeftRawImage;
        [SerializeField] private GameObject arrowRightRawImage;
        [SerializeField] private Canvas canvas;

        [Header("Audio")]
        [SerializeField] private List<AudioClip> middleSceneAudioClips;

        [Header("Video URLs")]
        [SerializeField] private List<VideoClip> npcVideoURLs;
        [SerializeField] private List<VideoClip> avatarVideoURLs;

        [SerializeField] private LocalSelectionTracker localSelectionHandler;

        #endregion

        #region Private Fields

        private int currentVideoIndex = 0;
        private Vector2 avatarVelocity = new Vector2(100f, 100f);
        private RectTransform canvasRectTransform;
        private bool videoPlayersPrepared = false;
        private Vector2 cachedCanvasSize;
        private Vector2 cachedAvatarSize;

        private bool avatarVideoStarted = false;
        private bool npcVideoStarted = false;

        #endregion

        #region Unity Methods

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            MoveAvatarRawImage();
        }

        void OnDisable()
        {
            CleanupVideoPlayers();
        }

        #endregion

        #region Initialization Methods

        private void Initialize()
        {
            Cursor.visible = false;

            npcRawImage.gameObject.SetActive(false);
            avatarRawImage.gameObject.SetActive(false);
            arrowLeftRawImage.SetActive(false);
            arrowRightRawImage.SetActive(false);

            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.alpha = 0;
                SetMaterialAlpha(avatarMaterial, 0);
                SetMaterialAlpha(npcMaterial, 0);
                SetMaterialAlpha(uiMaterial, 0);
                StartCoroutine(FadeInUI());
            }

            currentVideoIndex = 0;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlaylist(middleSceneAudioClips, false);
            }

            if (canvas != null)
            {
                canvasRectTransform = canvas.GetComponent<RectTransform>();
                CacheCanvasAndAvatarDimensions();
            }
            else
            {
                Debug.LogError("Canvas is not assigned! Please assign a Canvas in the Inspector.");
            }

            if (avatarRawImage == null)
            {
                Debug.LogError("avatarRawImage is not assigned! Please check the Inspector.");
            }

            // Start playing the initial videos
            PlayVideoAndAudio(currentVideoIndex);
        }

        private void CleanupVideoPlayers()
        {
            if (npcVideoPlayer != null)
            {
                npcVideoPlayer.prepareCompleted -= OnNPCVideoPrepared;
                npcVideoPlayer.Stop();
            }
            if (avatarVideoPlayer != null)
            {
                avatarVideoPlayer.prepareCompleted -= OnAvatarVideoPrepared;
                avatarVideoPlayer.Stop();
            }
        }

        #endregion

        #region Video Control Methods

        public void NextVideo()
        {
            int nextIndex = (currentVideoIndex + 1) % npcVideoURLs.Count;
            StartCoroutine(FadeOutAndChangeVideo(nextIndex));
        }

        public void PreviousVideo()
        {
            int prevIndex = (currentVideoIndex - 1 + npcVideoURLs.Count) % npcVideoURLs.Count;
            StartCoroutine(FadeOutAndChangeVideo(prevIndex));
        }

        public void OnVideoSelected()
        {
            if (localSelectionHandler != null)
            {
                localSelectionHandler.RecordVideoSelection(currentVideoIndex);
            }
            else
            {
                Debug.LogError("LocalSelectionHandler is not assigned in the Inspector.");
            }
        }

        private IEnumerator FadeOutAndChangeVideo(int newVideoIndex)
        {
            // Fade out over 0.5 seconds
            float fadeOutDuration = 0.75f;
            float elapsedTime = 0f;

            // Get the current alpha of the materials
            float startAlpha = avatarMaterial.GetFloat("_CanvasGroupAlpha");

            while (elapsedTime < fadeOutDuration)
            {
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);

                SetMaterialAlpha(avatarMaterial, alpha);
                SetMaterialAlpha(npcMaterial, alpha);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure materials are fully transparent
            SetMaterialAlpha(avatarMaterial, 0f);
            SetMaterialAlpha(npcMaterial, 0f);

            // Deactivate the RawImages
            avatarRawImage.gameObject.SetActive(false);
            npcRawImage.gameObject.SetActive(false);

            // Stop current videos
            if (avatarVideoPlayer != null)
            {
                avatarVideoPlayer.Stop();
            }
            if (npcVideoPlayer != null)
            {
                npcVideoPlayer.Stop();
            }

            // Change the video index
            currentVideoIndex = newVideoIndex;

            // Prepare and play the new videos
            PlayVideoAndAudio(currentVideoIndex);

            // Wait until both videos have started playing
            while (!avatarVideoStarted || !npcVideoStarted)
            {
                yield return null;
            }

            // Fade in over 0.5 seconds
            float fadeInDuration = 0.75f;
            elapsedTime = 0f;

            while (elapsedTime < fadeInDuration)
            {
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);

                SetMaterialAlpha(avatarMaterial, alpha);
                SetMaterialAlpha(npcMaterial, alpha);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure materials are fully opaque
            SetMaterialAlpha(avatarMaterial, 1f);
            SetMaterialAlpha(npcMaterial, 1f);
        }

        private void PlayVideoAndAudio(int index)
        {
            // Reset started flags
            avatarVideoStarted = false;
            npcVideoStarted = false;

            // Prepare and play NPC Video
            if (npcVideoPlayer != null && npcVideoURLs.Count > index && npcVideoURLs[index] != null)
            {
                npcVideoPlayer.clip = npcVideoURLs[index];
                npcVideoPlayer.prepareCompleted += OnNPCVideoPrepared;
                npcVideoPlayer.Prepare();
            }
            else
            {
                Debug.LogWarning("NPC Video URL at index " + index + " is invalid.");
            }

            // Prepare and play Avatar Video
            if (avatarVideoPlayer != null && avatarVideoURLs.Count > index && avatarVideoURLs[index] != null)
            {
                avatarVideoPlayer.clip = avatarVideoURLs[index];
                avatarVideoPlayer.prepareCompleted += OnAvatarVideoPrepared;
                avatarVideoPlayer.Prepare();
            }
            else
            {
                Debug.LogWarning("Avatar Video URL at index " + index + " is invalid.");
            }
        }

        private void OnNPCVideoPrepared(VideoPlayer source)
        {
            source.prepareCompleted -= OnNPCVideoPrepared;
            npcVideoPlayer.started += OnNPCVideoStarted;
            npcVideoPlayer.Play();
        }

        private void OnNPCVideoStarted(VideoPlayer source)
        {
            source.started -= OnNPCVideoStarted;
            npcRawImage.gameObject.SetActive(true);
            npcVideoStarted = true;
            CheckIfBothVideosStarted();
        }

        private void OnAvatarVideoPrepared(VideoPlayer source)
        {
            source.prepareCompleted -= OnAvatarVideoPrepared;
            avatarVideoPlayer.started += OnAvatarVideoStarted;
            avatarVideoPlayer.Play();
        }

        private void OnAvatarVideoStarted(VideoPlayer source)
        {
            source.started -= OnAvatarVideoStarted;
            avatarRawImage.gameObject.SetActive(true);
            avatarVideoStarted = true;
            CheckIfBothVideosStarted();
        }

        private void CheckIfBothVideosStarted()
        {
            if (npcVideoStarted && avatarVideoStarted)
            {
                arrowLeftRawImage.SetActive(true);
                arrowRightRawImage.SetActive(true);
            }
        }

        #endregion

        #region UI Methods

        private IEnumerator FadeInUI()
        {
            float elapsedTime = 0f;

            // While we haven't reached the fade duration, continue adjusting the alpha
            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);

                // Set the alpha for the UI Canvas Group
                uiCanvasGroup.alpha = alpha;

                // Set the alpha value for the materials
                SetMaterialAlpha(avatarMaterial, alpha);
                SetMaterialAlpha(npcMaterial, alpha);
                SetMaterialAlpha(uiMaterial, alpha);

                // Update the elapsed time
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure everything is fully visible at the end of the fade-in
            uiCanvasGroup.alpha = 1;
            SetMaterialAlpha(avatarMaterial, 1);
            SetMaterialAlpha(npcMaterial, 1);
            SetMaterialAlpha(uiMaterial, 1);
        }

        // Helper method to set the alpha on the material
        private void SetMaterialAlpha(Material material, float alpha)
        {
            if (material != null)
            {
                material.SetFloat("_CanvasGroupAlpha", alpha);
            }
        }

        #endregion

        #region Avatar Movement Methods

        private void CacheCanvasAndAvatarDimensions()
        {
            if (canvasRectTransform != null && avatarRawImage != null)
            {
                cachedCanvasSize = new Vector2(canvasRectTransform.rect.width, canvasRectTransform.rect.height);
                cachedAvatarSize = new Vector2(avatarRawImage.rect.width, avatarRawImage.rect.height);
            }
        }

        private void MoveAvatarRawImage()
        {
            if (avatarRawImage == null || canvasRectTransform == null)
            {
                Debug.LogError("Either avatarRawImage or canvasRectTransform is null. Movement cannot proceed.");
                return;
            }

            Vector2 currentCanvasSize = new Vector2(canvasRectTransform.rect.width, canvasRectTransform.rect.height);
            Vector2 currentAvatarSize = new Vector2(avatarRawImage.rect.width, avatarRawImage.rect.height);
            if (currentCanvasSize != cachedCanvasSize || currentAvatarSize != cachedAvatarSize)
            {
                CacheCanvasAndAvatarDimensions();
            }

            Vector2 currentPosition = avatarRawImage.anchoredPosition;
            currentPosition += avatarVelocity * Time.deltaTime;

            float canvasWidth = cachedCanvasSize.x;
            float canvasHeight = cachedCanvasSize.y;
            float avatarWidth = cachedAvatarSize.x;
            float avatarHeight = cachedAvatarSize.y + 100;

            float yOffsetTop = canvasHeight * 0.046f;

            float minX = -canvasWidth / 2 + avatarWidth / 2;
            float maxX = canvasWidth / 2 - avatarWidth / 2;
            float minY = (-canvasHeight / 2 + avatarHeight / 2) + yOffsetTop;
            float maxY = (canvasHeight / 2 - avatarHeight / 2) + yOffsetTop;

            if (currentPosition.x < minX)
            {
                currentPosition.x = minX;
                avatarVelocity.x *= -1;
            }
            else if (currentPosition.x > maxX)
            {
                currentPosition.x = maxX;
                avatarVelocity.x *= -1;
            }

            if (currentPosition.y < minY)
            {
                currentPosition.y = minY;
                avatarVelocity.y *= -1;
            }
            else if (currentPosition.y > maxY)
            {
                currentPosition.y = maxY;
                avatarVelocity.y *= -1;
            }

            avatarRawImage.anchoredPosition = currentPosition;
        }

        #endregion
    }
}