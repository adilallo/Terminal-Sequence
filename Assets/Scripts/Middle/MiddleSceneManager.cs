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
        [SerializeField] private List<string> npcVideoURLs;
        [SerializeField] private List<string> avatarVideoURLs;

        #endregion

        #region Private Fields

        private int currentVideoIndex = 0;
        private Vector2 avatarVelocity = new Vector2(100f, 100f);
        private RectTransform canvasRectTransform;
        private bool clipsSet = false;
        private bool videoPlayersPrepared = false;
        private Vector2 cachedCanvasSize;
        private Vector2 cachedAvatarSize;

        #endregion

        #region Unity Methods

        void Start()
        {
            Initialize();
        }

        void OnEnable()
        {
            PrepareVideoPlayers();
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
        }

        private void PrepareVideoPlayers()
        {
            if (!videoPlayersPrepared)
            {
                if (npcVideoPlayer != null)
                {
                    npcVideoPlayer.prepareCompleted += OnVideosPrepared;
                }
                if (arrowVideoPlayer != null)
                {
                    arrowVideoPlayer.prepareCompleted += OnVideosPrepared;
                }
                videoPlayersPrepared = true;
                PlayVideoAndAudio(currentVideoIndex);
            }
        }

        private void CleanupVideoPlayers()
        {
            if (npcVideoPlayer != null)
            {
                npcVideoPlayer.prepareCompleted -= OnVideosPrepared;
                npcVideoPlayer.Stop();
            }
            if (arrowVideoPlayer != null)
            {
                arrowVideoPlayer.prepareCompleted -= OnVideosPrepared;
                arrowVideoPlayer.Stop();
            }
            if (avatarVideoPlayer != null)
            {
                avatarVideoPlayer.Stop();
            }
        }

        #endregion

        #region Video Control Methods

        public void NextVideo()
        {
            currentVideoIndex = (currentVideoIndex + 1) % npcVideoURLs.Count;
            PlayVideoAndAudio(currentVideoIndex);
        }

        public void PreviousVideo()
        {
            currentVideoIndex = (currentVideoIndex - 1 + npcVideoURLs.Count) % npcVideoURLs.Count;
            PlayVideoAndAudio(currentVideoIndex);
        }

        public void OnVideoSelected()
        {
            LeaderboardManager.Instance.RecordVideoSelection(currentVideoIndex);
        }

        private void PlayVideoAndAudio(int index)
        {
            if (npcVideoPlayer != null && npcVideoURLs.Count > index && !string.IsNullOrEmpty(npcVideoURLs[index]))
            {
                npcVideoPlayer.source = VideoSource.Url;
                npcVideoPlayer.url = npcVideoURLs[index];
                npcVideoPlayer.Prepare();
                npcVideoPlayer.Play();
            }
            else
            {
                Debug.LogWarning("NPC Video URL at index " + index + " is invalid.");
            }

            if (avatarVideoPlayer != null && avatarVideoURLs.Count > index && !string.IsNullOrEmpty(avatarVideoURLs[index]))
            {
                avatarVideoPlayer.source = VideoSource.Url;
                avatarVideoPlayer.url = avatarVideoURLs[index];
                avatarVideoPlayer.Prepare();
                avatarVideoPlayer.Play();
            }
            else
            {
                Debug.LogWarning("Avatar Video URL at index " + index + " is invalid.");
            }
        }

        private void OnVideosPrepared(VideoPlayer vp)
        {
            avatarRawImage.gameObject.SetActive(true);
            npcRawImage.gameObject.SetActive(true);
            arrowLeftRawImage.SetActive(true);
            arrowRightRawImage.SetActive(true);
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
            float avatarHeight = cachedAvatarSize.y;

            float yOffset = canvasHeight * 0.07f;

            float minX = -canvasWidth / 2 + avatarWidth / 2;
            float maxX = canvasWidth / 2 - avatarWidth / 2;
            float minY = (-canvasHeight / 2 + avatarHeight / 2) + yOffset;
            float maxY = (canvasHeight / 2 - avatarHeight / 2) + yOffset;

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
