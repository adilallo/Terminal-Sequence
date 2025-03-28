using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;
using Utility;

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
        [SerializeField] private List<string> npcVideoURLs;
        [SerializeField] private List<string> avatarVideoURLs;

        [Header("Video Clips (Offline Fallback)")]
        [SerializeField] private List<VideoClip> npcFallbackClips;
        [SerializeField] private List<VideoClip> avatarFallbackClips;

        [SerializeField] private GoogleSheetsHandler googleSheetsHandler;

        #endregion

        #region Private Fields

        private int currentVideoIndex = 0;
        private Vector2 avatarVelocity = new Vector2(100f, 100f);
        private RectTransform canvasRectTransform;
        private Vector2 cachedCanvasSize;
        private Vector2 cachedAvatarSize;

        private bool avatarVideoStarted = false;
        private bool npcVideoStarted = false;
        private bool isOffline = false;

        private SceneChanger sceneChanger;

        #endregion

        #region Unity Methods

        void Start()
        {
            isOffline = (Application.internetReachability == NetworkReachability.NotReachable);
            sceneChanger = FindFirstObjectByType<SceneChanger>();
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
                uiCanvasGroup.interactable = false;
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

        public async void OnVideoSelected()
        {
            uiCanvasGroup.interactable = false;
            if (googleSheetsHandler != null)
            {
                googleSheetsHandler.RecordVideoSelection(currentVideoIndex);
            }
            else
            {
                Debug.LogWarning("GoogleSheetsHandler is not assigned in the Inspector.");
            }
            await FadeOutUICoroutine();
            sceneChanger.LoadThirdScene();
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

            // --- NPC Video ---
            if (npcVideoPlayer != null && npcVideoURLs.Count > index && !string.IsNullOrEmpty(npcVideoURLs[index]))
            {
                npcVideoPlayer.source = VideoSource.Url;
                npcVideoPlayer.url = npcVideoURLs[index];

                // If there's an error retrieving the video, call OnNPCVideoError
                npcVideoPlayer.errorReceived += OnNPCVideoError;

                // Once prepared, call OnNPCVideoPrepared
                npcVideoPlayer.prepareCompleted += OnNPCVideoPrepared;
                npcVideoPlayer.Prepare();
            }
            else
            {
                // Immediately fallback if we know the URL is invalid
                Debug.LogWarning($"NPC Video URL at index {index} is invalid – fallback to local clip.");
                PlayNPCFallback(index);
            }

            // --- Avatar Video ---
            if (avatarVideoPlayer != null && avatarVideoURLs.Count > index && !string.IsNullOrEmpty(avatarVideoURLs[index]))
            {
                avatarVideoPlayer.source = VideoSource.Url;
                avatarVideoPlayer.url = avatarVideoURLs[index];

                avatarVideoPlayer.errorReceived += OnAvatarVideoError;
                avatarVideoPlayer.prepareCompleted += OnAvatarVideoPrepared;
                avatarVideoPlayer.Prepare();
            }
            else
            {
                Debug.LogWarning($"Avatar Video URL at index {index} is invalid – fallback to local clip.");
                PlayAvatarFallback(index);
            }
        }

        private void OnNPCVideoError(VideoPlayer source, string message)
        {
            Debug.LogWarning($"NPC Video failed to load from '{source.url}' => {message}. Falling back to local clip.");

            // Cleanup this event so it doesn't keep firing
            source.errorReceived -= OnNPCVideoError;
            source.prepareCompleted -= OnNPCVideoPrepared;
            source.Stop();

            // Fallback
            PlayNPCFallback(currentVideoIndex);
        }

        private void PlayNPCFallback(int index)
        {
            if (npcVideoPlayer == null)
            {
                Debug.LogError("NPC VideoPlayer is null – cannot play fallback!");
                return;
            }
            if (npcFallbackClips == null || npcFallbackClips.Count <= index || npcFallbackClips[index] == null)
            {
                Debug.LogWarning($"No valid NPC fallback clip at index {index}.");
                return;
            }

            // Use local video clip
            npcVideoPlayer.source = VideoSource.VideoClip;
            npcVideoPlayer.clip = npcFallbackClips[index];

            // Make sure we remove any old listeners
            npcVideoPlayer.errorReceived -= OnNPCVideoError;
            npcVideoPlayer.prepareCompleted -= OnNPCVideoPrepared;

            npcVideoPlayer.prepareCompleted += OnNPCVideoPrepared;
            npcVideoPlayer.Prepare();
        }

        private void OnAvatarVideoError(VideoPlayer source, string message)
        {
            Debug.LogWarning($"Avatar Video failed to load from '{source.url}' => {message}. Falling back to local clip.");

            // Cleanup
            source.errorReceived -= OnAvatarVideoError;
            source.prepareCompleted -= OnAvatarVideoPrepared;
            source.Stop();

            PlayAvatarFallback(currentVideoIndex);
        }

        private void PlayAvatarFallback(int index)
        {
            if (avatarVideoPlayer == null)
            {
                Debug.LogError("Avatar VideoPlayer is null – cannot play fallback!");
                return;
            }
            if (avatarFallbackClips == null || avatarFallbackClips.Count <= index || avatarFallbackClips[index] == null)
            {
                Debug.LogWarning($"No valid Avatar fallback clip at index {index}.");
                return;
            }

            avatarVideoPlayer.source = VideoSource.VideoClip;
            avatarVideoPlayer.clip = avatarFallbackClips[index];

            avatarVideoPlayer.errorReceived -= OnAvatarVideoError;
            avatarVideoPlayer.prepareCompleted -= OnAvatarVideoPrepared;

            avatarVideoPlayer.prepareCompleted += OnAvatarVideoPrepared;
            avatarVideoPlayer.Prepare();
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
            uiCanvasGroup.interactable = true;
        }

        // Helper method to set the alpha on the material
        private void SetMaterialAlpha(Material material, float alpha)
        {
            if (material != null)
            {
                material.SetFloat("_CanvasGroupAlpha", alpha);
            }
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
            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
                uiCanvasGroup.alpha = alpha;
                SetMaterialAlpha(avatarMaterial, alpha);
                SetMaterialAlpha(npcMaterial, alpha);
                SetMaterialAlpha(uiMaterial, alpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            uiCanvasGroup.alpha = 0;
            SetMaterialAlpha(avatarMaterial, 0);
            SetMaterialAlpha(npcMaterial, 0);
            SetMaterialAlpha(uiMaterial, 0);
            uiCanvasGroup.interactable = false;

            tcs.SetResult(true);
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