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

    void Start()
    {
        // Start the update loop every 1 second
        InvokeRepeating(nameof(UpdateRandomImages), 0f, 1f);
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

        // Start a coroutine to download and display the image
        StartCoroutine(DownloadAndSetImage(imageUrl, targetImage));
    }

    IEnumerator DownloadAndSetImage(string url, RawImage targetImage)
    {
        UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(url);
        yield return textureRequest.SendWebRequest();

        if (textureRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error downloading image: " + textureRequest.error);
            yield break;
        }

        Texture2D tex = ((DownloadHandlerTexture)textureRequest.downloadHandler).texture;
        targetImage.texture = tex;
        targetImage.SetNativeSize();
    }
}
