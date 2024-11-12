using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections.Generic;

public class EndSceneManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private Material avatarMaterial;
    [SerializeField] private Material uiMaterial;
    [SerializeField] private float fadeDuration = 2f;

    [Header("UI")]
    [SerializeField] private GameObject backgroundImage;

    [Header("Video")]
    [SerializeField] private VideoPlayer avatarVideoPlayer;
    [SerializeField] private CanvasGroup avatarCanvasGroup;
    [SerializeField] private VideoPlayer endVideoPlayer;

    [Header("Swarm Management")]
    [SerializeField] private ManageSwarm manageSwarm;

    [Header("Audio")]
    [SerializeField] private List<AudioClip> endSceneAudioClips;

    private bool videosPrepared = false;
    private bool hasTriggeredEndVideo = false;

    void Start()
    {
        Camera.main.clearFlags = CameraClearFlags.Nothing;
        backgroundImage.SetActive(true);
        // Ensure the UI is invisible initially
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = 0;
            uiMaterial.SetFloat("_CanvasGroupAlpha", 0);
            avatarMaterial.SetFloat("_CanvasGroupAlpha", 0);
        }
        else
        {
            Debug.LogError("UI CanvasGroup is not assigned! Please check the Inspector.");
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlaylist(endSceneAudioClips, true);
        }

        // Start the avatar video fade-in and play process
        if (avatarVideoPlayer != null && avatarCanvasGroup != null)
        {
            StartCoroutine(PlayAvatarVideo());
        }
        else
        {
            Debug.LogError("Avatar VideoPlayer or Avatar CanvasGroup is not assigned!");
        }

        // Prepare the end video player and set it inactive
        if (endVideoPlayer != null)
        {
            endVideoPlayer.prepareCompleted += OnEndVideoPrepared;
            endVideoPlayer.Prepare();
        }
        else
        {
            Debug.LogError("End VideoPlayer is not assigned!");
        }
    }

    void Update()
    {
        if (!hasTriggeredEndVideo && AudioManager.Instance != null)
        {
            AudioSource audioSource = AudioManager.Instance.CurrentAudioSource;
            if (audioSource != null && audioSource.clip != null && audioSource.isPlaying)
            {
                float progress = audioSource.time / audioSource.clip.length;
                if (progress >= 0.85f)
                {
                    hasTriggeredEndVideo = true;
                    StartCoroutine(FadeOutUIAndPlayEndVideo());
                }
            }
        }
    }

    void OnEnable()
    {
        // Subscribe to VideoPlayer prepareCompleted event only once
        if (avatarVideoPlayer != null && !videosPrepared)
        {
            avatarVideoPlayer.prepareCompleted += OnAvatarVideoPrepared;
            avatarVideoPlayer.Prepare();
        }
    }

    void OnDisable()
    {
        // Unsubscribe from VideoPlayer events and stop the player without releasing the clip
        if (avatarVideoPlayer != null)
        {
            avatarVideoPlayer.prepareCompleted -= OnAvatarVideoPrepared;
            avatarVideoPlayer.Stop();
        }

        if (endVideoPlayer != null)
        {
            endVideoPlayer.prepareCompleted -= OnEndVideoPrepared;
            endVideoPlayer.Stop();
        }
    }

    private void OnAvatarVideoPrepared(VideoPlayer vp)
    {
        // Set the flag indicating videos are prepared
        videosPrepared = true;

        avatarVideoPlayer.Play();
    }

    private void OnEndVideoPrepared(VideoPlayer vp)
    {
        // You can set a flag here if needed or leave it empty
    }

    private void LoadFirstScene()
    {
        SceneManager.LoadScene("Lobby");
    }

    private IEnumerator PlayAvatarVideo()
    {
        while (!videosPrepared)
        {
            yield return null;
        }
        // Fade in the avatar video
        float elapsedTime = 0f;
        avatarCanvasGroup.alpha = 0;
        avatarMaterial.SetFloat("_CanvasGroupAlpha", 0);
        avatarVideoPlayer.Play();

        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
            avatarCanvasGroup.alpha = alpha;
            avatarMaterial.SetFloat("_CanvasGroupAlpha", alpha);
            uiMaterial.SetFloat("_CanvasGroupAlpha", alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        avatarCanvasGroup.alpha = 1;
        avatarMaterial.SetFloat("_CanvasGroupAlpha", 1);
        uiMaterial.SetFloat("_CanvasGroupAlpha", 1);

        // Wait until the avatar video is done playing
        while (avatarVideoPlayer.isPlaying)
        {
            yield return null;
        }

        // Fade out the avatar video
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            avatarCanvasGroup.alpha = alpha;
            avatarMaterial.SetFloat("_CanvasGroupAlpha", alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        avatarCanvasGroup.alpha = 0;
        avatarMaterial.SetFloat("_CanvasGroupAlpha", 0);

        // After the avatar video is done, fade in the UI
        StartCoroutine(FadeInUI());
    }

    private IEnumerator FadeOutUIAndPlayEndVideo()
    {
        // Fade out the UI
        float elapsedTime = 0f;
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

        // Activate and play the end video
        if (endVideoPlayer != null)
        {
            backgroundImage.SetActive(false);
            endVideoPlayer.Play();

            // Wait for the end video to finish playing
            while (endVideoPlayer.isPlaying)
            {
                yield return null;
            }

            // After the end video finishes, load the next scene
            LoadFirstScene();
        }
        else
        {
            Debug.LogError("End VideoPlayer is not assigned!");
        }
    }

    private IEnumerator FadeInUI()
    {
        // Initialize the swarm after the UI has fully faded in
        if (manageSwarm != null)
        {
            manageSwarm.InitializeSwarm();
        }
        else
        {
            Debug.LogError("ManageSwarm reference is not assigned! Please check the Inspector.");
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            float alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);

            uiCanvasGroup.alpha = alpha;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        uiCanvasGroup.alpha = 1;
    }
}
