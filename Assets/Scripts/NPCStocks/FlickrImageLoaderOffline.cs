using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlickrImageLoaderOffline : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RawImage[] displayImages;
    [SerializeField] private RectTransform parentRect;

    [Header("Material & Effects")]
    [SerializeField] private Material enhancedWeaveBlendMaterial;
    [SerializeField] private PixelSorter pixelSorter;

    [Header("Local Images")]
    [SerializeField] private Texture2D[] fallbackImages;

    [Header("Timing")]
    [SerializeField] private float refreshInterval = 1f;
    [SerializeField] private float crossfadeTime = 3f;
    [SerializeField] private float initialFadeTime = 1.25f;

    readonly Queue<RawImage> overlayPool = new();
    WaitForSecondsRealtime wait;

    void Awake()
    {
        if (displayImages == null || displayImages.Length == 0 || fallbackImages == null || fallbackImages.Length == 0)
        { Debug.LogError("OfflineImageLooper: missing refs"); enabled = false; return; }
        if (!parentRect) parentRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();

        if (enhancedWeaveBlendMaterial)
        {
            var shared = Instantiate(enhancedWeaveBlendMaterial);
            if (shared.HasProperty("_CanvasGroupAlpha"))
                shared.SetFloat("_CanvasGroupAlpha", 1f);
            foreach (var img in displayImages) img.material = shared;
        }

        // each slot needs a CanvasGroup for fades
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
        // wait until Canvas has non‑zero size (additive load safety)
        while (parentRect.rect.width < 1f || parentRect.rect.height < 1f)
            yield return null;

        ArrangeImagesWithOverlap();

        foreach (var img in displayImages)
            img.texture = PrepareTexture(RandomTexture());

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

private void ArrangeImagesWithOverlap()
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

    private void DetermineGridLayout(int n, out int rows, out int columns)
    {
        rows = Mathf.CeilToInt(Mathf.Sqrt(n));
        columns = Mathf.CeilToInt((float)n / rows);
        while (rows * columns < n) columns++;
    }

    IEnumerator Looper()
    {
        while (true)
        {
            yield return wait;
            int slot = Random.Range(0, displayImages.Length);
            RawImage target = displayImages[slot];
            var targetCG = target.GetComponent<CanvasGroup>();

            RawImage overlay = overlayPool.Count > 0 ? overlayPool.Dequeue() : CreateOverlay();
            overlay.texture = PrepareTexture(RandomTexture());
            overlay.material = target.material;

            // Match transforms
            RectTransform rt = overlay.rectTransform, trt = target.rectTransform;
            rt.anchorMin = trt.anchorMin; rt.anchorMax = trt.anchorMax;
            rt.pivot = trt.pivot; rt.sizeDelta = trt.sizeDelta; rt.anchoredPosition = trt.anchoredPosition;
            rt.SetSiblingIndex(trt.GetSiblingIndex() + 1);

            var ovCG = overlay.GetComponent<CanvasGroup>() ?? overlay.gameObject.AddComponent<CanvasGroup>();
            ovCG.alpha = 0f;

            // Classic manual fade (matches old behaviour)
            float t = 0f;
            while (t < crossfadeTime)
            {
                float a = t / crossfadeTime;
                ovCG.alpha = a;           // fade in
                targetCG.alpha = 1f - a;  // fade out
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            ovCG.alpha = 1f; targetCG.alpha = 1f; // restore target alpha

            // swap textures & recycle overlay
            target.texture = overlay.texture;
            overlayPool.Enqueue(overlay);
        }
    }

    Texture2D RandomTexture() => fallbackImages[Random.Range(0, fallbackImages.Length)];
    Texture2D PrepareTexture(Texture2D src) => pixelSorter ? (pixelSorter.SortTexture(src) ?? src) : src;
    RawImage CreateOverlay() { var go = new GameObject("OverlayImage"); go.transform.SetParent(parentRect, false); var ri = go.AddComponent<RawImage>(); ri.raycastTarget = false; go.AddComponent<CanvasGroup>(); return ri; }
}