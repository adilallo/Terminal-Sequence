using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class FlickrImageLoaderOnline : MonoBehaviour
{
    [Header("Remote Source")]
    [Tooltip("Raw GitHub path ending with a trailing slash; files are 000.jpg, 001.jpg, ...")]
    [SerializeField]
    private string baseUrl =
        "https://raw.githubusercontent.com/adilallo/No_Vacancy/feature/adilallo/ISG/Assets/Editor/FlickrImages/";
    [SerializeField, Tooltip("How many numbered images exist remotely (e.g., 99 -> 000..098)")]
    private int totalImages = 99;

    [Header("UI Elements")]
    [SerializeField] private RawImage[] displayImages;
    [SerializeField] private RectTransform parentRect;

    [Header("Material & Effects")]
    [SerializeField] private Material enhancedWeaveBlendMaterial;
    [SerializeField] private PixelSorter pixelSorter;

    [Header("Local Images (fallback)")]
    [SerializeField] private Texture2D[] fallbackImages;

    [Header("Timing")]
    [SerializeField] private float refreshInterval = 1f;
    [SerializeField] private float crossfadeTime = 3f;
    [SerializeField] private float initialFadeTime = 1.25f;

    readonly Queue<RawImage> overlayPool = new();
    WaitForSecondsRealtime wait;

    void Awake()
    {
        if (displayImages == null || displayImages.Length == 0)
        { Debug.LogError("OnlineImageLooper: displayImages missing"); enabled = false; return; }

        if (!parentRect)
            parentRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        if (!parentRect)
        { Debug.LogError("OnlineImageLooper: parent RectTransform not found"); enabled = false; return; }

        // Share one material instance across all slots (matches offline)
        if (enhancedWeaveBlendMaterial)
        {
            var shared = Instantiate(enhancedWeaveBlendMaterial);
            if (shared.HasProperty("_CanvasGroupAlpha"))
                shared.SetFloat("_CanvasGroupAlpha", 1f);

            foreach (var img in displayImages)
                img.material = shared;
        }

        // Each slot needs a CanvasGroup for fades (start hidden; we fade them in)
        foreach (var img in displayImages)
        {
            CanvasGroup cg = img.TryGetComponent(out CanvasGroup cgc) ? cgc : img.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        wait = new WaitForSecondsRealtime(refreshInterval);
        StartCoroutine(InitialiseAfterFirstFrame());
    }

    IEnumerator InitialiseAfterFirstFrame()
    {
        // Wait until the canvas has a non-zero size (important when loading additively)
        while (parentRect.rect.width < 1f || parentRect.rect.height < 1f)
            yield return null;

        ArrangeImagesWithOverlap();

        // Fill the grid from remote (with fallback). Do them in parallel, then fade in.
        int remaining = displayImages.Length;
        for (int i = 0; i < displayImages.Length; i++)
            StartCoroutine(FillSlotInitial(displayImages[i], () => remaining--));

        while (remaining > 0) yield return null;

        yield return StartCoroutine(FadeInInitialGrid());
        StartCoroutine(Looper());
    }

    IEnumerator FadeInInitialGrid()
    {
        float t = 0f;
        while (t < initialFadeTime)
        {
            float a = t / initialFadeTime;
            foreach (var img in displayImages)
                img.GetComponent<CanvasGroup>().alpha = a;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        foreach (var img in displayImages)
            img.GetComponent<CanvasGroup>().alpha = 1f;
    }

    // --- Layout (kept identical to the offline variant) ---
    private void ArrangeImagesWithOverlap()
    {
        int n = displayImages.Length;
        DetermineGridLayout(n, out int rows, out int columns);

        float cellWidth = parentRect.rect.width / columns * 1.2f;
        float cellHeight = parentRect.rect.height / rows * 1.2f;
        float overlapMargin = 0.15f;

        for (int i = 0; i < n; i++)
        {
            int row = i / columns;
            int col = i % columns;

            float anchorMinX = (float)col / columns - overlapMargin;
            float anchorMaxX = (float)(col + 1) / columns + overlapMargin;
            float anchorMinY = 1f - ((float)(row + 1) / rows) - overlapMargin;
            float anchorMaxY = 1f - ((float)row / rows) + overlapMargin;

            anchorMinX = Mathf.Clamp01(anchorMinX);
            anchorMaxX = Mathf.Clamp01(anchorMaxX);
            anchorMinY = Mathf.Clamp01(anchorMinY);
            anchorMaxY = Mathf.Clamp01(anchorMaxY);

            RectTransform rt = displayImages[i].rectTransform;
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            float randomOffsetX = Random.Range(-0.08f, 0.08f) * parentRect.rect.width / columns;
            float randomOffsetY = Random.Range(-0.08f, 0.08f) * parentRect.rect.height / rows;
            rt.anchoredPosition += new Vector2(randomOffsetX, randomOffsetY);
        }
    }

    private void DetermineGridLayout(int n, out int rows, out int columns)
    {
        rows = Mathf.CeilToInt(Mathf.Sqrt(n));
        columns = Mathf.CeilToInt((float)n / rows);
        while (rows * columns < n) columns++;
    }

    // --- Initial fill for each slot ---
    IEnumerator FillSlotInitial(RawImage img, System.Action onDone)
    {
        Texture2D tex = null;
        yield return StartCoroutine(GetRandomRemoteTexture(result => tex = result));

        if (!tex)
            tex = RandomLocalTexture();

        img.texture = PrepareTexture(tex);
        onDone?.Invoke();
    }

    // --- Main loop (matches offline: pick a slot, overlay, crossfade, swap, recycle) ---
    IEnumerator Looper()
    {
        while (true)
        {
            yield return wait;

            int slot = Random.Range(0, displayImages.Length);
            RawImage target = displayImages[slot];
            var targetCG = target.GetComponent<CanvasGroup>();

            // Create/reuse overlay
            RawImage overlay = overlayPool.Count > 0 ? overlayPool.Dequeue() : CreateOverlay();
            overlay.material = target.material;

            // Fetch remote (with fallback), then apply pixel sort
            Texture2D tex = null;
            yield return StartCoroutine(GetRandomRemoteTexture(result => tex = result));
            if (!tex) tex = RandomLocalTexture();
            overlay.texture = PrepareTexture(tex);

            // Match transforms & sibling for overlay
            RectTransform rt = overlay.rectTransform, trt = target.rectTransform;
            rt.anchorMin = trt.anchorMin; rt.anchorMax = trt.anchorMax;
            rt.pivot = trt.pivot; rt.sizeDelta = trt.sizeDelta; rt.anchoredPosition = trt.anchoredPosition;
            rt.SetSiblingIndex(trt.GetSiblingIndex() + 1);

            var ovCG = overlay.GetComponent<CanvasGroup>() ?? overlay.gameObject.AddComponent<CanvasGroup>();
            ovCG.alpha = 0f;

            // Manual crossfade (unscaled time to match offline)
            float t = 0f;
            while (t < crossfadeTime)
            {
                float a = t / crossfadeTime;
                ovCG.alpha = a;         // fade in
                targetCG.alpha = 1f - a;  // fade out
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            ovCG.alpha = 1f;
            targetCG.alpha = 1f; // restore

            // Swap & recycle
            target.texture = overlay.texture;
            overlayPool.Enqueue(overlay);
        }
    }

    // --- Remote fetch helper (tries once per call) ---
    IEnumerator GetRandomRemoteTexture(System.Action<Texture2D> onDone)
    {
        Texture2D result = null;

        int idx = Random.Range(0, Mathf.Max(1, totalImages));
        string url = $"{baseUrl}{idx:D3}.jpg";

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url, /*nonReadable:*/ false))
        {
            // Mildly helps with intermediaries caching stale 404s
            req.SetRequestHeader("Cache-Control", "no-cache");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                result = DownloadHandlerTexture.GetContent(req);
            }
            else
            {
                Debug.Log($"FlickrImageLoaderOnline: download failed {url} -> {req.error}");
            }
        }

        onDone?.Invoke(result);
    }

    // --- Utilities mirrored from offline ---
    Texture2D RandomLocalTexture()
    {
        if (fallbackImages != null && fallbackImages.Length > 0)
            return fallbackImages[Random.Range(0, fallbackImages.Length)];
        Debug.LogWarning("FlickrImageLoaderOnline: No fallback images assigned.");
        return Texture2D.blackTexture;
    }

    Texture2D PrepareTexture(Texture2D src)
    {
        if (pixelSorter == null) return src;
        var sorted = pixelSorter.SortTexture(src);
        return sorted ? sorted : src;
    }

    RawImage CreateOverlay()
    {
        var go = new GameObject("OverlayImage");
        go.transform.SetParent(parentRect, false);
        var ri = go.AddComponent<RawImage>();
        ri.raycastTarget = false;
        go.AddComponent<CanvasGroup>();
        return ri;
    }
}
