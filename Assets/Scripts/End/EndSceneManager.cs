using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class EndSceneManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private Material avatarMaterial;
    [SerializeField] private Material uiMaterial;
    [SerializeField] private float fadeDuration = 2f;

    [Header("UI")]

    [Header("Video")]
    [SerializeField] private VideoPlayer avatarVideoPlayer;  // Avatar Video Player
    [SerializeField] private CanvasGroup avatarCanvasGroup;  // CanvasGroup to control avatar video fade
    [SerializeField] private VideoPlayer endVideoPlayer;
    [SerializeField] private RawImage endVideo;

    [Header("Audio")]
    [SerializeField] private List<AudioClip> endSceneAudioClips;

    private bool videosPrepared = false;

    void Start()
    {
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
            AudioManager.Instance.OnPlaylistFinished += PlayEndVideo;
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
    }

    void OnEnable()
    {
        // Subscribe to VideoPlayer prepareCompleted event only once
        if (avatarVideoPlayer != null && !videosPrepared)
        {
            avatarVideoPlayer.prepareCompleted += OnAvatarVideoPrepared;
            avatarVideoPlayer.Prepare();
        }

        if (endVideoPlayer != null)
        {
            endVideoPlayer.Prepare();
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

        // Unsubscribe from AudioManager event
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnPlaylistFinished -= LoadFirstScene;
        }
    }


    private void OnAvatarVideoPrepared(VideoPlayer vp)
    {
        // Set the flag indicating videos are prepared
        videosPrepared = true;

        avatarVideoPlayer.Play();
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
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        avatarCanvasGroup.alpha = 1;
         avatarMaterial.SetFloat("_CanvasGroupAlpha", 1);

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

    private void PlayEndVideo()
    {
        if (endVideoPlayer != null)
        {
            endVideoPlayer.Play();
        }
        else
        {
            Debug.LogError("End VideoPlayer is not assigned!");
        }
    }

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
}