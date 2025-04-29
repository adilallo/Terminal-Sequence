using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

public class FlickrImageLoaderOnline : MonoBehaviour
{
    // REMOTE (online) location
    private string baseUrl =
        "https://raw.githubusercontent.com/adilallo/No_Vacancy/feature/adilallo/ISG/Assets/Editor/FlickrImages/";

    [Header("UI Elements")]
    public RawImage[] displayImages;                 // Array of UI elements to display images
    [SerializeField] private RectTransform parentRect; // Reference to the parent RectTransform (e.g., Canvas)

    [Header("Materials & Scripts")]
    [SerializeField] private Material enhancedWeaveBlendMaterial; // Assign the enhanced material in the Inspector
    [SerializeField] private PixelSorter pixelSorter;             // Reference to the PixelSorter script

    [Header("Fallback Settings")]
    [Tooltip("Local textures to use if remote download fails.")]
    public Texture2D[] fallbackImages;  // Assign your local images here in the Inspector

    private int totalImages = 99; // Number of remote images available (000 to 098)

    void Start()
    {
        // Validate parentRect
        if (parentRect == null)
        {
            parentRect = GetComponent<RectTransform>();
            if (parentRect == null)
            {
                Debug.LogError("Parent RectTransform not assigned and not found on the GameObject.");
                return;
            }
        }

        // Add CanvasGroup to each RawImage for individual fading
        foreach (RawImage rawImage in displayImages)
        {
            CanvasGroup canvasGroup = rawImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = rawImage.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f; // Initially fully visible
        }

        // Arrange images with overlap
        ArrangeImagesWithOverlap();

        // Populate all RawImage components initially
        PopulateAllImages();

        // Start the update loop every 1 second
        InvokeRepeating(nameof(UpdateRandomImages), 1f, 1f);
    }

    /// <summary>
    /// Arranges RawImages in a layout that fills the canvas and adds overlap between them.
    /// </summary>
    void ArrangeImagesWithOverlap()
    {
        int n = displayImages.Length;
        int rows, columns;
        DetermineGridLayout(n, out rows, out columns);

        // Calculate cell size based on parent RectTransform with overlap allowance
        float cellWidth = parentRect.rect.width / columns * 1.2f;
        float cellHeight = parentRect.rect.height / rows * 1.2f;

        float overlapMargin = 0.15f; // Overlap margin

        for (int i = 0; i < n; i++)
        {
            int row = i / columns;
            int col = i % columns;

            float anchorMinX = (float)col / columns - overlapMargin;
            float anchorMaxX = (float)(col + 1) / columns + overlapMargin;
            float anchorMinY = 1f - ((float)(row + 1) / rows) - overlapMargin;
            float anchorMaxY = 1f - ((float)row / rows) + overlapMargin;

            // Clamp to ensure anchors stay within [0,1]
            anchorMinX = Mathf.Clamp01(anchorMinX);
            anchorMaxX = Mathf.Clamp01(anchorMaxX);
            anchorMinY = Mathf.Clamp01(anchorMinY);
            anchorMaxY = Mathf.Clamp01(anchorMaxY);

            // Assign to RawImage's RectTransform
            RectTransform rt = displayImages[i].GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            rt.localScale = Vector3.one;

            // Random offset for a more organic look
            float randomOffsetX = Random.Range(-0.08f, 0.08f) * parentRect.rect.width / columns;
            float randomOffsetY = Random.Range(-0.08f, 0.08f) * parentRect.rect.height / rows;
            rt.anchoredPosition += new Vector2(randomOffsetX, randomOffsetY);
        }
    }

    /// <summary>
    /// Determines the optimal grid layout (rows and columns) for the given number of images.
    /// </summary>
    /// <param name="n">Number of images.</param>
    /// <param name="rows">Output number of rows.</param>
    /// <param name="columns">Output number of columns.</param>
    void DetermineGridLayout(int n, out int rows, out int columns)
    {
        // Start with a square-ish grid
        rows = Mathf.CeilToInt(Mathf.Sqrt(n));
        columns = Mathf.CeilToInt((float)n / rows);
        // Adjust if needed
        while (rows * columns < n) columns++;

        Debug.Log($"Grid Layout: Rows = {rows}, Columns = {columns}");
    }

    /// <summary>
    /// Populates all RawImage components by downloading (or falling back), sorting, and assigning images.
    /// </summary>
    void PopulateAllImages()
    {
        for (int i = 0; i < displayImages.Length; i++)
        {
            // Pick a random remote image index from 000 to 098
            int randomImageNumber = Random.Range(0, totalImages);
            // Construct the remote image URL
            string imageUrl = $"{baseUrl}{randomImageNumber:D3}.jpg";

            // Assign the enhanced custom material (if available)
            if (enhancedWeaveBlendMaterial != null)
            {
                displayImages[i].material = enhancedWeaveBlendMaterial;
            }
            else
            {
                Debug.LogWarning("EnhancedWeaveBlendMaterial is not assigned in the Inspector.");
            }

            // Start a coroutine to download OR fallback, then pixel-sort, then crossfade
            StartCoroutine(DownloadSortAndCrossfadeImage(imageUrl, displayImages[i]));
        }
    }

