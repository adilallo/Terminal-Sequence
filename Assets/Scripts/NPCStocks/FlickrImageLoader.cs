using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

public class FlickrImageLoader : MonoBehaviour
{
    // Reference to the images in the FlickrImages folder
    [Header("UI Elements")]
    public RawImage[] displayImages;              // Array of UI elements to display images
    [SerializeField] private RectTransform parentRect; // Reference to the parent RectTransform (e.g., Canvas or graphArea)

    [Header("Materials & Scripts")]
    [SerializeField] private Material enhancedWeaveBlendMaterial; // Assign the enhanced material in the Inspector
    [SerializeField] private PixelSorter pixelSorter;               // Reference to the PixelSorter script

    private int totalImages = 99;             // Number of images available (000 to 098)

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
            canvasGroup.alpha = 1f; // Initially make them fully visible
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
        float cellWidth = parentRect.rect.width / columns * 1.2f;  // Increase width for overlap
        float cellHeight = parentRect.rect.height / rows * 1.2f;   // Increase height for overlap

        float overlapMargin = 0.15f; // Increased overlap margin

        for (int i = 0; i < n; i++)
        {
            int row = i / columns;
            int col = i % columns;

            // Calculate anchorMin and anchorMax for the cell with increased overlap
            float anchorMinX = (float)col / columns - overlapMargin;  // Offset for overlap
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

            // Ensure the image fills and slightly exceeds its cell
            rt.localScale = Vector3.one;

            // Randomly adjust the position slightly for a more organic look
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
        // Start with a square grid
        rows = Mathf.CeilToInt(Mathf.Sqrt(n));
        columns = Mathf.CeilToInt((float)n / rows);

        // Adjust to ensure all images fit
        while (rows * columns < n)
        {
            columns++;
        }

        // Debugging log
        Debug.Log($"Grid Layout: Rows = {rows}, Columns = {columns}, Total Cells = {rows * columns}");
    }

    /// <summary>
    /// Populates all RawImage components by loading images from the FlickrImages folder.
    /// </summary>
    void PopulateAllImages()
    {
        for (int i = 0; i < displayImages.Length; i++)
        {
            // Pick a random image index from 000 to 098
            int randomImageNumber = Random.Range(0, totalImages);

            // Construct the image file path from the local Assets folder
            string imagePath = $"Assets/FlickrImages/{randomImageNumber:D3}.jpg";

            // Assign the enhanced custom material to handle blending and waving
            if (enhancedWeaveBlendMaterial != null)
            {
                displayImages[i].material = enhancedWeaveBlendMaterial;
            }
            else
            {
                Debug.LogWarning("EnhancedWeaveBlendMaterial is not assigned in the Inspector.");
            }

            // Start a coroutine to load, sort, and display the image with crossfade
            StartCoroutine(LoadSortAndCrossfadeImage(imagePath, displayImages[i]));
        }
    }

    /// <summary>
    /// Updates a random RawImage component with a new random image every second.
    /// </summary>
    void UpdateRandomImages()
    {
        // Pick a random RawImage component from the array
        int randomIndex = Random.Range(0, displayImages.Length);
        RawImage targetImage = displayImages[randomIndex];

        // Pick a random image index from 000 to 098
        int randomImageNumber = Random.Range(0, totalImages);

        // Construct the image file path from the local Assets folder
        string imagePath = $"Assets/FlickrImages/{randomImageNumber:D3}.jpg";

        // Start a coroutine to load, sort, and display the image with crossfade
        StartCoroutine(LoadSortAndCrossfadeImage(imagePath, targetImage));
    }

    /// <summary>
    /// Loads an image, applies pixel sorting, and assigns it to the target RawImage with a crossfade effect.
    /// </summary>
    /// <param name="path">Path of the image to load.</param>
    /// <param name="targetImage">RawImage component to assign the image to.</param>
    /// <returns>IEnumerator for coroutine.</returns>
    IEnumerator LoadSortAndCrossfadeImage(string path, RawImage targetImage)
    {
        // Load the image as a Texture2D
        Texture2D newTexture = new Texture2D(2, 2);
        byte[] imageData = System.IO.File.ReadAllBytes(path);
        if (imageData == null || imageData.Length == 0)
        {
            Debug.LogError($"Error loading image from {path}");
            yield break;
        }
        newTexture.LoadImage(imageData);

        // Sort the image using PixelSorter
        Texture2D sortedTexture = pixelSorter.SortTexture(newTexture);

        // Start the crossfade
        StartCoroutine(CrossfadeImage(targetImage, sortedTexture, 3.0f));
    }

    /// <summary>
    /// Crossfades from the current image to a new texture.
    /// </summary>
    /// <param name="targetImage">RawImage component to crossfade.</param>
    /// <param name="newTexture">New texture to apply.</param>
    /// <param name="duration">Duration of the crossfade in seconds.</param>
    /// <returns>IEnumerator for coroutine.</returns>
    IEnumerator CrossfadeImage(RawImage targetImage, Texture2D newTexture, float duration)
    {
        // Create a temporary RawImage for the new texture
        GameObject newImageObj = new GameObject("TempImage");
        newImageObj.transform.SetParent(parentRect, false); // Set parent to the same as original RawImages
        RawImage newImage = newImageObj.AddComponent<RawImage>();
        newImage.texture = newTexture;
        newImage.material = enhancedWeaveBlendMaterial;

        // Match the RectTransform of the target image
        RectTransform targetRect = targetImage.GetComponent<RectTransform>();
        RectTransform newRect = newImage.GetComponent<RectTransform>();
        newRect.anchorMin = targetRect.anchorMin;
        newRect.anchorMax = targetRect.anchorMax;
        newRect.pivot = targetRect.pivot;
        newRect.sizeDelta = targetRect.sizeDelta;
        newRect.anchoredPosition = targetRect.anchoredPosition;

        // Ensure the new image is on top
        newImageObj.transform.SetSiblingIndex(targetRect.GetSiblingIndex() + 1);

        // Add CanvasGroup for fading
        CanvasGroup newCanvasGroup = newImageObj.AddComponent<CanvasGroup>();
        newCanvasGroup.alpha = 0f;

        // Add CanvasGroup to the old image if not present
        CanvasGroup oldCanvasGroup = targetImage.GetComponent<CanvasGroup>();
        if (oldCanvasGroup == null)
        {
            oldCanvasGroup = targetImage.gameObject.AddComponent<CanvasGroup>();
            oldCanvasGroup.alpha = 1f;
        }

        // Perform the crossfade: Fade in new image while fading out the old one
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

        // Reset the old image's alpha to 1 for future use
        oldCanvasGroup.alpha = 1f;

        // Destroy the temporary RawImage
        Destroy(newImageObj);
    }
}
