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
        #region ─── Serialized

        [SerializeField] CanvasGroup uiCanvasGroup;
        [SerializeField] CanvasGroup textCanvasGroup;
        [SerializeField] Material avatarMaterial;
        [SerializeField] Material npcMaterial;
        [SerializeField] Material uiMaterial;
        [SerializeField] float fadeDuration = 2f;

        [Header("UI")]
        [SerializeField] VideoPlayer npcVideoPlayer;
        [SerializeField] VideoPlayer avatarVideoPlayer;
        [SerializeField] RectTransform npcRawImage;
        [SerializeField] RectTransform avatarRawImage;
        [SerializeField] VideoPlayer arrowVideoPlayer;
        [SerializeField] GameObject arrowLeftRawImage;
        [SerializeField] GameObject arrowRightRawImage;
        [SerializeField] Canvas canvas;

        [Header("Audio")]
        [SerializeField] List<AudioClip> middleSceneAudioClips;

        [Header("Video URLs / Fallbacks")]
        [SerializeField] List<string> npcVideoURLs;
        [SerializeField] List<string> avatarVideoURLs;
        [SerializeField] List<VideoClip> npcFallbackClips;
        [SerializeField] List<VideoClip> avatarFallbackClips;

        [SerializeField] GoogleSheetsHandler googleSheetsHandler;

        #endregion

        #region ─── Cached fields

        int currentVideoIndex;
        Vector2 avatarVelocity = new(100f, 100f);

        bool avatarStarted, npcStarted;
        bool isOffline;

        Material[] fadeMats;                     // batch SetFloat calls
        SceneChanger sceneChanger;

        // cached bounds to avoid per-frame Rect allocations
        Vector2 canvasSize, avatarSize;
        int cachedScreenW, cachedScreenH;

        // reused temp vector (avoids new Vector2 each frame)
        Vector2 tmpVec2 = new();

        #endregion

        /*────────────────────────────────────────────────────────────────────────*/

        void Awake()
        {
            fadeMats = new[] { avatarMaterial, npcMaterial, uiMaterial };
            sceneChanger = FindFirstObjectByType<SceneChanger>();
            isOffline = Application.internetReachability == NetworkReachability.NotReachable;
        }

        void Start()
        {
            Cursor.visible = false;

            // ensure objects hidden
            npcRawImage.gameObject.SetActive(false);
            avatarRawImage.gameObject.SetActive(false);
            arrowLeftRawImage.SetActive(false);
            arrowRightRawImage.SetActive(false);

            // fade-in coroutine
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.alpha = 0f;
            textCanvasGroup.alpha = 0f;
            SetAlphaAll(0f);
            StartCoroutine(FadeInTextUI());
            StartCoroutine(FadeInUI());

            currentVideoIndex = 0;

            AudioManager.Instance?.PlayPlaylist(middleSceneAudioClips, false);

            if (!canvas) Debug.LogError("Canvas missing!");
            CacheBounds();

            PlayVideoAndAudio(currentVideoIndex);
        }

        void Update() => MoveAvatarRawImage();

        void OnDisable() => CleanupVideoPlayers();

        /*────────────────────────────────────────────────────────────────────────*
         *                    INITIALISATION / CLEANUP                           *
        /*────────────────────────────────────────────────────────────────────────*/

        void CleanupVideoPlayers()
        {
            if (npcVideoPlayer)
            {
                npcVideoPlayer.errorReceived -= OnNPCVideoError;
                npcVideoPlayer.prepareCompleted -= OnNPCVideoPrepared;
                npcVideoPlayer.started -= OnNPCVideoStarted;
                npcVideoPlayer.Stop();
            }
            if (avatarVideoPlayer)
            {
                avatarVideoPlayer.errorReceived -= OnAvatarVideoError;
                avatarVideoPlayer.prepareCompleted -= OnAvatarVideoPrepared;
                avatarVideoPlayer.started -= OnAvatarVideoStarted;
                avatarVideoPlayer.Stop();
            }
        }

        /*────────────────────────────────────────────────────────────────────────*
         *                         VIDEO CONTROL                                 *
        /*────────────────────────────────────────────────────────────────────────*/

        public void NextVideo() => StartCoroutine(FadeSwapVideo((currentVideoIndex + 1) % npcVideoURLs.Count));
        public void PreviousVideo() => StartCoroutine(FadeSwapVideo((currentVideoIndex - 1 + npcVideoURLs.Count) % npcVideoURLs.Count));

        public async void OnVideoSelected()
        {
            uiCanvasGroup.interactable = false;
            googleSheetsHandler?.RecordVideoSelection(currentVideoIndex);
            await FadeOutUICoroutine();
            sceneChanger.LoadThirdScene();
        }

        /*── helpers ────────────────────────────────────────────────────────────*/

        IEnumerator FadeSwapVideo(int newIndex)
        {
            yield return FadeMaterials(1f, 0f, .75f);

            npcRawImage.gameObject.SetActive(false);
            avatarRawImage.gameObject.SetActive(false);
            npcVideoPlayer?.Stop();
            avatarVideoPlayer?.Stop();

            currentVideoIndex = newIndex;
            PlayVideoAndAudio(currentVideoIndex);

            while (!npcStarted || !avatarStarted) yield return null;
            yield return FadeMaterials(0f, 1f, .75f);
        }

        void PlayVideoAndAudio(int idx)
        {
            avatarStarted = npcStarted = false;

            // ----- NPC -----
            PreparePlayer(npcVideoPlayer, idx,
                          npcVideoURLs, npcFallbackClips,
                          OnNPCVideoError, OnNPCVideoPrepared);

            // ----- AVATAR -----
            PreparePlayer(avatarVideoPlayer, idx,
                          avatarVideoURLs, avatarFallbackClips,
                          OnAvatarVideoError, OnAvatarVideoPrepared);
        }

        static void PreparePlayer(VideoPlayer vp, int idx,
                                   List<string> urls, List<VideoClip> fallbacks,
                                   VideoPlayer.ErrorEventHandler err, VideoPlayer.EventHandler prepared)
        {
            if (!vp) return;

            vp.errorReceived -= err;
            vp.prepareCompleted -= prepared;

            if (urls.Count > idx && !string.IsNullOrEmpty(urls[idx]))
            {
                vp.source = VideoSource.Url;
                vp.url = urls[idx];
            }
            else if (fallbacks.Count > idx && fallbacks[idx])
            {
                vp.source = VideoSource.VideoClip;
                vp.clip = fallbacks[idx];
            }
            else
            {
                Debug.LogWarning($"No valid source for {vp.name} at index {idx}");
                return;
            }

            vp.errorReceived += err;
            vp.prepareCompleted += prepared;
            vp.Prepare();
        }

        /* video callbacks */
        void OnNPCVideoError(VideoPlayer s, string m) { Debug.LogWarning($"NPC error: {m}"); PlayNPCFallback(currentVideoIndex); }
        void OnAvatarVideoError(VideoPlayer s, string m) { Debug.LogWarning($"Avatar error: {m}"); PlayAvatarFallback(currentVideoIndex); }

        void OnNPCVideoPrepared(VideoPlayer s) { s.prepareCompleted -= OnNPCVideoPrepared; s.started += OnNPCVideoStarted; s.Play(); }
        void OnAvatarVideoPrepared(VideoPlayer s) { s.prepareCompleted -= OnAvatarVideoPrepared; s.started += OnAvatarVideoStarted; s.Play(); }

        void OnNPCVideoStarted(VideoPlayer s) { s.started -= OnNPCVideoStarted; npcRawImage.gameObject.SetActive(true); npcStarted = true; CheckBothStarted(); }
        void OnAvatarVideoStarted(VideoPlayer s) { s.started -= OnAvatarVideoStarted; avatarRawImage.gameObject.SetActive(true); avatarStarted = true; CheckBothStarted(); }

        void CheckBothStarted()
        {
            if (npcStarted && avatarStarted)
            {
                arrowLeftRawImage.SetActive(true);
                arrowRightRawImage.SetActive(true);
            }
        }

        void PlayNPCFallback(int i) => PlayClip(npcVideoPlayer, npcFallbackClips, i, OnNPCVideoPrepared);
        void PlayAvatarFallback(int i) => PlayClip(avatarVideoPlayer, avatarFallbackClips, i, OnAvatarVideoPrepared);

        static void PlayClip(VideoPlayer vp, List<VideoClip> clips, int idx, VideoPlayer.EventHandler prepared)
        {
            if (!vp || clips.Count <= idx || clips[idx] == null) return;
            vp.source = VideoSource.VideoClip;
            vp.clip = clips[idx];
            vp.prepareCompleted += prepared;
            vp.Prepare();
        }

        /*────────────────────────────────────────────────────────────────────────*
         *                           FADING                                      *
        /*────────────────────────────────────────────────────────────────────────*/

        IEnumerator FadeInUI()
        {
            yield return FadeMaterials(0f, 1f, fadeDuration);
            uiCanvasGroup.interactable = true;
        }

        async Task FadeOutUICoroutine()
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(FadeMaterials(1f, 0f, fadeDuration, () => tcs.SetResult(true)));
            await tcs.Task;
            uiCanvasGroup.interactable = false;
        }

        IEnumerator FadeMaterials(float from, float to, float dur, System.Action onDone = null)
        {
            float t = 0f;
            while (t < dur)
            {
                float a = Mathf.Lerp(from, to, t / dur);
                uiCanvasGroup.alpha = a;
                SetAlphaAll(a);
                t += Time.deltaTime;
                yield return null;
            }
            uiCanvasGroup.alpha = to;
            SetAlphaAll(to);
            onDone?.Invoke();
        }

        IEnumerator FadeInTextUI()
        {
            float t = 0f;
            float dur = fadeDuration;
            textCanvasGroup.alpha = 0f;
            textCanvasGroup.interactable = false;
            textCanvasGroup.blocksRaycasts = false;

            while (t < dur)
            {
                float a = Mathf.Lerp(0f, 1f, t / dur);
                textCanvasGroup.alpha = a;
                t += Time.deltaTime;
                yield return null;
            }

            textCanvasGroup.alpha = 1f;
        }

        void SetAlphaAll(float a)
        {
            for (int i = 0; i < fadeMats.Length; i++)
                if (fadeMats[i]) fadeMats[i].SetFloat("_CanvasGroupAlpha", a);
        }

        /*────────────────────────────────────────────────────────────────────────*
         *                 AVATAR MOVEMENT   (no allocations)                    *
        /*────────────────────────────────────────────────────────────────────────*/

        void CacheBounds()
        {
            if (!canvas) return;
            canvasSize = new Vector2(canvas.pixelRect.width, canvas.pixelRect.height);
            avatarSize = avatarRawImage ? avatarRawImage.sizeDelta : Vector2.zero;
            cachedScreenW = Screen.width;
            cachedScreenH = Screen.height;
        }

        void MoveAvatarRawImage()
        {
            if (!avatarRawImage) return;

            if (Screen.width != cachedScreenW || Screen.height != cachedScreenH)
                CacheBounds();

            tmpVec2.Set(avatarRawImage.anchoredPosition.x + avatarVelocity.x * Time.deltaTime,
                        avatarRawImage.anchoredPosition.y + avatarVelocity.y * Time.deltaTime);

            float w = canvasSize.x;
            float h = canvasSize.y;
            float aw = avatarSize.x;
            float ah = avatarSize.y + 100f;            // y offset fudge

            float yOff = h * 0.046f;

            float minX = -w * .5f + aw * .5f;
            float maxX = w * .5f - aw * .5f;
            float minY = -h * .5f + ah * .5f + yOff;
            float maxY = h * .5f - ah * .5f + yOff;

            if (tmpVec2.x < minX) { tmpVec2.x = minX; avatarVelocity.x *= -1; }
            else if (tmpVec2.x > maxX) { tmpVec2.x = maxX; avatarVelocity.x *= -1; }
            if (tmpVec2.y < minY) { tmpVec2.y = minY; avatarVelocity.y *= -1; }
            else if (tmpVec2.y > maxY) { tmpVec2.y = maxY; avatarVelocity.y *= -1; }

            avatarRawImage.anchoredPosition = tmpVec2;
        }
    }
}