    /// <summary>
    /// Updates a random RawImage component with a new random image every second.
    /// </summary>
    void UpdateRandomImages()
    {
        // Pick a random RawImage component
        int randomIndex = Random.Range(0, displayImages.Length);
        RawImage targetImage = displayImages[randomIndex];

        // Pick a random remote image index
        int randomImageNumber = Random.Range(0, totalImages);

        // Construct the URL
        string imageUrl = $"{baseUrl}{randomImageNumber:D3}.jpg";

        // Download or fallback
        StartCoroutine(DownloadSortAndCrossfadeImage(imageUrl, targetImage));
    }

    /// <summary>
    /// Downloads an image, applies pixel sorting, and assigns it to the target RawImage with a crossfade effect.  
    /// If the download fails, it uses a random fallback image from the local array.
    /// </summary>
    IEnumerator DownloadSortAndCrossfadeImage(string url, RawImage targetImage)
    {
        // Attempt to download the image
        UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(url);
        yield return textureRequest.SendWebRequest();

        Texture2D finalTexture = null;

        if (textureRequest.result == UnityWebRequest.Result.Success)
        {
            // Got a remote texture
            Texture2D downloadedTexture = ((DownloadHandlerTexture)textureRequest.downloadHandler).texture;
            // Sort via pixelSorter
            finalTexture = pixelSorter.SortTexture(downloadedTexture);
        }
        else
        {
            Debug.Log($"Error downloading image from {url}: {textureRequest.error}");

            // ------------- FALLBACK MODE -------------
            if (fallbackImages != null && fallbackImages.Length > 0)
            {
                // Pick one random image from the fallback array
                int randomFallbackIndex = Random.Range(0, fallbackImages.Length);
                Texture2D fallbackTexture = fallbackImages[randomFallbackIndex];

                // Sort via pixelSorter
                finalTexture = pixelSorter.SortTexture(fallbackTexture);
            }
            else
            {
                // If no fallback images are assigned, just quit
                Debug.LogWarning("No fallback images available. Cannot load an image.");
                yield break;
            }
        }

        // Start the crossfade (finalTexture should never be null if we reached here)
        StartCoroutine(CrossfadeImage(targetImage, finalTexture, 3.0f));
    }

    /// <summary>
    /// Crossfades from the current image to a new texture.
    /// </summary>
    IEnumerator CrossfadeImage(RawImage targetImage, Texture2D newTexture, float duration)
    {
        // Create a temporary RawImage for the new texture
        GameObject newImageObj = new GameObject("TempImage");
        newImageObj.transform.SetParent(parentRect, false);
        RawImage newImage = newImageObj.AddComponent<RawImage>();
        newImage.texture = newTexture;
        newImage.material = enhancedWeaveBlendMaterial;

        RectTransform targetRect = targetImage.GetComponent<RectTransform>();
        RectTransform newRect = newImage.GetComponent<RectTransform>();
        newRect.anchorMin = targetRect.anchorMin;
        newRect.anchorMax = targetRect.anchorMax;
        newRect.pivot = targetRect.pivot;
        newRect.sizeDelta = targetRect.sizeDelta;
        newRect.anchoredPosition = targetRect.anchoredPosition;

        // Ensure new image is on top
        newImageObj.transform.SetSiblingIndex(targetRect.GetSiblingIndex() + 1);

        // Add CanvasGroup for fading
        CanvasGroup newCanvasGroup = newImageObj.AddComponent<CanvasGroup>();
        newCanvasGroup.alpha = 0f;

        // Add or retrieve CanvasGroup for old image
        CanvasGroup oldCanvasGroup = targetImage.GetComponent<CanvasGroup>();
        if (oldCanvasGroup == null)
        {
            oldCanvasGroup = targetImage.gameObject.AddComponent<CanvasGroup>();
            oldCanvasGroup.alpha = 1f;
        }

        // Crossfade
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            newCanvasGroup.alpha = Mathf.Lerp(0, 1, t);
            oldCanvasGroup.alpha = Mathf.Lerp(1, 0, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        newCanvasGroup.alpha = 1f;
        oldCanvasGroup.alpha = 0f;

        // Assign the new texture to the target image
        targetImage.texture = newTexture;

        // Reset the old image's alpha
        oldCanvasGroup.alpha = 1f;

        // Cleanup temporary object
        Destroy(newImageObj);
    }
}
