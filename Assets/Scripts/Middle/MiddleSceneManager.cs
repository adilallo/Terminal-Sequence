using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class MiddleSceneManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private float fadeDuration = 2f;

    [Header("UI")]
    [SerializeField] private VideoPlayer npcVideoPlayer;
    [SerializeField] private VideoPlayer avatarVideoPlayer;
    [SerializeField] private RectTransform npcRawImage;
    [SerializeField] private RectTransform avatarRawImage;
    [SerializeField] private VideoPlayer arrowVideoPlayer;
    [SerializeField] private GameObject arrowLeftRawImage;
    [SerializeField] private GameObject arrowRightRawImage;
    [SerializeField] private List<VideoClip> npcVideoClips;
    [SerializeField] private List<VideoClip> avatarVideoClips;
    [SerializeField] private Canvas canvas;

    [Header("Audio")]
    [SerializeField] private List<AudioClip> middleSceneAudioClips;

    private int currentVideoIndex = 0;
    private Vector2 avatarVelocity = new Vector2(100f, 100f);
    private RectTransform canvasRectTransform;

    private bool clipsSet = false;
    private bool videoPlayersPrepared = false;

    private Vector2 cachedCanvasSize;
    private Vector2 cachedAvatarSize;

    void Start()
    {
        Cursor.visible = false;

        npcRawImage.gameObject.SetActive(false);
        avatarRawImage.gameObject.SetActive(false);
        arrowLeftRawImage.SetActive(false);
        arrowRightRawImage.SetActive(false);

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = 0;
            StartCoroutine(FadeInUI());
        }

        currentVideoIndex = 0;

        if (!clipsSet && LeaderboardManager.Instance.GetVideoClips().Count == 0)
        {
            LeaderboardManager.Instance.SetVideoClips(npcVideoClips);
            clipsSet = true;
        }
        else
        {
            Debug.LogWarning("Video clips have already been assigned.");
            clipsSet = true;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlaylist(middleSceneAudioClips, false);
        }

        // Assign the Canvas RectTransform
        if (canvas != null)
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
            CacheCanvasAndAvatarDimensions();
        }
        else
        {
            Debug.LogError("Canvas is not assigned! Please assign a Canvas in the Inspector.");
        }

        // Check if avatarRawImage is assigned properly
        if (avatarRawImage == null)
        {
            Debug.LogError("avatarRawImage is not assigned! Please check the Inspector.");
        }
    }

    void OnEnable()
    {
        // Prepare VideoPlayers and subscribe to events only once
        if (!videoPlayersPrepared)
        {
            if (npcVideoPlayer != null)
            {
                npcVideoPlayer.prepareCompleted += OnVideosPrepared;
                npcVideoPlayer.Prepare();
            }
            if (arrowVideoPlayer != null)
            {
                arrowVideoPlayer.prepareCompleted += OnVideosPrepared;
                arrowVideoPlayer.Prepare();
            }
            videoPlayersPrepared = true;
        }
    }

    void Update()
    {
        MoveAvatarRawImage();  // Move the avatar every frame
    }

    private void OnDisable()
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

    public void NextVideo()
    {
        currentVideoIndex = (currentVideoIndex + 1) % npcVideoClips.Count;
        PlayVideoAndAudio(currentVideoIndex);
    }

    public void PreviousVideo()
    {
        currentVideoIndex = (currentVideoIndex - 1 + npcVideoClips.Count) % npcVideoClips.Count;
        PlayVideoAndAudio(currentVideoIndex);
    }

    public void OnVideoSelected()
    {
        LeaderboardManager.Instance.RecordVideoSelection(currentVideoIndex);
    }

    private void PlayVideoAndAudio(int index)
    {
        if (npcVideoClips.Count > 0 && npcVideoClips[index] != null)
        {
            npcVideoPlayer.clip = npcVideoClips[index];
            npcVideoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("NPC VideoClip at index " + index + " is null.");
        }

        if (avatarVideoClips.Count > index && avatarVideoClips[index] != null)
        {
            avatarVideoPlayer.clip = avatarVideoClips[index];
            avatarVideoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("Avatar VideoClip at index " + index + " is null.");
        }
    }

    private void OnVideosPrepared(VideoPlayer vp)
    {
        avatarRawImage.gameObject.SetActive(true);
        npcRawImage.gameObject.SetActive(true);
        arrowLeftRawImage.SetActive(true);
        arrowRightRawImage.SetActive(true);
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
        // Ensure both avatarRawImage and canvasRectTransform are not null
        if (avatarRawImage == null || canvasRectTransform == null)
        {
            Debug.LogError("Either avatarRawImage or canvasRectTransform is null. Movement cannot proceed.");
            return;
        }

        // Check if canvas or avatar size has changed and update cache if necessary
        Vector2 currentCanvasSize = new Vector2(canvasRectTransform.rect.width, canvasRectTransform.rect.height);
        Vector2 currentAvatarSize = new Vector2(avatarRawImage.rect.width, avatarRawImage.rect.height);
        if (currentCanvasSize != cachedCanvasSize || currentAvatarSize != cachedAvatarSize)
        {
            CacheCanvasAndAvatarDimensions();
        }

        // Get the current position of the avatar
        Vector2 currentPosition = avatarRawImage.anchoredPosition;

        // Update the position based on the velocity
        currentPosition += avatarVelocity * Time.deltaTime;

        float canvasWidth = cachedCanvasSize.x;
        float canvasHeight = cachedCanvasSize.y;
        float avatarWidth = cachedAvatarSize.x;
        float avatarHeight = cachedAvatarSize.y;

        float yOffset = canvasHeight * 0.07f;  // Adjust this value as needed to push everything up

        float minX = -canvasWidth / 2 + avatarWidth / 2;
        float maxX = canvasWidth / 2 - avatarWidth / 2;
        float minY = (-canvasHeight / 2 + avatarHeight / 2) + yOffset;
        float maxY = (canvasHeight / 2 - avatarHeight / 2) + yOffset;

        // Check and reverse direction if hitting horizontal edges
        if (currentPosition.x < minX)
        {
            currentPosition.x = minX;
            avatarVelocity.x *= -1;  // Move right
        }
        else if (currentPosition.x > maxX)
        {
            currentPosition.x = maxX;
            avatarVelocity.x *= -1;  // Move left
        }

        // Check and reverse direction if hitting vertical edges
        if (currentPosition.y < minY)
        {
            currentPosition.y = minY;
            avatarVelocity.y *= -1;   // Move up
        }
        else if (currentPosition.y > maxY)
        {
            currentPosition.y = maxY;
            avatarVelocity.y *= -1;   // Move down
        }

        avatarRawImage.anchoredPosition = currentPosition;
    }
}
