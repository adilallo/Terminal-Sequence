using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class EndSceneManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private float fadeDuration = 2f;

    [Header("UI")]
    [SerializeField] private TMP_Text leaderboardText;
    [SerializeField] private RectTransform parentPanelRectTransform;

    [Header("Video")]
    [SerializeField] private VideoPlayer avatarVideoPlayer;  // Avatar Video Player
    [SerializeField] private CanvasGroup avatarCanvasGroup;  // CanvasGroup to control avatar video fade

    [Header("Audio")]
    [SerializeField] private List<AudioClip> endSceneAudioClips;

    [Header("Scrolling")]
    [SerializeField] private float scrollSpeed = 50f;
    private RectTransform leaderboardRectTransform;

    private bool videosPrepared = false;
    private bool leaderboardDisplayed = false;

    private Vector2 cachedParentPanelSize;
    private float textHeight;

    void Start()
    {
        leaderboardText.text = "";

        if (!leaderboardDisplayed)
        {
            DisplayLeaderboard();
            leaderboardDisplayed = true;
        }

        if (leaderboardText != null)
        {
            leaderboardRectTransform = leaderboardText.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("LeaderboardText is not assigned! Please check the Inspector.");
        }

        // Ensure the UI is invisible initially
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = 0;
        }
        else
        {
            Debug.LogError("UI CanvasGroup is not assigned! Please check the Inspector.");
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlaylist(endSceneAudioClips, true);
            AudioManager.Instance.OnPlaylistFinished += LoadFirstScene;
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

        CacheParentPanelDimensions();
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


    private void DisplayLeaderboard()
    {
        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("LeaderboardManager instance is missing.");
            return;
        }

        Dictionary<int, int> videoSelections = LeaderboardManager.Instance.GetAllVideoSelections();
        List<VideoClip> videoClips = LeaderboardManager.Instance.GetVideoClips();

        if (videoSelections == null || videoClips == null)
        {
            Debug.LogWarning("Leaderboard data is missing. Cannot display the leaderboard.");
            return;
        }

        List<KeyValuePair<int, int>> videoSelectionList = new List<KeyValuePair<int, int>>();

        foreach (var entry in videoSelections)
        {
            int videoIndex = entry.Key;
            int selectionCount = entry.Value;
            videoSelectionList.Add(new KeyValuePair<int, int>(videoIndex, selectionCount));
        }

        videoSelectionList.Sort((x, y) => y.Value.CompareTo(x.Value));

        System.Text.StringBuilder leaderboardBuilder = new System.Text.StringBuilder();

        for (int i = 0; i < videoSelectionList.Count; i++)
        {
            int videoIndex = videoSelectionList[i].Key;
            if (videoIndex >= videoClips.Count)
            {
                Debug.LogWarning($"Video index {videoIndex} is out of range.");
                continue;
            }
            string videoName = videoClips[videoIndex].name;
            int selectionCount = videoSelectionList[i].Value;

            leaderboardBuilder.AppendLine($"{videoName}: {selectionCount}");
        }

        leaderboardText.text = leaderboardBuilder.ToString();
    }

    private void LoadFirstScene()
    {
        SceneManager.LoadScene("Start");
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
        avatarVideoPlayer.Play();

        while (elapsedTime < fadeDuration)
        {
            avatarCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        avatarCanvasGroup.alpha = 1;

        // Wait until the avatar video is done playing
        while (avatarVideoPlayer.isPlaying)
        {
            yield return null;
        }

        // Fade out the avatar video
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            avatarCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        avatarCanvasGroup.alpha = 0;

        // After the avatar video is done, fade in the UI
        StartCoroutine(ScrollLeaderboardText());
        StartCoroutine(FadeInUI());
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

    private IEnumerator ScrollLeaderboardText()
    {
        if (leaderboardRectTransform == null || parentPanelRectTransform == null)
        {
            yield break;
        }

        leaderboardRectTransform.ForceUpdateRectTransforms();

        float textHeight = leaderboardRectTransform.rect.height;
        float parentHeight = parentPanelRectTransform.rect.height;

        if (cachedParentPanelSize == Vector2.zero)
        {
            cachedParentPanelSize = new Vector2(parentPanelRectTransform.rect.width, parentPanelRectTransform.rect.height);
        }

        Vector2 startPosition = new Vector2(leaderboardRectTransform.anchoredPosition.x, -textHeight);
        // Ending position above the parent panel
        Vector2 endPosition = new Vector2(leaderboardRectTransform.anchoredPosition.x, parentHeight + textHeight);

        leaderboardRectTransform.anchoredPosition = startPosition;

        while (true)
        {
            while (leaderboardRectTransform.anchoredPosition.y < endPosition.y)
            {
                leaderboardRectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);
                yield return null;
            }

            // Reset to start position
            leaderboardRectTransform.anchoredPosition = startPosition;
        }
    }

    private void CacheParentPanelDimensions()
    {
        if (parentPanelRectTransform != null)
        {
            cachedParentPanelSize = new Vector2(parentPanelRectTransform.rect.width, parentPanelRectTransform.rect.height);
        }
        else
        {
            Debug.LogError("Parent Panel RectTransform is not assigned!");
        }
    }
}