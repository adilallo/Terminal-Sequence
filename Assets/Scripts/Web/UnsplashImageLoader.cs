using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

public class UnsplashImageLoader : MonoBehaviour
{
    private string accessKey = "";
    private string baseUrl = "https://api.unsplash.com/search/photos";

    public string searchText = "dog";    // Search term
    public RawImage displayImage;        // UI element to display the image

    private int totalPages = -1;         // Total pages available (-1 means not fetched yet)

    void Start()
    {
        // Start the update loop every 2 seconds
        InvokeRepeating(nameof(UpdateImages), 0f, 8f);
    }

    void UpdateImages()
    {
        StartCoroutine(GetImages(searchText));
    }

    IEnumerator GetImages(string search)
    {
        // If totalPages is not known, fetch it
        if (totalPages <= 0)
        {
            string url = ConstructUrl(search, page: 1);
            yield return StartCoroutine(FetchTotalPages(url));
            if (totalPages <= 0)
            {
                Debug.LogWarning("No pages available for the specified search term.");
                yield break;
            }
        }

        // Now that we have totalPages, pick a random page
        int randomPage = Random.Range(1, totalPages + 1);

        string pageUrl = ConstructUrl(search, randomPage);
        yield return StartCoroutine(FetchAndDisplayImage(pageUrl));
    }

    string ConstructUrl(string search, int page)
    {
        string url = $"{baseUrl}?query={UnityWebRequest.EscapeURL(search)}" +
                     $"&page={page}" +
                     $"&per_page=30";
        return url;
    }

    IEnumerator FetchTotalPages(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Client-ID " + accessKey);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching total pages: " + request.error);
            yield break;
        }

        // Parse the response and extract total pages
        UnsplashResponse response = JsonUtility.FromJson<UnsplashResponse>(request.downloadHandler.text);
        if (response != null)
        {
            totalPages = response.total_pages;
            Debug.Log("Total pages available: " + totalPages);
        }
        else
        {
            totalPages = 0;
        }
    }

    IEnumerator FetchAndDisplayImage(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Client-ID " + accessKey);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching images: " + request.error);
            yield break;
        }

        // Parse the response and extract photo details
        UnsplashResponse response = JsonUtility.FromJson<UnsplashResponse>(request.downloadHandler.text);
        if (response != null && response.results.Length > 0)
        {
            // Get a random photo from the list
            int randomIndex = Random.Range(0, response.results.Length);
            var selectedPhoto = response.results[randomIndex];

            // Get the image URL
            string imageUrl = selectedPhoto.urls.regular;
            StartCoroutine(DownloadImage(imageUrl));
        }
        else
        {
            Debug.LogWarning("No images found on the random page.");
        }
    }

    IEnumerator DownloadImage(string url)
    {
        UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(url);
        yield return textureRequest.SendWebRequest();

        if (textureRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error downloading image: " + textureRequest.error);
            yield break;
        }

        Texture2D tex = ((DownloadHandlerTexture)textureRequest.downloadHandler).texture;
        displayImage.texture = tex;
        displayImage.SetNativeSize();
    }

    [System.Serializable]
    public class UnsplashResponse
    {
        public int total;
        public int total_pages;
        public UnsplashPhoto[] results;
    }

    [System.Serializable]
    public class UnsplashPhoto
    {
        public string id;
        public UnsplashUrls urls;
        // Add other fields if needed
    }

    [System.Serializable]
    public class UnsplashUrls
    {
        public string raw;
        public string full;
        public string regular;
        public string small;
        public string thumb;
    }
}
