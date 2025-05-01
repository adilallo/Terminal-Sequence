using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

namespace StartScene
{
    public class StartSceneManager : MonoBehaviour
    {
        /* ─── Serialized ─────────────────────────────────────────────────── */

        [Header("Fade Targets")]
        [SerializeField] CanvasGroup videoCanvasGroup;
        [SerializeField] CanvasGroup uiCanvasGroup;
        [SerializeField] Material uiMaterial;
        [SerializeField] Material avatarMaterial;
        [SerializeField] float fadeDuration = 2f;

        [Header("Intro Assets")]
        [SerializeField] VideoPlayer introVideoPlayer;
        [SerializeField] RawImage introDisplay;

        [Header("UI")]
        [SerializeField] GameObject UI;
        [SerializeField] VideoPlayer stockVideoPlayer;
        [SerializeField] RawImage stockRawImage;
        [SerializeField] VideoPlayer avatarVideoPlayer;

        [Header("Audio")]
        [SerializeField] List<AudioClip> startSceneAudioClips;

        /* ─── private fields ─────────────────────────────────────────────── */

        bool selectButtonVisible;
        bool stockVideoStarted;
        bool videosPrepared;

        Color baseStockColor;

        readonly Material[] fadeMats = new Material[2];

        /* ─── Unity lifecycle ────────────────────────────────────────────── */

        void Awake()
        {
            fadeMats[0] = uiMaterial;
            fadeMats[1] = avatarMaterial;
        }

        void Start()
        {
            if (introVideoPlayer)
            {
                var rt = new RenderTexture(600, 960, 0, RenderTextureFormat.ARGB32);
                introVideoPlayer.targetTexture = rt;

                // Point every RawImage or material at that RT
                introDisplay.texture = rt;          // RawImage that shows intro
            }

            UI.SetActive(false);
            stockRawImage.color = new Color(1, 1, 1, 0);
            SetAlphaAll(0f); uiCanvasGroup.alpha = 0;
            uiCanvasGroup.interactable = false;

            AudioManager.Instance?.PlayPlaylist(startSceneAudioClips, false);
            StartCoroutine(PrepareVideos());
            StartCoroutine(PlayIntroWhenReady());
        }

        void Update()
        {
            if (!avatarVideoPlayer || !avatarVideoPlayer.isPlaying) return;

            if (!selectButtonVisible &&
                avatarVideoPlayer.time >= avatarVideoPlayer.length * .93f)
            {
                StartCoroutine(FadeMaterial(uiMaterial, uiCanvasGroup, 0f, 1f, fadeDuration));
                selectButtonVisible = true;
            }

            if (!stockVideoStarted &&
                avatarVideoPlayer.time >= avatarVideoPlayer.length * .5625f)
            {
                StartCoroutine(FadeInStockVideo());
                stockVideoStarted = true;
            }
        }

        void OnEnable()
        {
            if (introVideoPlayer && !videosPrepared)
            {
                introVideoPlayer.loopPointReached += OnIntroFinished;
                videosPrepared = true;
            }
            if (avatarVideoPlayer)
                avatarVideoPlayer.prepareCompleted += _ => { /* prepared */ };
        }

        void OnDisable() => StopAllPlayers();

        /* ─── Video prep / playback ──────────────────────────────────────── */

        IEnumerator PrepareVideos()
        {
            if (introVideoPlayer) introVideoPlayer.Prepare();
            if (stockVideoPlayer) stockVideoPlayer.Prepare();
            if (avatarVideoPlayer) avatarVideoPlayer.Prepare();

            while ((introVideoPlayer && !introVideoPlayer.isPrepared) ||
                   (stockVideoPlayer && !stockVideoPlayer.isPrepared) ||
                   (avatarVideoPlayer && !avatarVideoPlayer.isPrepared))
                yield return null;
        }

        IEnumerator PlayIntroWhenReady()
        {
            while (!introVideoPlayer || !introVideoPlayer.isPrepared) yield return null;
            introVideoPlayer.Play();
        }

        void OnIntroFinished(VideoPlayer vp)
        {
            vp.time = 0;  
            StartCoroutine(FadeInAvatar());

            avatarVideoPlayer?.Play();
            UI.SetActive(true);
        }

        /* ─── Fade helpers ──────────────────────────────────────────────── */

        IEnumerator FadeInAvatar()
        {
            yield return FadeMaterial(avatarMaterial, videoCanvasGroup, 0f, 1f, fadeDuration);
        }

        IEnumerator FadeInStockVideo()
        {
            if (!stockVideoPlayer || !stockRawImage) yield break;
            stockVideoPlayer.Play();

            float t = 0;
            while (t < fadeDuration)
            {
                float a = t / fadeDuration;
                stockRawImage.color = new Color(1, 1, 1, a);
                t += Time.deltaTime;
                yield return null;
            }
            stockRawImage.color = Color.white;
        }

        IEnumerator FadeMaterial(Material mat, CanvasGroup can, float from, float to,
                                 float dur, System.Action onDone = null)
        {
            float t = 0;
            while (t < dur)
            {
                float a = Mathf.Lerp(from, to, t / dur);
                mat.SetFloat("_CanvasGroupAlpha", a);
                can.alpha = a;      // for UI fade
                t += Time.deltaTime;
                yield return null;
            }
            mat.SetFloat("_CanvasGroupAlpha", to);
            can.alpha = to;
            onDone?.Invoke();
        }

        void SetAlphaAll(float a)
        {
            for (int i = 0; i < fadeMats.Length; i++)
                if (fadeMats[i]) fadeMats[i].SetFloat("_CanvasGroupAlpha", a);
        }

        /* ─── cleanup ───────────────────────────────────────────────────── */

        void StopAllPlayers()
        {
            if (introVideoPlayer)
                introVideoPlayer.loopPointReached -= OnIntroFinished;

            VideoPlayer[] vps = { introVideoPlayer, stockVideoPlayer, avatarVideoPlayer };
            foreach (var vp in vps)
                if (vp) vp.Stop();
        }
    }
}
