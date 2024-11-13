using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class FlickrImageLoader : MonoBehaviour
{
    private string apiKey = "976c8648744b8e40c8541aba9ed2f978";
    private string baseUrl = "https://www.flickr.com/services/rest/";

    public string searchText = "dog";    // Search term
    public string tags = "";             // Tags to filter by
    public string tagMode = "any";       // 'any' or 'all'
    public string contentType = "1";     // '1' for photos only
    public string media = "photos";      // 'photos', 'videos', or 'all'
    public string sort = "relevance";    // Sorting method
    public string license = "";          // License type
    public int safeSearch = 1;           // '1' for safe search
    public RawImage[] displayImages;     // UI elements to display images

    private int totalPages = -1;         // Total pages available (-1 means not fetched yet)

    void Start()
    {
        // Start the update loop every 8 seconds
        InvokeRepeating(nameof(UpdateImages), 0f, 8f);
    }

    void UpdateImages()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        int imagesPerPage = (displayImages != null && displayImages.Length > 0) ? displayImages.Length : 5;

        if (totalPages <= 0)
        {
            // Fetch the first page to get totalPages
            FetchFlickrImagesJS(
                searchText,
                1.ToString(),
                tags,
                tagMode,
                contentType,
                media,
                sort,
                license,
                safeSearch.ToString(),
                imagesPerPage.ToString()
            );
        }
        else
        {
            // Fetch a random page
            int randomPage = Random.Range(1, totalPages + 1);
            FetchFlickrImagesJS(
    searchText,
    randomPage.ToString(), // Convert int to string
    tags,
    tagMode,
    contentType,
    media,
    sort,
    license,
    safeSearch.ToString(),
    imagesPerPage.ToString() // Convert int to string
);
        }
#else
        StartCoroutine(GetImages(searchText));
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
private static extern void FetchFlickrImagesJS(
    string searchText,
    string page,
    string tags,
    string tagMode,
    string contentType,
    string media,
    string sort,
    string license,
    string safeSearch,
    string perPage
);
#endif

    public void OnFlickrResponse(string jsonData)
    {
        Debug.Log("Received Flickr response: " + jsonData);

        FlickrResponse response = JsonUtility.FromJson<FlickrResponse>(jsonData);

        if (response == null)
        {
            Debug.LogError("Failed to parse Flickr response.");
            return;
        }

        if (response.stat != "ok")
        {
            Debug.LogError("Flickr API returned an error: " + response.stat);
            return;
        }

        if (response.photos != null)
        {
            if (totalPages <= 0)
            {
                // Set total pages from the first response
                try
                {
                    int totalImages = int.Parse(response.photos.total);
                    int imagesPerPage = (displayImages != null && displayImages.Length > 0) ? displayImages.Length : 5;
                    totalPages = Mathf.CeilToInt((float)totalImages / imagesPerPage);
                    Debug.Log("Total pages calculated: " + totalPages);
                }
                catch (System.FormatException ex)
                {
                    Debug.LogError("Error parsing total images: " + ex.Message);
                    totalPages = 0;
                }

                // Since we fetched page 1 to get totalPages, we need to fetch images from a random page now
                if (totalPages > 1)
                {
#if UNITY_WEBGL && !UNITY_EDITOR
    int randomPage = Random.Range(1, totalPages + 1);
    FetchFlickrImagesJS(
        searchText,
        randomPage.ToString(),                
        tags,                                 
        tagMode,                              
        contentType,                          
        media,                               
        sort,                                 
        license,                              
        safeSearch.ToString(),          
        (displayImages != null && displayImages.Length > 0) 
            ? displayImages.Length.ToString() 
            : "5"                            
    );
#endif
                }
                else
                {
                    // Only one page available, display images from page 1
                    DisplayPhotos(response.photos.photo);
                }
            }
            else
            {
                // Fetching a random page, display images
                DisplayPhotos(response.photos.photo);
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

        int imagesPerPage = (displayImages != null && displayImages.Length > 0) ? displayImages.Length : 5;

        // If totalPages is not known, fetch it
        if (totalPages <= 0)
        {
            string url = ConstructUrl(search, cacheBuster, page: null, perPage: imagesPerPage);
            yield return StartCoroutine(FetchTotalPages(url, imagesPerPage));
            if (totalPages <= 0)
            {
                Debug.LogWarning("No pages available for the specified filters.");
                yield break;
            }
        }

        // Now that we have totalPages, pick a random page
        int randomPage = Random.Range(1, totalPages + 1);

        string pageUrl = ConstructUrl(search, cacheBuster, randomPage, imagesPerPage);
        yield return StartCoroutine(FetchAndDisplayImages(pageUrl));
    }

    string ConstructUrl(string search, string cacheBuster, int? page, int perPage)
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
             $"&format=json&nojsoncallback=1" + // This is the key addition
             $"&per_page={perPage}" +
             (page.HasValue ? $"&page={page.Value}" : "") +
             $"&cachebuster={cacheBuster}";
        Debug.Log("Constructed API URL: " + url); // For debugging
        return url;
    }

    IEnumerator FetchTotalPages(string url, int imagesPerPage)
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

        // Parse the response and extract total images
        FlickrResponse response = JsonUtility.FromJson<FlickrResponse>(request.downloadHandler.text);
        if (response.photos != null)
        {
            try
            {
                int totalImages = int.Parse(response.photos.total);
                totalPages = Mathf.CeilToInt((float)totalImages / imagesPerPage);
                Debug.Log("Total pages calculated: " + totalPages);
            }
            catch (System.FormatException ex)
            {
                Debug.LogError("Error parsing total images: " + ex.Message);
                totalPages = 0;
            }
        }
        else
        {
            totalPages = 0;
        }
    }

    IEnumerator FetchAndDisplayImages(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching images: " + request.error);
            yield break;
        }

        FlickrResponse response = JsonUtility.FromJson<FlickrResponse>(request.downloadHandler.text);
        if (response.photos != null && response.photos.photo.Length > 0)
        {
            int imagesToLoad = Mathf.Min(displayImages.Length, response.photos.photo.Length);
            for (int i = 0; i < imagesToLoad; i++)
            {
                var photo = response.photos.photo[i];

                // Construct image URL
                string photoId = photo.id;
                string serverId = photo.server;
                string secret = photo.secret;

                string imageUrl = $"https://live.staticflickr.com/{serverId}/{photoId}_{secret}.jpg";
                StartCoroutine(DownloadImage(imageUrl, displayImages[i]));
            }
        }
        else
        {
            Debug.LogWarning("No images found on the page.");
        }
    }

    void DisplayPhotos(FlickrResponse.Photo[] photos)
    {
        if (photos.Length > 0)
        {
            int imagesToLoad = Mathf.Min(displayImages.Length, photos.Length);
            for (int i = 0; i < imagesToLoad; i++)
            {
                var photo = photos[i];

                // Construct image URL
                string photoId = photo.id;
                string serverId = photo.server;
                string secret = photo.secret;

                string imageUrl = $"https://live.staticflickr.com/{serverId}/{photoId}_{secret}.jpg";
                StartCoroutine(DownloadImage(imageUrl, displayImages[i]));
            }
        }
        else
        {
            Debug.LogWarning("No images found on the page.");
        }
    }

    IEnumerator DownloadImage(string url, RawImage targetImage)
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

    [System.Serializable]
    public class FlickrResponse
    {
        public string stat; // Added to handle 'stat' field
        public Photos photos;

        [System.Serializable]
        public class Photos
        {
            public int page;
            public int pages;
            public int perpage;
            public string total;
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
