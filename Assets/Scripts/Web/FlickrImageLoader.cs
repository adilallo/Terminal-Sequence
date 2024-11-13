using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class FlickrImageLoader : MonoBehaviour
{
    //private string apiKey = "976c8648744b8e40c8541aba9ed2f978";
    private string baseUrl = "https://www.flickr.com/services/rest/";

    public string searchText = "dog";    // Search term
    public string tags = "";             // Tags to filter by
    public string tagMode = "any";       // 'any' or 'all'
    public string contentType = "1";     // '1' for photos only
    public string media = "photos";      // 'photos', 'videos', or 'all'
    public string sort = "relevance";    // Sorting method
    public string license = "";          // License type
    public int safeSearch = 1;           // '1' for safe search
    public RawImage displayImage;        // UI element to display the image

    private int totalPages = -1;         // Total pages available (-1 means not fetched yet)

    void Start()
    {
        // Start the update loop every 2 seconds
        InvokeRepeating(nameof(UpdateImages), 0f, 8f);
    }

    void UpdateImages()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (totalPages <= 0)
        {
            // Request total pages first
            FetchFlickrImagesJS(searchText, 1);
        }
        else
        {
            // Request a random page
            int randomPage = Random.Range(1, totalPages + 1);
            FetchFlickrImagesJS(searchText, randomPage);
        }
#else
        StartCoroutine(GetImages(searchText));
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void FetchFlickrImagesJS(string searchText, int page);
#endif

    public void OnFlickrResponse(string jsonData)
    {
        Debug.Log("Received Flickr response: " + jsonData);

        FlickrResponse response = JsonUtility.FromJson<FlickrResponse>(jsonData);
        if (response.photos != null)
        {
            if (totalPages <= 0)
            {
                // Set total pages
                totalPages = response.photos.pages;
                Debug.Log("Total pages available: " + totalPages);
            }

            if (response.photos.photo.Length > 0)
            {
                // Get a random photo from the list
                int randomIndex = Random.Range(0, response.photos.photo.Length);
                var selectedPhoto = response.photos.photo[randomIndex];

                // Construct image URL
                string photoId = selectedPhoto.id;
                string serverId = selectedPhoto.server;
                string secret = selectedPhoto.secret;

                string imageUrl = $"https://live.staticflickr.com/{serverId}/{photoId}_{secret}.jpg";
                StartCoroutine(DownloadImage(imageUrl));
            }
            else
            {
                Debug.LogWarning("No images found on the random page.");
            }
        }
        else
        {
            Debug.LogWarning("No photos data received.");
        }
    }

    IEnumerator GetImages(string search)
    {
        string cacheBuster = Random.Range(0, 10000).ToString();

        // If totalPages is not known, fetch it
        if (totalPages <= 0)
        {
            string url = ConstructUrl(search, cacheBuster, page: null);
            yield return StartCoroutine(FetchTotalPages(url));
            if (totalPages <= 0)
            {
                Debug.LogWarning("No pages available for the specified filters.");
                yield break;
            }
        }

        // Now that we have totalPages, pick a random page
        int randomPage = Random.Range(1, totalPages + 1);

        string pageUrl = ConstructUrl(search, cacheBuster, randomPage);
        yield return StartCoroutine(FetchAndDisplayImage(pageUrl));
    }

    string ConstructUrl(string search, string cacheBuster, int? page)
    {
        string url = $"{baseUrl}?method=flickr.photos.search" +
                     $"&api_key={apiKey}" +
                     $"&text={UnityWebRequest.EscapeURL(search)}" +
                     $"&tags={UnityWebRequest.EscapeURL(tags)}" +
                     $"&tag_mode={tagMode}" +
                     $"&content_type={contentType}" +
                     $"&media={media}" +
                     $"&sort={sort}" +
                     $"&license={license}" +
                     $"&safe_search={safeSearch}" +
                     $"&format=json&nojsoncallback=1" +
                     $"&per_page=50" +
                     (page.HasValue ? $"&page={page.Value}" : "") +
                     $"&cachebuster={cacheBuster}";
        return url;
    }

    IEnumerator FetchTotalPages(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching total pages: " + request.error);
            yield break;
        }

        Debug.Log("Total Pages Response: " + request.downloadHandler.text);

        // Parse the response and extract total pages
        FlickrResponse response = JsonUtility.FromJson<FlickrResponse>(request.downloadHandler.text);
        if (response.photos != null)
        {
            totalPages = response.photos.pages;
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
        request.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching images: " + request.error);
            yield break;
        }

        Debug.Log("Image Fetch Response: " + request.downloadHandler.text);

        // Parse the response and extract photo details
        FlickrResponse response = JsonUtility.FromJson<FlickrResponse>(request.downloadHandler.text);
        if (response.photos != null && response.photos.photo.Length > 0)
        {
            // Get a random photo from the list
            int randomIndex = Random.Range(0, response.photos.photo.Length);
            var selectedPhoto = response.photos.photo[randomIndex];

            // Construct image URL
            string photoId = selectedPhoto.id;
            string serverId = selectedPhoto.server;
            string secret = selectedPhoto.secret;

            string imageUrl = $"https://live.staticflickr.com/{serverId}/{photoId}_{secret}.jpg";
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
    public class FlickrResponse
    {
        public Photos photos;

        [System.Serializable]
        public class Photos
        {
            public int page;
            public int pages;
            public int perpage;
            public int total;
            public Photo[] photo;
        }

        [System.Serializable]
        public class Photo
        {
            public string id;
            public string server;
            public string secret;
            public string title;
        }
    }
}
