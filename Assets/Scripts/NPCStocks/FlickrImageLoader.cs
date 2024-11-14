using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

public class FlickrImageLoader : MonoBehaviour
{
    // Base URL for the images hosted on GitHub
    private string baseUrl = "https://raw.githubusercontent.com/adilallo/No_Vacancy/feature/adilallo/ISG/Assets/Editor/FlickrImages/";

    public RawImage[] displayImages;  // Array of UI elements to display images
    private int totalImages = 99;     // Number of images available (000 to 098)

    [SerializeField] private Material enhancedWeaveBlendMaterial; // Assign the enhanced material in the Inspector
    [SerializeField] private PixelSorter pixelSorter;               // Reference to the PixelSorter script

    void Start()
    {
        // Populate all RawImage components initially
        PopulateAllImages();

        // Start the update loop every 1 second
        InvokeRepeating(nameof(UpdateRandomImages), 1f, 1f);
    }

    void PopulateAllImages()
    {
        for (int i = 0; i < displayImages.Length; i++)
        {
            // Pick a random image index from 000 to 098
            int randomImageNumber = Random.Range(0, totalImages);

            // Construct the image URL with the correct file name
            string imageUrl = $"{baseUrl}{randomImageNumber:D3}.jpg";

            // Assign the enhanced custom material to handle blending and waving
            if (enhancedWeaveBlendMaterial != null)
            {
                displayImages[i].material = enhancedWeaveBlendMaterial;
            }
            else
            {
                Debug.LogWarning("EnhancedWeaveBlendMaterial is not assigned in the Inspector.");
            }

            // Start a coroutine to download, sort, and display the image
            StartCoroutine(DownloadSortAndSetImage(imageUrl, displayImages[i]));
        }
    }

    void UpdateRandomImages()
    {
        // Pick a random RawImage component from the array
        int randomIndex = Random.Range(0, displayImages.Length);
        RawImage targetImage = displayImages[randomIndex];

        // Pick a random image index from 000 to 098
        int randomImageNumber = Random.Range(0, totalImages);

        // Construct the image URL with the correct file name
        string imageUrl = $"{baseUrl}{randomImageNumber:D3}.jpg";

        // Start a coroutine to download, sort, and display the image
        StartCoroutine(DownloadSortAndSetImage(imageUrl, targetImage));
    }

    IEnumerator DownloadSortAndSetImage(string url, RawImage targetImage)
    {
        // Download the image
        UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(url);
        yield return textureRequest.SendWebRequest();

        if (textureRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error downloading image from {url}: {textureRequest.error}");
            yield break;
        }

        Texture2D originalTexture = ((DownloadHandlerTexture)textureRequest.downloadHandler).texture;

        // Sort the image using PixelSorter
        Texture2D sortedTexture = pixelSorter.SortTexture(originalTexture);

        // Assign the sorted texture to the RawImage
        targetImage.texture = sortedTexture;
        targetImage.SetNativeSize();
    }
}